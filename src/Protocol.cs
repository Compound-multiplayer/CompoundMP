using System;
using System.Collections.Generic;
using System.IO;

namespace CompoundMP
{
	// Wire format. Kept free of UnityEngine so it can be tested outside the game.
	public static class Proto
	{
		public const byte Hello = 1, Seed = 2, State = 3, Bye = 4, Enemies = 5, EnemyDamage = 6, PlayerDamage = 7, Projectile = 8, Trail = 9, Ack = 10, Welcome = 11, Reject = 12, Effect = 13, LevelChange = 14;
		// Bumped whenever the handshake or packet layout changes; a host rejects a different version.
		public const byte Version = 2;
		// Handshake packets are the only ones sent before a session key exists, so they are never tagged.
		public static bool IsHandshake(byte type) => type == Hello || type == Welcome || type == Reject;
		public const int EnemiesPerPacket = 40;

		public struct EnemyRec
		{
			public bool Alive;
			public float X, Y, Z, Yaw;
		}

		public static void WriteEnemies(BinaryWriter w, string map, int total, int start, IList<EnemyRec> recs, int count)
		{
			w.Write(map);
			w.Write(total);
			w.Write(start);
			w.Write((byte)count);
			for (int i = 0; i < count; i++)
			{
				var e = recs[start + i];
				w.Write(e.Alive);
				w.Write(e.X); w.Write(e.Y); w.Write(e.Z); w.Write(e.Yaw);
			}
		}

		public static EnemyRec[] ReadEnemies(BinaryReader r, out string map, out int total, out int start)
		{
			map = r.ReadString();
			total = r.ReadInt32();
			start = r.ReadInt32();
			int n = r.ReadByte();
			var recs = new EnemyRec[n];
			for (int i = 0; i < n; i++)
				recs[i] = new EnemyRec { Alive = r.ReadBoolean(), X = r.ReadSingle(), Y = r.ReadSingle(), Z = r.ReadSingle(), Yaw = r.ReadSingle() };
			return recs;
		}
	}

	// Password-based mutual authentication and per-packet tags. The password never crosses the wire: both sides prove they
	// know it with an HMAC over fresh nonces, then derive a session key that tags every later packet (HMAC-SHA256,
	// truncated to 16 bytes). Authentication only: poses and damage are not secret, but nobody may inject or alter them.
	public static class Auth
	{
		public const int NonceLen = 16, TagLen = 16;

		public static byte[] PasswordKey(string password)
		{
			using (var kdf = new System.Security.Cryptography.Rfc2898DeriveBytes(password, System.Text.Encoding.UTF8.GetBytes("compoundmp.v2"), 5000))
				return kdf.GetBytes(32);
		}

		public static byte[] Nonce()
		{
			var n = new byte[NonceLen];
			using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create()) rng.GetBytes(n);
			return n;
		}

		public static byte[] Hmac(byte[] key, params byte[][] parts)
		{
			using (var h = new System.Security.Cryptography.HMACSHA256(key))
			{
				var ms = new MemoryStream();
				foreach (var p in parts) ms.Write(p, 0, p.Length);
				return h.ComputeHash(ms.ToArray());
			}
		}

		static byte[] Utf8(string s) => System.Text.Encoding.UTF8.GetBytes(s);

		public static byte[] HelloProof(byte[] pwKey, string id, byte[] clientNonce) => Hmac(pwKey, Utf8("hello"), Utf8(id), clientNonce);
		public static byte[] WelcomeProof(byte[] pwKey, byte[] clientNonce, byte[] hostNonce) => Hmac(pwKey, Utf8("welcome"), clientNonce, hostNonce);
		public static byte[] SessionKey(byte[] pwKey, byte[] clientNonce, byte[] hostNonce) => Hmac(pwKey, Utf8("session"), clientNonce, hostNonce);

		public static byte[] Tag(byte[] key, byte[] data, int length)
		{
			var buf = new byte[length];
			Array.Copy(data, buf, length);
			var full = Hmac(key, buf);
			var tag = new byte[TagLen];
			Array.Copy(full, tag, TagLen);
			return tag;
		}

