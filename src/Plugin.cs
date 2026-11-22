using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace CompoundMP
{
	[BepInPlugin("com.compoundmp.coop", "COMPOUND Multiplayer", "0.4.0")]
	public class Plugin : BaseUnityPlugin
	{
		const float SendInterval = 1f / 20f;
		const float EnemyInterval = 1f / 10f;

		internal static Plugin Instance;
		internal static BepInEx.Logging.ManualLogSource Log => Instance.Logger;

		ConfigEntry<KeyCode> hostKey, joinKey, stopKey;
		ConfigEntry<bool> useUpnp;
		Upnp upnp;
		string menuNote = "";
		ConfigEntry<string> joinAddress, password, hostName, recentHosts;
		ConfigEntry<bool> lanOn;
		LanDiscovery.Announcer lanAnnouncer; LanDiscovery.Listener lanListener;
		ConfigEntry<int> port;
		ConfigEntry<string> autoStart, debugLoadMap;
		ConfigEntry<bool> debugCombat, debugDumpMenu;
		ConfigEntry<int> simLoss;
		ConfigEntry<bool> levelTest, gunTest, hostQuitTest, mpShot;
		int mpShots;
		bool levelTested, gunTested;
		ConfigEntry<string> shotDir;
		bool menuDumped;
		string autoStartMode = ""; bool mapLoadRequested, trailTested; int lastProbe = -1, lastPlace = -1, lastAware = -1; Vector3? fakeHead; float combatClock = -1f; int combatStep;
		float statusTimer;

		Transport net;
		internal readonly Dictionary<string, Avatar> Avatars = new Dictionary<string, Avatar>();
		readonly EnemySync enemies = new EnemySync();
		float sendTimer, enemyTimer;
		internal readonly string MyId = Guid.NewGuid().ToString("N").Substring(0, 8);

		// Seed received from the host; applied on the client the next time a map is created.
		internal static bool HasHostSeed;
		internal static int HostSeed;
		internal static bool Running => Instance != null && Instance.net != null;
		internal static bool IsClient => Running && !Instance.net.IsHost;
		internal static bool IsHost => Running && Instance.net.IsHost;
		// True while the mod itself is applying damage that came over the network.
		internal static bool Applying;

		void Awake()
		{
			Instance = this;
			hostKey = Config.Bind("Keys", "Host", KeyCode.F8, "Start hosting a co-op session");
			joinKey = Config.Bind("Keys", "Join", KeyCode.F9, "Join the host at JoinAddress");
			stopKey = Config.Bind("Keys", "Stop", KeyCode.F10, "Leave / stop the session");
			useUpnp = Config.Bind("Network", "UseUPnP", true, "When hosting, ask the router to forward the UDP port automatically (UPnP)");
			password = Config.Bind("Network", "Password", "", "Optional shared secret: a host with a password rejects joiners that do not use the same one");
			hostName = Config.Bind("Network", "HostName", "COMPOUND co-op", "Name shown to other players on the LAN when you host");
			lanOn = Config.Bind("Network", "LanDiscovery", true, "Announce your hosted game on the local network and list other hosts in the co-op menu");
			recentHosts = Config.Bind("Network", "RecentHosts", "", "Last hosts joined, separated by ';' (managed by the mod)");
			joinAddress = Config.Bind("Network", "JoinAddress", "127.0.0.1", "Host IP to join");
			port = Config.Bind("Network", "Port", 27777, "UDP port (host must forward/open it)");
			autoStart = Config.Bind("Debug", "AutoStart", "", "'host' or 'join' to start a session on launch (headless testing)");
			debugLoadMap = Config.Bind("Debug", "LoadMap", "", "Scene to load a few seconds after launch (headless testing)");
			shotDir = Config.Bind("Debug", "ScreenshotDir", "", "Save PNG screenshots here (development; needs graphics)");
			Shot.Dir = shotDir.Value;
			mpShot = Config.Bind("Debug", "MultiplayerShot", false, "Save screenshots of the combat scenario from near the first enemy (needs ScreenshotDir and a rendering game)");
			hostQuitTest = Config.Bind("Debug", "HostQuitTest", false, "Host leaves the session 90 s into the combat scenario (testing that clients notice)");
			levelTest = Config.Bind("Debug", "LevelTest", false, "Host changes level a while into the combat scenario (testing level sync)");
			gunTest = Config.Bind("Debug", "GunTest", false, "Client fires one player bullet in the combat scenario (testing projectile relay)");
			simLoss = Config.Bind("Debug", "SimulatedLossPercent", 0, "Drop this percentage of outgoing packets (testing how the game copes with a lossy network)");
			debugDumpMenu = Config.Bind("Debug", "DumpMenu", false, "Log the game's VR menu hierarchy (development)");
			debugCombat = Config.Bind("Debug", "Combat", false, "Run a scripted enemy/damage sync check once enemies exist (headless testing)");
			autoStartMode = autoStart.Value;
			// Steam passes "+connect <host:port>" when the game is started from a steam://run/615120//+connect%20<host:port> link.
			var cmd = Environment.GetCommandLineArgs();
			for (int i = 0; i + 1 < cmd.Length; i++)
				if (cmd[i] == "+connect" || cmd[i] == "--connect") { joinAddress.Value = cmd[i + 1]; autoStartMode = "join"; Logger.LogInfo("Joining " + cmd[i + 1] + " (from the command line)"); }
			var harmony = new Harmony("com.compoundmp.coop");
			harmony.PatchAll(typeof(Plugin).Assembly);
			foreach (var m in harmony.GetPatchedMethods()) Logger.LogInfo("Patched " + m.DeclaringType.Name + "." + m.Name);
			Logger.LogInfo("COMPOUND MP loaded. F8=host, F9=join, F10=stop");
		}

		void Update()
		{
			if (net == null && autoStartMode != "" && Time.realtimeSinceStartup > 5f)
			{
				string mode = autoStartMode;
				autoStartMode = ""; // not written back to the config: several instances can share one file
				if (mode == "host") StartHost(); else if (mode == "join") StartClient();
			}
			if (Input.GetKeyDown(hostKey.Value)) StartHost();
			if (Input.GetKeyDown(joinKey.Value)) StartClient();
			if (Input.GetKeyDown(stopKey.Value)) Stop();
			DebugDriver();
			SteamJoin.Tick(Time.unscaledTime);
			if (net == null) return;

			while (net.TryDequeue(out var from, out var data))
			{
				if (!net.IsHost && from.Equals(net.HostEp)) lastHostRx = Time.unscaledTime;
				Handle(from, data);
				if (net == null) return; // a Reject or Bye may have ended the session
			}
			net.Pump();
			Watchdog();
			LevelSync.Tick();

			sendTimer += Time.unscaledDeltaTime;
			if (sendTimer >= SendInterval) { sendTimer = 0f; SendState(); }

			enemyTimer += Time.unscaledDeltaTime;
			if (enemyTimer >= EnemyInterval)
			{
				enemyTimer = 0f;
				if (net.IsHost) enemies.HostSend(net);
			}
			if (!net.IsHost) enemies.ClientTick();

			foreach (var a in Avatars.Values) a.Tick();
			var pc = Singleton<GlobalGameInfo>.Instance?.PlayerControllerInstance;
			bool localDead = pc != null && pc.IsDead();
			if (localDead && !wasLocalDead && Avatars.Count > 0 && Avatars.Values.All(a => a.Dead)) Toast("Everyone is down");
			wasLocalDead = localDead;

			statusTimer += Time.unscaledDeltaTime;
			if (statusTimer >= 5f)
			{
				statusTimer = 0f;
				if (net.IsHost) SteamJoin.Publish(SteamConnectAddress());
				Logger.LogInfo($"[status] {(net.IsHost ? "host" : "client")} peers={(net.IsHost ? net.Clients.Count : Avatars.Count)} avatars={Avatars.Count} seed={(HasHostSeed ? HostSeed.ToString() : "-")} {(upnp != null ? upnp.Status + " " : "")}{ProjectileSync.Stats()} {NetRates(5f)} map={EnemySync.MapName} {enemies.Summary()}");
			}
		}

		void OnDestroy() { Stop(); lanListener?.Stop(); }

		// Throughput since the previous call, total and for the busiest packet types, e.g. "tx=2.1kB/s rx=1.4kB/s [Enemies 1.1 State 0.8]".
		readonly long[] prevSent = new long[256], prevRecv = new long[256];
		static readonly string[] TypeNames = { "", "Hello", "Seed", "State", "Bye", "Enemies", "EnemyDamage", "PlayerDamage", "Projectile", "Trail", "Ack", "Welcome", "Reject", "Effect", "LevelChange" };
		string NetRates(float seconds)
		{
			long tx = 0, rx = 0;
			var per = new List<KeyValuePair<string, double>>();
			for (int t = 1; t < TypeNames.Length; t++)
			{
				long ds = net.SentBytes[t] - prevSent[t], dr = net.RecvBytes[t] - prevRecv[t];
				prevSent[t] = net.SentBytes[t]; prevRecv[t] = net.RecvBytes[t];
				tx += ds; rx += dr;
				if (ds + dr > 0) per.Add(new KeyValuePair<string, double>(TypeNames[t], (ds + dr) / 1024.0 / seconds));
			}
			per.Sort((a, b) => b.Value.CompareTo(a.Value));
			var top = new List<string>();
			for (int i = 0; i < per.Count && i < 4; i++) top.Add($"{per[i].Key} {per[i].Value:0.0}");
			return $"tx={tx / 1024.0 / seconds:0.0}kB/s rx={rx / 1024.0 / seconds:0.0}kB/s [{string.Join(" ", top.ToArray())}] rejected={net.RejectedPackets}";
		}

		// Connection upkeep: the client keeps asking to join until the host answers; each side drops a peer that went silent.
		const float HelloRetry = 1f, HelloGiveUp = 15f, PeerTimeout = 10f, HostTimeout = 15f;
internal bool Welcomed => welcomed;
		bool wasLocalDead;
		bool welcomed; int helloTries; float helloTimer, lastHostRx;
		byte[] pwKey, clientNonce;
		readonly Dictionary<string, KeyValuePair<byte[], byte[]>> sessions = new Dictionary<string, KeyValuePair<byte[], byte[]>>(); // client id -> (client nonce, host nonce)
		readonly Dictionary<string, float> lastSeen = new Dictionary<string, float>();

		void Watchdog()
		{
			if (net.IsHost)
			{
				List<string> gone = null;
				foreach (var kv in lastSeen)
					if (Time.unscaledTime - kv.Value > PeerTimeout) (gone ?? (gone = new List<string>())).Add(kv.Key);
				if (gone != null)
					foreach (var id in gone)
					{
						lastSeen.Remove(id); sessions.Remove(id); if (net.Clients.TryGetValue(id, out var gone2)) net.RemoveKey(gone2); net.Clients.Remove(id);
						if (Avatars.TryGetValue(id, out var av)) { av.Destroy(); Avatars.Remove(id); }
						Logger.LogInfo("Client timed out: " + id); Toast("A player lost connection");
					}
				return;
			}
			if (!welcomed)
			{
				helloTimer += Time.unscaledDeltaTime;
				if (helloTimer >= HelloRetry)
				{
					helloTimer = 0f;
					if (++helloTries * HelloRetry > HelloGiveUp) { Fail("No answer from the host at " + net.HostEp); return; }
					SendHello();
				}
			}
			else if (Time.unscaledTime - lastHostRx > HostTimeout) Fail("Lost connection to the host");
		}

		void Fail(string why)
		{
			Logger.LogWarning(why);
			Stop();
			menuNote = why;
			Toast(why);
		}

		// Headless test driver: load a level, then exercise the real damage paths and log the results.
		void DebugDriver()
		{
			var g = Singleton<GlobalGameInfo>.Instance;
			if (g == null) return;
			if (debugLoadMap.Value != "" && !mapLoadRequested && net != null && Time.realtimeSinceStartup > 20f)
			{
				mapLoadRequested = true;
				string name = debugLoadMap.Value;
				Logger.LogInfo("[debug] loading " + name);
				UnityEngine.SceneManagement.SceneManager.LoadScene(name, UnityEngine.SceneManagement.LoadSceneMode.Additive);
				g.CurrentMapName = name;
				g.Map = null;
				g.GameControllerInstance.CurrentMapInfo = null;
			}
			if (debugDumpMenu.Value && !menuDumped && Time.realtimeSinceStartup > 25f) { menuDumped = true; StartCoroutine(MenuDump.Run(Logger)); }
			if (!debugCombat.Value || net == null || g.Map == null || g.Map.Enemies.Count == 0) return;
			if (combatClock < 0f) { combatClock = Time.realtimeSinceStartup; Logger.LogInfo("[debug] enemies present: " + g.Map.Enemies.Count); }
			float t = Time.realtimeSinceStartup - combatClock;
			if ((int)t != lastProbe) { lastProbe = (int)t; Logger.LogInfo($"[debug] t={t:0} {(net.IsHost ? "host" : "client")} {enemies.Probe()}"); }
			// Live-fight scenario: client player is made mortal and stood 3m from enemy 0; the host enemies must
			// chase/shoot the nearest player (the client's ghost) and their bullets must reach the client as damage.
			var first = g.Map.Enemies[0];
			if (first == null) return;
			Vector3 ep = first.transform.position;
			if (!net.IsHost && t > 6f && (int)t != lastPlace)
			{
				lastPlace = (int)t;
				var pd = g.PlayerControllerInstance.DamageableComponent;
				pd.IsImmortal = false;
				pd.InvincibilityCoolDown = 0f;
				fakeHead = ep + Quaternion.Euler(0f, Mathf.Abs(MyId.GetHashCode()) % 360, 0f) * new Vector3(3f, 0f, 0f) + Vector3.up * 1.5f; // each test client stands at its own spot 3 m from enemy 0
			}
			if (!net.IsHost && gunTest.Value && t > 14f && !gunTested)
			{
				// A player bullet fired on a client: the host and other clients must receive a replica.
				gunTested = true;
				var all = Resources.FindObjectsOfTypeAll<BulletController>().Where(b => !b.gameObject.scene.IsValid() && !(b is HitscanBulletController) && b.GetComponent<Rigidbody>() != null).ToList();
				Logger.LogInfo("[debug] bullet prefabs: " + string.Join(", ", all.Select(b => b.name).ToArray()));
				var gunBullet = all.FirstOrDefault(b => !b.name.Contains("Enemy") && !b.name.StartsWith("Red")) ?? all.FirstOrDefault();
				if (gunBullet != null)
				{
					var shot = Instantiate(gunBullet.gameObject, fakeHead.GetValueOrDefault(ep) + Vector3.up * 0.2f, Quaternion.LookRotation(ep - fakeHead.GetValueOrDefault(ep)));
					shot.GetComponent<Rigidbody>().velocity = (ep - fakeHead.GetValueOrDefault(ep)).normalized * 30f;
					Logger.LogInfo("[debug] client fired " + gunBullet.name);
				}
			}
			if (mpShot.Value && mpShots < 3 && t > 22f + mpShots * 5f)
			{
				// The scene as a player in the fight sees it: the first enemy, the other player's avatar and the bullets between them.
				int n = ++mpShots;
				Vector3 camPos = ep + Quaternion.Euler(0f, 40f, 0f) * new Vector3(0f, 2.2f, -6.5f), look = ep + new Vector3(1.2f, 1.2f, 0f);
  var other = Avatars.Values.FirstOrDefault();
  if (net.IsHost && other != null)
  {
    // from behind the other player's avatar, so the avatar is in front and the enemy and its bullets are beyond it
    Vector3 h = other.HeadPos, away = new Vector3(h.x - ep.x, 0f, h.z - ep.z).normalized;
    camPos = h + away * 3.2f + Vector3.up * 0.5f; look = (h + ep) * 0.5f + Vector3.up * 0.4f;
  }
  else if (!net.IsHost && fakeHead.HasValue) { camPos = fakeHead.Value; look = ep + Vector3.up * 1.2f; } // through this player's own eyes
  var cam = Shot.ViewCamera(camPos, look, 70f);
				StartCoroutine(Shot.CaptureRoutine(cam, (net.IsHost ? "multiplayer_host_view_" : "multiplayer_client_view_") + n));
			}
			if (net.IsHost && hostQuitTest.Value && t > 90f) { Logger.LogInfo("[debug] host leaves"); Stop(); }
			if (net.IsHost && levelTest.Value && t > 40f && !levelTested)
			{
				levelTested = true;
				var lt = FindObjectsOfType<NextLevelTrigger>().FirstOrDefault();
				if (lt != null)
				{
					Logger.LogInfo("[debug] host changes level from " + g.CurrentMapName);
					lt.NextMapName = "map01_sewers_b";
					lt.ChangeLevel();
				}
				else Logger.LogInfo("[debug] no NextLevelTrigger in this scene");
			}
			if (net.IsHost && t > 12f && !trailTested)
			{
				// One real hitscan shot from the host, to exercise trail replication.
				trailTested = true;
				var hp = Resources.FindObjectsOfTypeAll<HitscanBulletController>().FirstOrDefault(h => !h.gameObject.scene.IsValid());
				if (hp != null)
				{
					Instantiate(hp.gameObject, ep + Vector3.up * 1.5f, Quaternion.LookRotation(Vector3.right));
					Logger.LogInfo("[debug] fired hitscan " + hp.name);
				}
				else Logger.LogInfo("[debug] no hitscan prefab loaded");
			}
			if (net.IsHost && t > 8f && (int)t % 5 == 0 && (int)t != lastAware)
			{
				lastAware = (int)t;
				var ec = first.GetComponentInParent<EnemyController>();
				if (ec != null) ec.MakeAwareOfPlayer();
				foreach (var a in Avatars.Values)
					Logger.LogInfo($"[debug] ghost head={a.HeadPos} enemy0={ep} dist={Vector3.Distance(a.HeadPos, ep):0.0} enemy0Alert={(ec != null && ec.IsAlert)}");
			}
		}

		// ---- state shown by the in-game VR menu (CoopMenu.cs) ----

				internal bool UpnpOn { get => useUpnp.Value; set => useUpnp.Value = value; }
		internal string PasswordText { get => password.Value; set => password.Value = value; }
		internal bool HasPassword => password.Value != "";

		// Hosts heard on the LAN (listener starts on first use) and the hosts joined before, most recent first.
		internal List<LanDiscovery.HostInfo> LanHosts()
		{
			try
			{
				if (lanListener == null) { lanListener = new LanDiscovery.Listener(MyId); lanListener.Start(); }
				return lanListener.Hosts();
			}
			catch (Exception e) { Logger.LogWarning("LAN listener failed: " + e.Message); lanListener = null; return new List<LanDiscovery.HostInfo>(); }
		}
		internal List<string> RecentHosts() => new List<string>(recentHosts.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
		void RememberHost(string target)
		{
			target = target.Trim();
			if (target == "") return;
			var list = RecentHosts();
			list.RemoveAll(h => h == target);
			list.Insert(0, target);
			if (list.Count > 5) list.RemoveRange(5, list.Count - 5);
			recentHosts.Value = string.Join(";", list.ToArray());
		}
		internal string JoinTarget { get => joinAddress.Value; set => joinAddress.Value = value; }
		internal string StatusLine()
		{
			if (net == null) return menuNote != "" ? menuNote : "Not connected";
			if (net.IsHost) return $"Hosting on {port.Value} - {net.Clients.Count + 1} player(s)";
			return HasHostSeed ? "Joined - synced with host" : "Joining " + net.HostEp + "...";
		}
		// What Steam friends connect to: the public address when the router mapping worked, else this PC's LAN address.
		string SteamConnectAddress()
		{
			string ip = upnp != null && upnp.ExternalIp != null && !Upnp.IsPrivate(upnp.ExternalIp) ? upnp.ExternalIp : null;
			if (ip == null)
				foreach (var a in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
					if (a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(a)) { ip = a.ToString(); break; }
			return ip == null ? "" : ip + ":" + port.Value;
		}

		// A Steam friend pressed "Join Game" while this game was already running.
		internal void JoinFromSteam(string connect)
		{
			if (string.IsNullOrEmpty(connect)) return;
			joinAddress.Value = connect.Trim();
			Toast("Joining a friend's game");
			StartClient();
		}

		string inviteLogged = "";
		internal string NetLine()
		{
			if (net == null || !net.IsHost) return net == null ? "Start a run from HQ once connected" : "The host starts the next level";
			if (upnp == null) return "Forward UDP " + port.Value + " or use a VPN";
			if (upnp.ExternalIp != null && inviteLogged != upnp.ExternalIp)
			{
				inviteLogged = upnp.ExternalIp;
				Logger.LogInfo($"Invite link for Steam friends: steam://run/615120//+connect%20{upnp.ExternalIp}:{port.Value}");
			}
			return upnp.ExternalIp != null ? "Friends join: " + upnp.ExternalIp + (port.Value != 27777 ? ":" + port.Value : "") : upnp.Status;
		}

		// ---- session control ----

		internal void StartHost()
		{
			Stop();
			menuNote = "";
			try
			{
pwKey = password.Value != "" ? Auth.PasswordKey(password.Value) : null;
				net = new Transport(true, port.Value, pwKey != null) { SimulatedLossPercent = simLoss.Value };
				Logger.LogInfo("Hosting on UDP " + port.Value);
				Toast("Hosting co-op on port " + port.Value);
				if (useUpnp.Value)
				{
				upnp = new Upnp { Log = m => Logger.LogInfo(m) };
				upnp.Start(port.Value, "COMPOUND co-op");
				}
				if (lanOn.Value)
				{
					try
					{
						lanAnnouncer = new LanDiscovery.Announcer { Name = hostName.Value, GamePort = port.Value, NeedsPassword = pwKey != null, Players = () => net != null ? net.Clients.Count + 1 : 1 };
						lanAnnouncer.Start();
					}
					catch (Exception e) { Logger.LogWarning("LAN announce failed: " + e.Message); lanAnnouncer = null; }
				}
			}
			catch (Exception e) { Logger.LogError(e); menuNote = "Could not host: " + e.Message; Toast(menuNote); net?.Close(); net = null; }
		}

		internal void StartClient()
		{
			Stop();
			menuNote = "";
			try
			{
				// "host" or "host:port"; the VR keypad types the latter.
				string target = joinAddress.Value.Trim();
				int hostPort = port.Value, colon = target.LastIndexOf(':');
				if (colon > 0 && int.TryParse(target.Substring(colon + 1), out int parsed)) { hostPort = parsed; target = target.Substring(0, colon); }
				var addr = ResolveHost(target);
pwKey = password.Value != "" ? Auth.PasswordKey(password.Value) : null;
				clientNonce = Auth.Nonce();
				net = new Transport(false, 0, pwKey != null) { SimulatedLossPercent = simLoss.Value };
				net.HostEp = new IPEndPoint(addr, hostPort);
				welcomed = false; helloTries = 1; helloTimer = 0f; lastHostRx = Time.unscaledTime;
				SendHello();
				Logger.LogInfo("Joining " + net.HostEp);
				Toast("Joining " + net.HostEp + " - return to HQ and start a new run");
			}
			catch (Exception e) { Logger.LogError(e); menuNote = "Could not join: " + e.Message; Toast(menuNote); net?.Close(); net = null; }
		}

		void SendHello() => net.SendTo(net.HostEp, Proto.Hello, w =>
		{
			w.Write(Proto.Version);
			w.Write(MyId);
			w.Write(clientNonce);
			w.Write(pwKey != null);
			if (pwKey != null) w.Write(Auth.HelloProof(pwKey, MyId, clientNonce));
		});

		// Accepts a dotted IP or a hostname (e.g. a dynamic-DNS name).
		static IPAddress ResolveHost(string host)
		{
			host = host.Trim();
			if (IPAddress.TryParse(host, out var ip)) return ip;
			foreach (var a in Dns.GetHostAddresses(host))
				if (a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) return a;
			throw new Exception("cannot resolve " + host);
		}

		internal void Stop()
		{
			if (upnp != null) { upnp.Stop(); upnp = null; }
			if (lanAnnouncer != null) { lanAnnouncer.Stop(); lanAnnouncer = null; }
			SteamJoin.Publish("");
			if (net == null) return;
			try { net.Broadcast(Proto.Bye, w => w.Write(MyId)); } catch { }
			net.Close();
			net = null;
			lastSeen.Clear();
			sessions.Clear();
			HasHostSeed = false;
			foreach (var a in Avatars.Values) a.Destroy();
			Avatars.Clear();
			enemies.Reset();
			Logger.LogInfo("Session stopped");
		}

		internal void Toast(string s)
		{
			try { Singleton<GlobalGameInfo>.Instance.GameControllerInstance.MessageSystem.AddMessage(s); }
			catch { }
		}

		// ---- protocol ----

		void Handle(IPEndPoint from, byte[] data)
		{
			try
			{
				var r = new BinaryReader(new MemoryStream(data));
				byte type = r.ReadByte();
				bool host = net.IsHost;
				switch (type)
				{
					case Proto.Hello when host:
						{
							byte ver = r.ReadByte();
							if (ver != Proto.Version)
							{
								net.SendTo(from, Proto.Reject, w => w.Write("Version mismatch: host speaks protocol " + Proto.Version + ", you speak " + ver));
								break;
							}
							string id = r.ReadString();
							byte[] cn = r.ReadBytes(Auth.NonceLen);
							bool hasPw = r.ReadBoolean();
							byte[] proof = hasPw ? r.ReadBytes(32) : null;
							if (pwKey != null)
							{
								if (!hasPw) { net.SendTo(from, Proto.Reject, w => w.Write("This host needs a password")); break; }
								if (!Auth.Equal(proof, Auth.HelloProof(pwKey, id, cn)))
								{
									Logger.LogWarning("Rejected " + from + ": wrong password");
									net.SendTo(from, Proto.Reject, w => w.Write("Wrong password"));
									break;
								}
							}
							// A repeated Hello (same client nonce) gets the same host nonce, so a late duplicate cannot change a live session key.
							byte[] hn;
							if (sessions.TryGetValue(id, out var sess) && Auth.Equal(sess.Key, cn)) hn = sess.Value;
							else { hn = Auth.Nonce(); sessions[id] = new KeyValuePair<byte[], byte[]>(cn, hn); }
							bool isNew = !net.Clients.ContainsKey(id);
							net.Clients[id] = from;
							lastSeen[id] = Time.unscaledTime;
							if (isNew) { Logger.LogInfo("Client joined: " + id); Toast("Player joined"); }
							net.SendTo(from, Proto.Welcome, w =>
							{
								w.Write(pwKey != null);
								if (pwKey != null) { w.Write(hn); w.Write(Auth.WelcomeProof(pwKey, cn, hn)); }
							});
							if (pwKey != null) net.SetKey(from, Auth.SessionKey(pwKey, cn, hn)); // everything after Welcome is tagged
							SendSeed(from); // repeated Hellos re-send it, which is harmless
							break;
						}
					case Proto.Welcome when !host:
						{
							bool authRequired = r.ReadBoolean();
							if (pwKey != null && !authRequired) { Fail("Host is not password protected; refusing to join without authentication"); break; }
							if (pwKey == null && authRequired) { Fail("This host needs a password (set Password in the config)"); break; }
							if (authRequired)
							{
								byte[] hn = r.ReadBytes(Auth.NonceLen);
								byte[] proof = r.ReadBytes(32);
								if (!Auth.Equal(proof, Auth.WelcomeProof(pwKey, clientNonce, hn))) { Fail("The host failed to prove it knows the password"); break; }
								net.SetKey(net.HostEp, Auth.SessionKey(pwKey, clientNonce, hn));
							}
							welcomed = true;
							RememberHost(joinAddress.Value);
							break;
						}
					case Proto.Reject when !host:
						Fail("Host refused the connection: " + r.ReadString());
						break;
					case Proto.Ack:
						net.Ack(r.ReadUInt32());
						break;
					case Proto.Seed when !host:
						HostSeed = r.ReadInt32();
						HasHostSeed = true;
						Logger.LogInfo("Got host seed " + HostSeed);
						Toast("Synced to host seed");
						break;
					case Proto.State:
						{
							string id = r.ReadString();
							if (id == MyId) break;
							if (host) lastSeen[id] = Time.unscaledTime;
							var pose = new Pose3[3];
							for (int i = 0; i < 3; i++) pose[i] = Pose3.Read(r);
							bool dead = r.ReadBoolean();
							if (!Avatars.TryGetValue(id, out var av)) Avatars[id] = av = new Avatar(id);
							bool wasDead = av.Dead;
							av.SetTarget(pose, dead);
							if (dead && !wasDead) Toast("A player went down");
							if (host) net.Broadcast(Proto.State, w => w.Write(data, 1, data.Length - 1), from); // relay to the other clients
							break;
						}
					case Proto.Bye:
						{
							string id = r.ReadString();
							if (!host && from.Equals(net.HostEp)) { Fail("The host left the game"); break; }
							if (Avatars.TryGetValue(id, out var av)) { av.Destroy(); Avatars.Remove(id); }
							if (net.Clients.TryGetValue(id, out var byeEp)) net.RemoveKey(byeEp);
							net.Clients.Remove(id);
							lastSeen.Remove(id);
							sessions.Remove(id);
							break;
						}
					case Proto.Enemies when !host:
						enemies.ClientReceive(r);
						break;
					case Proto.EnemyDamage when host:
						{
							uint seq = r.ReadUInt32();
							net.SendAck(from, seq);
							if (net.Fresh(from, seq)) enemies.HostApplyDamage(r.ReadInt32(), r.ReadInt32());
							break;
						}
					case Proto.LevelChange when !host:
						{
							uint seq = r.ReadUInt32();
							net.SendAck(from, seq);
							if (net.Fresh(from, seq)) LevelSync.OnHostAnnounced(r.ReadString(), r.ReadInt32());
							break;
						}
					case Proto.Projectile:
						ProjectileSync.ReceiveProjectile(r);
						if (host) net.Broadcast(Proto.Projectile, w => w.Write(data, 1, data.Length - 1), from);
						break;
					case Proto.Effect:
						ProjectileSync.ReceiveEffect(r);
						if (host) net.Broadcast(Proto.Effect, w => w.Write(data, 1, data.Length - 1), from);
						break;
					case Proto.Trail:
						ProjectileSync.ReceiveTrail(r);
						if (host) net.Broadcast(Proto.Trail, w => w.Write(data, 1, data.Length - 1), from);
						break;
					case Proto.PlayerDamage when !host:
						{
							uint seq = r.ReadUInt32();
							net.SendAck(from, seq);
							if (!net.Fresh(from, seq)) break;
							int dmg = r.ReadInt32();
							var d = Singleton<GlobalGameInfo>.Instance?.PlayerControllerInstance?.DamageableComponent;
							if (d == null) break;
							int before = d.GetCurrentHealth();
							Applying = true;
							try { d.TakeDamage(dmg); } finally { Applying = false; }
							var gc2 = Singleton<GlobalGameInfo>.Instance.GameControllerInstance;
							Logger.LogInfo($"[debug] PlayerDamage {dmg} applied: health {before} -> {d.GetCurrentHealth()} (immortal={d.IsImmortal} shield={gc2.ShieldPowerupEffect.activeSelf} cooldown={d.OnDamageCoolDown()} IMMORTAN={Singleton<GlobalGameInfo>.Instance.GameSettings.IMMORTAN} tag={d.gameObject.tag})");
							break;
						}
				}
			}
			catch (Exception e) { Logger.LogWarning("Bad packet: " + e.Message); }
		}

		// Host: to every client. Client: to the host (which relays to the others).
		// Host -> every client, reliably: a level change must not be lost.
		internal void SendLevelChange(string map, int seed)
		{
			if (net == null || !net.IsHost) return;
			foreach (var ep in net.Clients.Values) net.SendReliable(ep, Proto.LevelChange, w => { w.Write(map); w.Write(seed); });
		}

		internal static void NetSend(byte type, Action<BinaryWriter> body)
		{
			if (Running) Instance.net.Broadcast(type, body);
		}

		void SendSeed(IPEndPoint to)
		{
			var gc = Singleton<GlobalGameInfo>.Instance?.GameControllerInstance;
			if (gc == null) return;
			net.SendTo(to, Proto.Seed, w => w.Write(gc.RandomSeed));
		}

		internal void HostMapCreated()
		{
			if (!IsHost) return;
			foreach (var ep in net.Clients.Values) SendSeed(ep);
		}

		void SendState()
		{
			var g = Singleton<GlobalGameInfo>.Instance;
			if (g == null || g.VRCameraEye == null) return;
			var t = new[] { g.VRCameraEye.transform, g.LeftController?.transform, g.RightController?.transform };
			bool dead = g.PlayerControllerInstance != null && g.PlayerControllerInstance.DamageableComponent != null && g.PlayerControllerInstance.IsDead();
			var fake = fakeHead;
			net.Broadcast(Proto.State, w =>
			{
				w.Write(MyId);
				for (int i = 0; i < 3; i++)
				{
					if (fake.HasValue) Pose3.Write(w, fake.Value + new Vector3(i == 1 ? -0.3f : i == 2 ? 0.3f : 0f, i == 0 ? 0f : -0.4f, 0.2f), Quaternion.identity); // debug: pretend to stand here
					else Pose3.Write(w, t[i]);
				}
				w.Write(dead);
			});
		}

		// Host -> the client that owns the ghost that was hit.
		internal void SendPlayerDamage(string id, int dmg)
		{
			if (net != null && net.Clients.TryGetValue(id, out var ep))
				net.SendReliable(ep, Proto.PlayerDamage, w => w.Write(dmg));
		}

		internal bool TryEnemyIndex(Damageable d, out int i) => enemies.TryGetIndex(d, out i);
		internal Damageable EnemyAt(int i) => enemies.At(i);

		// Client -> host.
		internal void SendEnemyDamage(int index, int dmg)
		{
			if (net != null && net.HostEp != null)
				net.SendReliable(net.HostEp, Proto.EnemyDamage, w => { w.Write(index); w.Write(dmg); });
		}
	}

	struct Pose3
	{
		public Vector3 Pos; public Quaternion Rot;
		public static void Write(BinaryWriter w, Transform t)
		{
			Vector3 p = t != null ? t.position : Vector3.zero;
			Quaternion q = t != null ? t.rotation : Quaternion.identity;
			w.Write(p.x); w.Write(p.y); w.Write(p.z);
			w.Write(q.x); w.Write(q.y); w.Write(q.z); w.Write(q.w);
		}
		public static void Write(BinaryWriter w, Vector3 p, Quaternion q)
		{
			w.Write(p.x); w.Write(p.y); w.Write(p.z);
			w.Write(q.x); w.Write(q.y); w.Write(q.z); w.Write(q.w);
		}
		public static Pose3 Read(BinaryReader r) => new Pose3
		{
			Pos = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()),
			Rot = new Quaternion(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle())
		};
	}

	// Marks the hitbox of a remote player so damage dealt to it can be forwarded to its owner.
	class GhostMarker : MonoBehaviour { public string Id; }

	// Ghost of a remote player: head cube + two hand spheres + a hitbox that enemy bullets can hit.
	class Avatar
	{
		readonly GameObject[] parts = new GameObject[3];
		readonly Pose3[] target = new Pose3[3];
		readonly GameObject body;
		bool seen;
		public bool Dead;
		public Vector3 HeadPos => parts[0].transform.position;

		public Avatar(string id)
		{
			var color = Color.HSVToRGB((id.GetHashCode() & 0xFF) / 255f, 0.7f, 1f);
			for (int i = 0; i < 3; i++)
			{
				var go = GameObject.CreatePrimitive(i == 0 ? PrimitiveType.Cube : PrimitiveType.Sphere);
				UnityEngine.Object.Destroy(go.GetComponent<Collider>());
				go.transform.localScale = Vector3.one * (i == 0 ? 0.25f : 0.1f);
				go.GetComponent<Renderer>().material.color = color;
				go.name = "MP_" + id + "_" + i;
				parts[i] = go;
			}

			body = new GameObject("MP_" + id + "_body");
			int layer = LayerMask.NameToLayer("PlayerDamageCollider");
			if (layer >= 0) body.layer = layer;
			var cap = body.AddComponent<CapsuleCollider>();
			cap.height = 1.7f; cap.radius = 0.25f;
			body.AddComponent<GhostMarker>().Id = id;
			var dmg = body.AddComponent<Damageable>();
			dmg.MaxHealth = 100000;
			dmg.Revive();
		}

		public void DebugHit() => body.GetComponent<Damageable>().TakeDamage(1);

		public void SetTarget(Pose3[] p, bool dead)
		{
			Dead = dead;
			for (int i = 0; i < 3; i++) target[i] = p[i];
			if (!seen)
			{
				seen = true;
				for (int i = 0; i < 3; i++) { parts[i].transform.position = target[i].Pos; parts[i].transform.rotation = target[i].Rot; }
			}
		}

		public void Tick()
		{
			if (!seen) return;
			float k = 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime);
			for (int i = 0; i < 3; i++)
			{
				var tr = parts[i].transform;
				tr.position = Vector3.Lerp(tr.position, target[i].Pos, k);
				tr.rotation = Quaternion.Slerp(tr.rotation, target[i].Rot, k);
			}
			Vector3 h = parts[0].transform.position;
			body.transform.position = new Vector3(h.x, h.y - 0.75f, h.z);
			if (body.activeSelf == Dead) body.SetActive(!Dead);
		}

		public void Destroy()
		{
			foreach (var p in parts) if (p != null) UnityEngine.Object.Destroy(p);
			if (body != null) UnityEngine.Object.Destroy(body);
		}
	}
}
