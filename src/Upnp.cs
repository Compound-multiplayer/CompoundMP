using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;

namespace CompoundMP
{
	// Minimal UPnP IGD client (SSDP discovery + SOAP AddPortMapping) so a host behind a home router can open its UDP port
	// without manual forwarding. Kept free of UnityEngine so it can be tested outside the game. Runs on its own thread;
	// the UI reads Status / ExternalIp.
	public class Upnp
	{
		static readonly string[] SearchTargets =
		{
			"urn:schemas-upnp-org:device:InternetGatewayDevice:1",
			"urn:schemas-upnp-org:service:WANIPConnection:1",
			"urn:schemas-upnp-org:service:WANPPPConnection:1",
			"upnp:rootdevice",
		};

		// Overridable so tests can point discovery at a fake gateway.
		public IPEndPoint SsdpEndpoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);
		public Func<IList<IPAddress>> LocalAddresses = DefaultLocalAddresses;
		public int DiscoverMs = 2500;
		public Action<string> Log = s => { };

		public volatile string Status = "UPnP: starting";
		public volatile string ExternalIp;
		public volatile bool Mapped;
		public NatPmp Pmp = new NatPmp();
		bool usingPmp;
		public int Port { get; private set; }

		readonly ManualResetEvent stop = new ManualResetEvent(false);
		Thread thread;
		string controlUrl, serviceType, localIp;
		int leaseSeconds;

		public void Start(int port, string description)
		{
			Port = port;
			thread = new Thread(() => Run(description)) { IsBackground = true, Name = "CompoundMP-UPnP" };
			thread.Start();
		}

		// Removes the mapping (if we made one) and ends the thread.
		public void Stop()
		{
			stop.Set();
			if (thread != null && !thread.Join(4000)) Log("UPnP thread did not exit in time");
		}

		void Run(string description)
		{
			try
			{
				if (!FindGateway(description))
				{
					bool upnpFailed = Status.StartsWith("UPnP: failed");
					if (stop.WaitOne(0) || !TryNatPmp()) { Status = upnpFailed ? Status : "UPnP / NAT-PMP: no gateway found"; return; }
				}
				while (!stop.WaitOne(leaseSeconds > 0 ? leaseSeconds * 500 : Timeout.Infinite))
				{
					// Lease-limited router: renew at half-life.
					try { if (usingPmp) Pmp.Map(Port, leaseSeconds); else AddMapping(description, leaseSeconds); }
					catch (Exception e) { Log("port mapping renew failed: " + e.Message); }
				}
			}
			catch (Exception e) { Status = "UPnP: failed (" + e.Message + ")"; Log("UPnP: " + e); }
			finally
			{
				if (Mapped)
				{
					try
					{
						if (usingPmp) { Pmp.Unmap(Port); Log("NAT-PMP mapping removed"); }
						else { SoapCall("DeletePortMapping", "<NewRemoteHost></NewRemoteHost><NewExternalPort>" + Port + "</NewExternalPort><NewProtocol>UDP</NewProtocol>"); Log("UPnP mapping removed"); }
					}
					catch (Exception e) { Log("unmap failed: " + e.Message); }
					Mapped = false;
				}
			}
		}

		// Routers with UPnP switched off often still speak NAT-PMP.
		bool TryNatPmp()
		{
			Status = "NAT-PMP: asking the router";
			if (!Pmp.QueryExternalAddress()) return false;
			int granted = Pmp.Map(Port, 7200);
			if (granted == 0) { Status = "NAT-PMP: the router refused the mapping"; return false; }
			usingPmp = true; leaseSeconds = granted; Mapped = true; ExternalIp = Pmp.ExternalIp;
			Status = "NAT-PMP: port " + Port + " open, public IP " + ExternalIp + Describe(ExternalIp);
			return true;
		}