		public static bool Equal(byte[] a, byte[] b)
		{
			if (a == null || b == null || a.Length != b.Length) return false;
			int diff = 0;
			for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
			return diff == 0;
		}
	}

	// UDP transport: one host, many clients. Packets are [type byte][body].
	public class Transport
	{
		readonly System.Net.Sockets.UdpClient udp;
		readonly System.Threading.Thread rx;
		readonly System.Collections.Concurrent.ConcurrentQueue<KeyValuePair<System.Net.IPEndPoint, byte[]>> inbox =
			new System.Collections.Concurrent.ConcurrentQueue<KeyValuePair<System.Net.IPEndPoint, byte[]>>();
		volatile bool running = true;

		public readonly bool IsHost;
		public readonly Dictionary<string, System.Net.IPEndPoint> Clients = new Dictionary<string, System.Net.IPEndPoint>();
		public System.Net.IPEndPoint HostEp;
		public int LocalPort => ((System.Net.IPEndPoint)udp.Client.LocalEndPoint).Port;

		// With AuthEnabled every packet except the handshake carries a 16-byte tag made with the peer's session key; packets
		// with a missing or wrong tag are dropped by the receive thread and counted in RejectedPackets.
		public readonly bool AuthEnabled;
		readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte[]> keys = new System.Collections.Concurrent.ConcurrentDictionary<string, byte[]>();
		public long RejectedPackets;
		public void SetKey(System.Net.IPEndPoint ep, byte[] key) => keys[ep.ToString()] = key;
		public void RemoveKey(System.Net.IPEndPoint ep) { keys.TryRemove(ep.ToString(), out _); }

		// Traffic counters per packet type (index = type byte), read by the status line.
		public readonly long[] SentBytes = new long[256], RecvBytes = new long[256], SentPackets = new long[256], RecvPackets = new long[256];
		// Test hook: percentage (0-100) of outgoing packets to drop, to see how the game behaves on a lossy link.
		public int SimulatedLossPercent;
		readonly Random lossRng = new Random();

		public Transport(bool host, int port, bool auth = false)
		{
			IsHost = host;
			AuthEnabled = auth;
			udp = new System.Net.Sockets.UdpClient(host ? port : 0);
			udp.Client.ReceiveTimeout = 200; // lets the rx thread notice shutdown without Close() blocking on Receive
			rx = new System.Threading.Thread(RxLoop) { IsBackground = true };
			rx.Start();
		}

		void RxLoop()
		{
			while (running)
			{
				try
				{
					var from = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);
						byte[] data = udp.Receive(ref from);
						if (data.Length == 0) continue;
						if (AuthEnabled && !Proto.IsHandshake(data[0]))
						{
							if (data.Length <= Auth.TagLen || !keys.TryGetValue(from.ToString(), out var key)) { System.Threading.Interlocked.Increment(ref RejectedPackets); continue; }
							int bodyLen = data.Length - Auth.TagLen;
							var expect = Auth.Tag(key, data, bodyLen);
							var got = new byte[Auth.TagLen];
							Array.Copy(data, bodyLen, got, 0, Auth.TagLen);
							if (!Auth.Equal(expect, got)) { System.Threading.Interlocked.Increment(ref RejectedPackets); continue; }
							Array.Resize(ref data, bodyLen);
						}
						RecvBytes[data[0]] += data.Length; RecvPackets[data[0]]++;
						inbox.Enqueue(new KeyValuePair<System.Net.IPEndPoint, byte[]>(from, data));
				}
				catch (System.Net.Sockets.SocketException) { } // timeout, or ICMP port unreachable on Windows
				catch (ObjectDisposedException) { break; }
			}
			try { udp.Close(); } catch { }
		}

		public bool TryDequeue(out System.Net.IPEndPoint from, out byte[] data)
		{
			if (inbox.TryDequeue(out var kv)) { from = kv.Key; data = kv.Value; return true; }
			from = null; data = null; return false;
		}

		public static byte[] Build(byte type, Action<BinaryWriter> body)
		{
			var ms = new MemoryStream();
			var w = new BinaryWriter(ms);
			w.Write(type);
			body(w);
			return ms.ToArray();
		}

		// Test hook: return true to silently drop an outgoing packet (simulates packet loss).
		public Func<byte[], bool> DropFilter;

		public void SendRaw(System.Net.IPEndPoint ep, byte[] b)
		{
			if (!running || ep == null || b.Length == 0 || (DropFilter != null && DropFilter(b))) return;
			if (SimulatedLossPercent > 0 && lossRng.Next(100) < SimulatedLossPercent) return;
			SentBytes[b[0]] += b.Length; SentPackets[b[0]]++;
			if (AuthEnabled && !Proto.IsHandshake(b[0]))
			{
				if (!keys.TryGetValue(ep.ToString(), out var key)) return; // no session with that peer yet
				var tag = Auth.Tag(key, b, b.Length);
				var signed = new byte[b.Length + Auth.TagLen];
				Array.Copy(b, signed, b.Length);
				Array.Copy(tag, 0, signed, b.Length, Auth.TagLen);
				b = signed;
			}
			try { udp.Send(b, b.Length, ep); } catch (ObjectDisposedException) { }
		}

		public void SendTo(System.Net.IPEndPoint ep, byte type, Action<BinaryWriter> body) => SendRaw(ep, Build(type, body));

		// ---- reliable delivery for events that must not be lost (damage) ----
		// The packet carries a sequence number right after the type byte; the receiver answers with Proto.Ack and drops
		// duplicates (Fresh). Unacknowledged packets are re-sent every ResendMs, up to MaxTries times. Main thread only.
		public const int ResendMs = 200, MaxTries = 10;
		class Pending { public System.Net.IPEndPoint Ep; public byte[] Data; public long NextMs; public int Tries; }
		readonly Dictionary<uint, Pending> pending = new Dictionary<uint, Pending>();
		readonly Dictionary<string, HashSet<uint>> seen = new Dictionary<string, HashSet<uint>>();
		readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
		uint nextSeq = 1;
		public int PendingCount => pending.Count;

		public void SendReliable(System.Net.IPEndPoint ep, byte type, Action<BinaryWriter> body)
		{
			uint seq = nextSeq++;
			byte[] data = Build(type, w => { w.Write(seq); body(w); });
			pending[seq] = new Pending { Ep = ep, Data = data, NextMs = clock.ElapsedMilliseconds + ResendMs, Tries = 1 };
			SendRaw(ep, data);
		}

		public void Ack(uint seq) => pending.Remove(seq);

		public void SendAck(System.Net.IPEndPoint to, uint seq) => SendTo(to, Proto.Ack, w => w.Write(seq));

		// True the first time (sender, seq) is seen; the caller still acks duplicates so the sender stops resending.
		public bool Fresh(System.Net.IPEndPoint from, uint seq)
		{
			string key = from.ToString();
			if (!seen.TryGetValue(key, out var set)) seen[key] = set = new HashSet<uint>();
			if (set.Count > 1024) set.Clear();
			return set.Add(seq);
		}

		// Call every frame: re-sends what is still unacknowledged.
		public void Pump()
		{
			if (pending.Count == 0) return;
			long now = clock.ElapsedMilliseconds;
			List<uint> drop = null;
			foreach (var kv in pending)
			{
				var p = kv.Value;
				if (now < p.NextMs) continue;
				if (p.Tries >= MaxTries) { (drop ?? (drop = new List<uint>())).Add(kv.Key); continue; }
				p.Tries++; p.NextMs = now + ResendMs;
				SendRaw(p.Ep, p.Data);
			}
			if (drop != null) foreach (var k in drop) pending.Remove(k);
		}

		// Host: every client except `except`. Client: the host.
		public void Broadcast(byte type, Action<BinaryWriter> body, System.Net.IPEndPoint except = null)
		{
			byte[] b = Build(type, body);
			if (IsHost)
			{
				foreach (var ep in Clients.Values)
					if (except == null || !ep.Equals(except)) SendRaw(ep, b);
			}
			else SendRaw(HostEp, b);
		}

		public void Close()
		{
			running = false; // the rx thread closes the socket
		}
	}
}
