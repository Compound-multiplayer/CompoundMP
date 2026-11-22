using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace CompoundMP
{
	// Finds hosts on the local network without typing an address: a host broadcasts a small announcement every two
	// seconds and the co-op page lists what it hears. Unity-free so it can be tested.
	public static class LanDiscovery
	{
		public const int DiscoveryPort = 27778;
		public const int AnnounceMs = 2000, ExpireMs = 6500;
		const string Magic = "CMPD";

		public class HostInfo
		{
			public string Id, Name, Address;
			public int Port, Players;
			public bool NeedsPassword;
			public DateTime Seen;
			public string Target => Address + ":" + Port;
		}

		public static byte[] Build(string id, string name, int port, int players, bool password)
		{
			var ms = new MemoryStream();
			var w = new BinaryWriter(ms);
			w.Write(Magic); w.Write(Proto.Version); w.Write(id); w.Write(name); w.Write(port); w.Write(players); w.Write(password);
			return ms.ToArray();
		}

		// Returns null for anything that is not an announcement from the same protocol version.
		public static HostInfo Parse(byte[] data, IPAddress from)
		{
			try
			{
				var r = new BinaryReader(new MemoryStream(data));
				if (r.ReadString() != Magic || r.ReadByte() != Proto.Version) return null;
				var h = new HostInfo { Id = r.ReadString(), Name = r.ReadString(), Port = r.ReadInt32(), Players = r.ReadInt32(), NeedsPassword = r.ReadBoolean() };
				if (h.Name.Length > 40) h.Name = h.Name.Substring(0, 40);
				h.Address = from.ToString(); h.Seen = DateTime.UtcNow;
				return h.Port > 0 && h.Port < 65536 ? h : null;
			}
			catch (Exception) { return null; }
		}

		public static IList<IPEndPoint> DefaultTargets(out IList<IPAddress> locals)
		{
			var targets = new List<IPEndPoint>(); locals = new List<IPAddress>();
			foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
				foreach (var ua in nic.GetIPProperties().UnicastAddresses)
				{
					if (ua.Address.AddressFamily != AddressFamily.InterNetwork || ua.IPv4Mask == null) continue;
					var a = ua.Address.GetAddressBytes(); var m = ua.IPv4Mask.GetAddressBytes();
					var b = new byte[4];
					for (int i = 0; i < 4; i++) b[i] = (byte)(a[i] | ~m[i]);
					locals.Add(ua.Address); targets.Add(new IPEndPoint(new IPAddress(b), DiscoveryPort));
				}
			}
			return targets;
		}

		public class Announcer
		{
			public IList<IPEndPoint> Targets; public IList<IPAddress> Locals;
			public Func<int> Players = () => 1;
			public string Id = Guid.NewGuid().ToString("N").Substring(0, 8), Name = "COMPOUND co-op";
			public int GamePort; public bool NeedsPassword;
			readonly ManualResetEvent stop = new ManualResetEvent(false);
			Thread thread;

			public void Start()
			{
				if (Targets == null) { Targets = DefaultTargets(out var l); Locals = l; }
				thread = new Thread(Run) { IsBackground = true, Name = "CompoundMP-LanAnnounce" };
				thread.Start();
			}

			void Run()
			{
				var socks = new List<UdpClient>();
				try
				{
					foreach (var ip in Locals ?? new List<IPAddress>())
						try { var u = new UdpClient(new IPEndPoint(ip, 0)) { EnableBroadcast = true }; socks.Add(u); } catch { }
					if (socks.Count == 0) try { socks.Add(new UdpClient { EnableBroadcast = true }); } catch { }
					do
					{
						var pkt = Build(Id, Name, GamePort, Players(), NeedsPassword);
						foreach (var s in socks) foreach (var t in Targets) try { s.Send(pkt, pkt.Length, t); } catch { }
					} while (!stop.WaitOne(AnnounceMs));
				}
				finally { foreach (var s in socks) try { s.Close(); } catch { } }
			}

			public void Stop() { stop.Set(); thread?.Join(1500); }
		}

		public class Listener
		{
			readonly Dictionary<string, HostInfo> hosts = new Dictionary<string, HostInfo>();
			readonly string ownId;
			readonly int port;
			volatile bool running = true;
			UdpClient udp;
			Thread thread;

			public Listener(string ownId, int port = DiscoveryPort) { this.ownId = ownId; this.port = port; }

			public void Start()
			{
				udp = new UdpClient { ExclusiveAddressUse = false };
				udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
				udp.Client.Bind(new IPEndPoint(IPAddress.Any, port));
				udp.Client.ReceiveTimeout = 300;
				thread = new Thread(Run) { IsBackground = true, Name = "CompoundMP-LanListen" };
				thread.Start();
			}

			void Run()
			{
				while (running)
				{
					try
					{
						var from = new IPEndPoint(IPAddress.Any, 0);
						var info = Parse(udp.Receive(ref from), from.Address);
						if (info == null || info.Id == ownId) continue;
						lock (hosts) hosts[info.Id] = info;
					}
					catch (SocketException) { }
					catch (ObjectDisposedException) { break; }
				}
				try { udp.Close(); } catch { }
			}

			// Hosts heard recently, busiest first (most players).
			public List<HostInfo> Hosts()
			{
				var cutoff = DateTime.UtcNow.AddMilliseconds(-ExpireMs);
				lock (hosts)
				{
					foreach (var k in hosts.Where(kv => kv.Value.Seen < cutoff).Select(kv => kv.Key).ToList()) hosts.Remove(k);
					return hosts.Values.OrderByDescending(h => h.Players).ThenBy(h => h.Name).ToList();
				}
			}

			public void Stop() { running = false; thread?.Join(1500); }
		}
	}
}