		// Why friends might not reach a host even though a mapping exists.
		public static string Describe(string ip)
		{
			if (ip == null) return "";
			if (IsCgnat(ip)) return " (carrier-grade NAT: friends cannot reach you; use a VPN such as Tailscale)";
			if (IsPrivate(ip)) return " (behind another router; internet play may fail)";
			return "";
		}
		bool FindGateway(string description)
		{
			Status = "UPnP: searching for router";
			var seen = new HashSet<string>();
			foreach (var location in Discover())
			{
				if (!seen.Add(location)) continue;
				if (stop.WaitOne(0)) return false;
				try
				{
					foreach (var svc in ParseServices(Fetch(location), location))
					{
						controlUrl = svc.Key; serviceType = svc.Value;
						localIp = LocalIpFor(new Uri(controlUrl).Host);
						Log($"UPnP gateway {controlUrl} ({serviceType}) local {localIp}");
						if (TryMap(description)) return true;
					}
				}
				catch (Exception e) { Log("UPnP candidate " + location + " failed: " + e.Message); }
			}
			return false;
		}

		bool TryMap(string description)
		{
			// Prefer a leased mapping (renewed while running) so a crash cannot leave a port open forever; some routers
			// only allow permanent ones (error 725), so fall back to that.
			foreach (int lease in new[] { 7200, 0 })
			{
				try
				{
					AddMapping(description, lease);
					leaseSeconds = lease;
					Mapped = true;
					try { ExternalIp = ParseTag(SoapCall("GetExternalIPAddress", ""), "NewExternalIPAddress"); } catch (Exception e) { Log("GetExternalIPAddress failed: " + e.Message); }
					Status = "UPnP: port " + Port + " open" + (ExternalIp != null ? ", public IP " + ExternalIp : "");
					Status += Describe(ExternalIp);
					return true;
				}
				catch (UpnpError e)
				{
					Log($"UPnP AddPortMapping lease {lease} refused: {e.Code} {e.Message}");
					Status = "UPnP: failed (router said " + e.Code + " " + e.Message + ")";
					if (e.Code != 725 && e.Code != 402) break; // only lease/arg problems are worth retrying
				}
			}
			return false;
		}

		void AddMapping(string description, int lease)
		{
			SoapCall("AddPortMapping",
				"<NewRemoteHost></NewRemoteHost><NewExternalPort>" + Port + "</NewExternalPort><NewProtocol>UDP</NewProtocol>" +
				"<NewInternalPort>" + Port + "</NewInternalPort><NewInternalClient>" + localIp + "</NewInternalClient>" +
				"<NewEnabled>1</NewEnabled><NewPortMappingDescription>" + System.Security.SecurityElement.Escape(description) +
				"</NewPortMappingDescription><NewLeaseDuration>" + lease + "</NewLeaseDuration>");
		}

		// ---- SSDP ----

		IEnumerable<string> Discover()
		{
			var found = new List<string>();
			var socks = new List<UdpClient>();
			try
			{
				foreach (var ip in LocalAddresses())
				{
					try { socks.Add(new UdpClient(new IPEndPoint(ip, 0))); } catch { }
				}
				foreach (var s in socks)
					foreach (var st in SearchTargets)
					{
						string req = "M-SEARCH * HTTP/1.1\r\nHOST: 239.255.255.250:1900\r\nMAN: \"ssdp:discover\"\r\nMX: 2\r\nST: " + st + "\r\n\r\n";
						byte[] b = Encoding.ASCII.GetBytes(req);
						try { s.Send(b, b.Length, SsdpEndpoint); } catch { }
					}
				var end = DateTime.UtcNow.AddMilliseconds(DiscoverMs);
				while (DateTime.UtcNow < end && !stop.WaitOne(0))
				{
					bool any = false;
					foreach (var s in socks)
					{
						while (s.Available > 0)
						{
							any = true;
							var from = new IPEndPoint(IPAddress.Any, 0);
							string text = Encoding.ASCII.GetString(s.Receive(ref from));
							foreach (var line in text.Split('\n'))
								if (line.Trim().StartsWith("LOCATION:", StringComparison.OrdinalIgnoreCase))
								{
									string loc = line.Substring(line.IndexOf(':') + 1).Trim();
									if (!found.Contains(loc)) { found.Add(loc); }
								}
						}
					}
					if (!any) Thread.Sleep(50);
				}
			}
			finally { foreach (var s in socks) try { s.Close(); } catch { } }
			return found;
		}

