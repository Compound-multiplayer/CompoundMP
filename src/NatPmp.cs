using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace CompoundMP
{
	// NAT-PMP (RFC 6886) client: the fallback when a router has UPnP switched off. Unity-free so it can be tested.
	public class NatPmp
	{
		public IPEndPoint Gateway;
		public int TimeoutMs = 400, Tries = 3;
		public string ExternalIp;

		public static IPAddress DefaultGateway()
		{
			foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
				foreach (var g in nic.GetIPProperties().GatewayAddresses)
					if (g.Address.AddressFamily == AddressFamily.InterNetwork && !g.Address.Equals(IPAddress.Any)) return g.Address;
			}
			return null;
		}

		byte[] Exchange(byte[] request, int expectedOp)
		{
			if (Gateway == null)
			{
				var gw = DefaultGateway();
				if (gw == null) return null;
				Gateway = new IPEndPoint(gw, 5351);
			}
			using (var udp = new UdpClient())
			{
				udp.Client.ReceiveTimeout = TimeoutMs;
				for (int i = 0; i < Tries; i++)
				{
					try
					{
						udp.Send(request, request.Length, Gateway);
						var from = new IPEndPoint(IPAddress.Any, 0);
						var resp = udp.Receive(ref from);
						if (resp.Length >= 8 && resp[0] == 0 && resp[1] == expectedOp) return resp;
					}
					catch (SocketException) { } // timeout: try again
				}
			}
			return null;
		}

		static int ResultCode(byte[] r) => (r[2] << 8) | r[3];

		// Asks the gateway for its public address (and learns whether it speaks NAT-PMP at all).
		public bool QueryExternalAddress()
		{
			var r = Exchange(new byte[] { 0, 0 }, 128);
			if (r == null || r.Length < 12 || ResultCode(r) != 0) return false;
			ExternalIp = new IPAddress(new[] { r[8], r[9], r[10], r[11] }).ToString();
			return true;
		}

		static byte[] MapRequest(int port, int lifetime)
		{
			return new byte[]
			{
				0, 1, 0, 0,
				(byte)(port >> 8), (byte)port,
				(byte)((lifetime == 0 ? 0 : port) >> 8), (byte)(lifetime == 0 ? 0 : port),
				(byte)(lifetime >> 24), (byte)(lifetime >> 16), (byte)(lifetime >> 8), (byte)lifetime,
			};
		}

		// Maps UDP `port` on the gateway to this machine for `lifetime` seconds. Returns the lifetime the router granted, or 0 on failure.
		public int Map(int port, int lifetime)
		{
			var r = Exchange(MapRequest(port, lifetime), 129);
			if (r == null || r.Length < 16 || ResultCode(r) != 0) return 0;
			return (r[12] << 24) | (r[13] << 16) | (r[14] << 8) | r[15];
		}

		public void Unmap(int port) => Exchange(MapRequest(port, 0), 129);
	}
}
