using System;
using System.Collections.Generic;
using System.IO;

namespace CompoundMP
{
	// Wire format. Kept free of UnityEngine so it can be tested outside the game.
	public static class Proto
	{
		public const byte Hello = 1, Seed = 2, State = 3, Bye = 4, Enemies = 5, EnemyDamage = 6, PlayerDamage = 7, Projectile = 8, Trail = 9, Ack = 10, Welcome = 11, Reject = 12;
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

		public Transport(bool host, int port)
		{
			IsHost = host;
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
					if (data.Length > 0) inbox.Enqueue(new KeyValuePair<System.Net.IPEndPoint, byte[]>(from, data));
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
			if (!running || ep == null || (DropFilter != null && DropFilter(b))) return;
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