		static IList<IPAddress> DefaultLocalAddresses()
		{
			var list = new List<IPAddress>();
			foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
				foreach (var ua in nic.GetIPProperties().UnicastAddresses)
					if (ua.Address.AddressFamily == AddressFamily.InterNetwork && !ua.Address.ToString().StartsWith("169.254."))
						list.Add(ua.Address);
			}
			if (list.Count == 0) list.Add(IPAddress.Any);
			return list;
		}

		static string LocalIpFor(string remoteHost)
		{
			using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
			{
				s.Connect(new IPEndPoint(IPAddress.Parse(remoteHost), 9));
				return ((IPEndPoint)s.LocalEndPoint).Address.ToString();
			}
		}

		// ---- device description / SOAP ----

		static string Fetch(string url)
		{
			var req = (HttpWebRequest)WebRequest.Create(url);
			req.Timeout = 3000;
			using (var resp = req.GetResponse())
			using (var r = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
				return r.ReadToEnd();
		}

		// Returns (absolute controlURL, serviceType) for each WAN connection service in a device description.
		public static List<KeyValuePair<string, string>> ParseServices(string xml, string location)
		{
			var doc = new XmlDocument();
			doc.LoadXml(xml);
			string baseUrl = location;
			var ub = doc.SelectSingleNode("//*[local-name()='URLBase']");
			if (ub != null && ub.InnerText.Trim().Length > 0) baseUrl = ub.InnerText.Trim();
			var res = new List<KeyValuePair<string, string>>();
			foreach (XmlNode svc in doc.SelectNodes("//*[local-name()='service']"))
			{
				string type = svc.SelectSingleNode("*[local-name()='serviceType']")?.InnerText.Trim();
				string ctl = svc.SelectSingleNode("*[local-name()='controlURL']")?.InnerText.Trim();
				if (type == null || ctl == null) continue;
				if (!type.Contains(":WANIPConnection:") && !type.Contains(":WANPPPConnection:")) continue;
				res.Add(new KeyValuePair<string, string>(new Uri(new Uri(baseUrl), ctl).ToString(), type));
			}
			return res;
		}

		string SoapCall(string action, string args)
		{
			string body = "<?xml version=\"1.0\"?><s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\"><s:Body><u:" +
				action + " xmlns:u=\"" + serviceType + "\">" + args + "</u:" + action + "></s:Body></s:Envelope>";
			byte[] data = Encoding.UTF8.GetBytes(body);
			var req = (HttpWebRequest)WebRequest.Create(controlUrl);
			req.Method = "POST";
			req.Timeout = 4000;
			req.ContentType = "text/xml; charset=\"utf-8\"";
			req.Headers.Add("SOAPAction", "\"" + serviceType + "#" + action + "\"");
			req.ContentLength = data.Length;
			using (var s = req.GetRequestStream()) s.Write(data, 0, data.Length);
			try
			{
				using (var resp = req.GetResponse())
				using (var r = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
					return r.ReadToEnd();
			}
			catch (WebException e) when (e.Response != null)
			{
				string text;
				using (var r = new StreamReader(e.Response.GetResponseStream(), Encoding.UTF8)) text = r.ReadToEnd();
				int.TryParse(ParseTag(text, "errorCode"), out int code);
				throw new UpnpError(code, ParseTag(text, "errorDescription") ?? e.Message);
			}
		}

		static string ParseTag(string xml, string tag)
		{
			var doc = new XmlDocument();
			doc.LoadXml(xml);
			return doc.SelectSingleNode("//*[local-name()='" + tag + "']")?.InnerText.Trim();
		}

		// 100.64.0.0/10: shared address space used by ISPs for carrier-grade NAT.
		public static bool IsCgnat(string ip)
		{
			if (!IPAddress.TryParse(ip, out var a)) return false;
			var b = a.GetAddressBytes();
			return b.Length == 4 && b[0] == 100 && b[1] >= 64 && b[1] <= 127;
		}

		public static bool IsPrivate(string ip)
		{
			if (!IPAddress.TryParse(ip, out var a)) return false;
			var b = a.GetAddressBytes();
			if (b.Length != 4) return false;
			return b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168)
				|| (b[0] == 100 && b[1] >= 64 && b[1] <= 127) || b[0] == 0;
		}

		class UpnpError : Exception
		{
			public readonly int Code;
			public UpnpError(int code, string msg) : base(msg) { Code = code; }
		}
	}
}
