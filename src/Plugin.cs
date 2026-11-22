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
	[BepInPlugin("com.compoundmp.coop", "COMPOUND Multiplayer", "0.3.0")]
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
		ConfigEntry<string> joinAddress;
		ConfigEntry<int> port;
		ConfigEntry<string> autoStart, debugLoadMap;
		ConfigEntry<bool> debugCombat, debugDumpMenu;
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
			joinAddress = Config.Bind("Network", "JoinAddress", "127.0.0.1", "Host IP to join");
			port = Config.Bind("Network", "Port", 27777, "UDP port (host must forward/open it)");
			autoStart = Config.Bind("Debug", "AutoStart", "", "'host' or 'join' to start a session on launch (headless testing)");
			debugLoadMap = Config.Bind("Debug", "LoadMap", "", "Scene to load a few seconds after launch (headless testing)");
			shotDir = Config.Bind("Debug", "ScreenshotDir", "", "Save PNG screenshots here (development; needs graphics)");
			Shot.Dir = shotDir.Value;
			debugDumpMenu = Config.Bind("Debug", "DumpMenu", false, "Log the game's VR menu hierarchy (development)");
			debugCombat = Config.Bind("Debug", "Combat", false, "Run a scripted enemy/damage sync check once enemies exist (headless testing)");
			autoStartMode = autoStart.Value;
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
			if (net == null) return;

			while (net.TryDequeue(out var from, out var data)) Handle(from, data);

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

			statusTimer += Time.unscaledDeltaTime;
			if (statusTimer >= 5f)
			{
				statusTimer = 0f;
				Logger.LogInfo($"[status] {(net.IsHost ? "host" : "client")} peers={(net.IsHost ? net.Clients.Count : Avatars.Count)} avatars={Avatars.Count} seed={(HasHostSeed ? HostSeed.ToString() : "-")} {(upnp != null ? upnp.Status + " " : "")}{ProjectileSync.Stats()} map={EnemySync.MapName} {enemies.Summary()}");
			}
		}

		void OnDestroy() => Stop();

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
		internal string JoinTarget { get => joinAddress.Value; set => joinAddress.Value = value; }
		internal string StatusLine()
		{
			if (net == null) return menuNote != "" ? menuNote : "Not connected";
			if (net.IsHost) return $"Hosting on port {port.Value} - {net.Clients.Count} player(s) connected";
			return HasHostSeed ? "Joined - synced with host" : "Joining " + net.HostEp + "...";
		}
		internal string NetLine()
		{
			if (net == null || !net.IsHost) return net == null ? "Return to HQ and start a new run once connected" : "Start a new run from HQ to play together";
			if (upnp == null) return "UPnP off - forward UDP " + port.Value + " or use a VPN";
			return upnp.ExternalIp != null ? "Friends join: " + upnp.ExternalIp + (port.Value != 27777 ? ":" + port.Value : "") : upnp.Status;
		}

		// ---- session control ----

		internal void StartHost()
		{
			Stop();
			menuNote = "";
			try
			{
				net = new Transport(true, port.Value);
				Logger.LogInfo("Hosting on UDP " + port.Value);
				Toast("Hosting co-op on port " + port.Value);
				if (useUpnp.Value)
				{
					upnp = new Upnp { Log = m => Logger.LogInfo(m) };
					upnp.Start(port.Value, "COMPOUND co-op");
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
				net = new Transport(false, 0);
				net.HostEp = new IPEndPoint(addr, hostPort);
				net.SendTo(net.HostEp, Proto.Hello, w => w.Write(MyId));
				Logger.LogInfo("Joining " + net.HostEp);
				Toast("Joining " + net.HostEp + " - return to HQ and start a new run");
			}
			catch (Exception e) { Logger.LogError(e); menuNote = "Could not join: " + e.Message; Toast(menuNote); net?.Close(); net = null; }
		}

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
			if (net == null) return;
			try { net.Broadcast(Proto.Bye, w => w.Write(MyId)); } catch { }
			net.Close();
			net = null;
			HasHostSeed = false;
			foreach (var a in Avatars.Values) a.Destroy();
			Avatars.Clear();
			enemies.Reset();
			Logger.LogInfo("Session stopped");
		}

		void Toast(string s)
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
							string id = r.ReadString();
							net.Clients[id] = from;
							Logger.LogInfo("Client joined: " + id);
							Toast("Player joined");
							SendSeed(from);
							break;
						}
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
							var pose = new Pose3[3];
							for (int i = 0; i < 3; i++) pose[i] = Pose3.Read(r);
							bool dead = r.ReadBoolean();
							if (!Avatars.TryGetValue(id, out var av)) Avatars[id] = av = new Avatar(id);
							av.SetTarget(pose, dead);
							if (host) net.Broadcast(Proto.State, w => w.Write(data, 1, data.Length - 1), from); // relay to the other clients
							break;
						}
					case Proto.Bye:
						{
							string id = r.ReadString();
							if (Avatars.TryGetValue(id, out var av)) { av.Destroy(); Avatars.Remove(id); }
							net.Clients.Remove(id);
							break;
						}
					case Proto.Enemies when !host:
						enemies.ClientReceive(r);
						break;
					case Proto.EnemyDamage when host:
						enemies.HostApplyDamage(r.ReadInt32(), r.ReadInt32());
						break;
					case Proto.Projectile:
						ProjectileSync.ReceiveProjectile(r);
						if (host) net.Broadcast(Proto.Projectile, w => w.Write(data, 1, data.Length - 1), from);
						break;
					case Proto.Trail:
						ProjectileSync.ReceiveTrail(r);
						if (host) net.Broadcast(Proto.Trail, w => w.Write(data, 1, data.Length - 1), from);
						break;
					case Proto.PlayerDamage when !host:
						{
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
				net.SendTo(ep, Proto.PlayerDamage, w => w.Write(dmg));
		}

		internal bool TryEnemyIndex(Damageable d, out int i) => enemies.TryGetIndex(d, out i);

		// Client -> host.
		internal void SendEnemyDamage(int index, int dmg)
		{
			if (net != null && net.HostEp != null)
				net.SendTo(net.HostEp, Proto.EnemyDamage, w => { w.Write(index); w.Write(dmg); });
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
