using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using CompoundMP;

int fails = 0;
void Check(bool ok, string what) { Console.WriteLine((ok ? "PASS " : "FAIL ") + what); if (!ok) fails++; }
byte[] Wait(Transport t, out IPEndPoint from, int ms = 2000)
{
	var end = DateTime.UtcNow.AddMilliseconds(ms);
	while (DateTime.UtcNow < end) { if (t.TryDequeue(out from, out var d)) return d; Thread.Sleep(5); }
	from = null; return null;
}

// Enemy batch round trip + chunking
var recs = Enumerable.Range(0, 95).Select(i => new Proto.EnemyRec { Alive = i % 3 != 0, X = i, Y = 0.5f, Z = -i, Yaw = i * 2 }).ToArray();
var got = new Proto.EnemyRec[95]; int received = 0;
for (int s = 0; s < 95; s += Proto.EnemiesPerPacket)
{
	int n = Math.Min(Proto.EnemiesPerPacket, 95 - s), st = s;
	byte[] pkt = Transport.Build(Proto.Enemies, w => Proto.WriteEnemies(w, "map01_sewers", 95, st, recs, n));
	Check(pkt.Length < 1200, $"enemy packet fits MTU ({pkt.Length} bytes)");
	var r = new BinaryReader(new MemoryStream(pkt)); r.ReadByte();
	var rr = Proto.ReadEnemies(r, out var map, out var total, out var start);
	Check(map == "map01_sewers" && total == 95 && start == s, "enemy header round-trips");
	for (int i = 0; i < rr.Length; i++) { got[start + i] = rr[i]; received++; }
}
Check(received == 95 && got.Zip(recs, (a, b) => a.Alive == b.Alive && a.X == b.X && a.Z == b.Z && a.Yaw == b.Yaw).All(x => x), "all 95 enemies reassembled intact");

// Host + two clients over real UDP on loopback
var host = new Transport(true, 0);
int port = host.LocalPort;
var c1 = new Transport(false, 0); c1.HostEp = new IPEndPoint(IPAddress.Loopback, port);
var c2 = new Transport(false, 0); c2.HostEp = new IPEndPoint(IPAddress.Loopback, port);
c1.SendTo(c1.HostEp, Proto.Hello, w => w.Write("aaaa"));
c2.SendTo(c2.HostEp, Proto.Hello, w => w.Write("bbbb"));
for (int i = 0; i < 2; i++)
{
	var d = Wait(host, out var from);
	var r = new BinaryReader(new MemoryStream(d));
	Check(d != null && r.ReadByte() == Proto.Hello, "host receives Hello");
	host.Clients[r.ReadString()] = from;
}
Check(host.Clients.Count == 2, "host registered both clients");

host.SendTo(host.Clients["aaaa"], Proto.Seed, w => w.Write(123456));
var sd = Wait(c1, out _); var sr = new BinaryReader(new MemoryStream(sd));
Check(sr.ReadByte() == Proto.Seed && sr.ReadInt32() == 123456, "client gets host seed");

// client state -> host -> relayed to the other client only
c1.Broadcast(Proto.State, w => { w.Write("aaaa"); w.Write(1.5f); });
var st1 = Wait(host, out var fromA);
host.Broadcast(Proto.State, w => w.Write(st1, 1, st1.Length - 1), fromA);
var rel = Wait(c2, out _);
var rr2 = new BinaryReader(new MemoryStream(rel));
Check(rel != null && rr2.ReadByte() == Proto.State && rr2.ReadString() == "aaaa" && rr2.ReadSingle() == 1.5f, "state relayed to other client");
Check(Wait(c1, out _, 300) == null, "sender does not get its own state echoed");

// client damage to enemy -> host
c2.SendTo(c2.HostEp, Proto.EnemyDamage, w => { w.Write(7); w.Write(3); });
var dd = Wait(host, out _); var dr = new BinaryReader(new MemoryStream(dd));
Check(dr.ReadByte() == Proto.EnemyDamage && dr.ReadInt32() == 7 && dr.ReadInt32() == 3, "enemy damage reaches host");

// host damage to a player -> that client only
host.SendTo(host.Clients["bbbb"], Proto.PlayerDamage, w => w.Write(2));
var pd = Wait(c2, out _);
Check(pd != null && pd[0] == Proto.PlayerDamage, "player damage reaches target client");
Check(Wait(c1, out _, 300) == null, "other client not damaged");

// garbage doesn't break the transport
c1.SendRaw(c1.HostEp, new byte[] { 99, 1, 2 });
Check(Wait(host, out _) != null, "host survives junk packet");

// ---- UPnP against a fake router (SSDP responder + HTTP/SOAP server on loopback) ----
{
	var http = new HttpListener();
	int hp = 38000 + new Random().Next(1000);
	http.Prefixes.Add($"http://127.0.0.1:{hp}/");
	http.Start();
	var calls = new System.Collections.Generic.List<string>();
	string svcType = "urn:schemas-upnp-org:service:WANIPConnection:1";
	var httpThread = new Thread(() =>
	{
		while (http.IsListening)
		{
			HttpListenerContext ctx; try { ctx = http.GetContext(); } catch { break; }
			string body = new StreamReader(ctx.Request.InputStream).ReadToEnd();
			string resp;
			if (ctx.Request.HttpMethod == "GET")
				resp = $"<root xmlns=\"urn:schemas-upnp-org:device-1-0\"><device><deviceList><device><deviceList><device><serviceList><service><serviceType>{svcType}</serviceType><controlURL>/ctl</controlURL></service></serviceList></device></deviceList></device></deviceList></device></root>";
			else
			{
				string act = ctx.Request.Headers["SOAPAction"];
				lock (calls) calls.Add(act + " " + body);
				if (act.Contains("GetExternalIPAddress")) resp = "<s:Envelope xmlns:s=\"x\"><s:Body><u:R xmlns:u=\"y\"><NewExternalIPAddress>203.0.113.7</NewExternalIPAddress></u:R></s:Body></s:Envelope>";
				else if (act.Contains("AddPortMapping") && !body.Contains("<NewLeaseDuration>0<"))
				{
					ctx.Response.StatusCode = 500;
					resp = "<s:Envelope xmlns:s=\"x\"><s:Body><s:Fault><detail><UPnPError><errorCode>725</errorCode><errorDescription>OnlyPermanentLeasesSupported</errorDescription></UPnPError></detail></s:Fault></s:Body></s:Envelope>";
				}
				else resp = "<s:Envelope xmlns:s=\"x\"><s:Body/></s:Envelope>";
			}
			var b = System.Text.Encoding.UTF8.GetBytes(resp);
			ctx.Response.OutputStream.Write(b, 0, b.Length);
			ctx.Response.Close();
		}
	}) { IsBackground = true };
	httpThread.Start();

	var ssdp = new System.Net.Sockets.UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
	int sp = ((IPEndPoint)ssdp.Client.LocalEndPoint).Port;
	var ssdpThread = new Thread(() =>
	{
		try
		{
			while (true)
			{
				var from = new IPEndPoint(IPAddress.Any, 0);
				var d = ssdp.Receive(ref from);
				if (!System.Text.Encoding.ASCII.GetString(d).StartsWith("M-SEARCH")) continue;
				var r = System.Text.Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nLOCATION: http://127.0.0.1:{hp}/desc.xml\r\nST: upnp:rootdevice\r\n\r\n");
				ssdp.Send(r, r.Length, from);
			}
		}
		catch { }
	}) { IsBackground = true };
	ssdpThread.Start();

	var up = new Upnp { SsdpEndpoint = new IPEndPoint(IPAddress.Loopback, sp), LocalAddresses = () => new[] { IPAddress.Loopback }, DiscoverMs = 600 };
	up.Start(27777, "test");
	var until = DateTime.UtcNow.AddSeconds(8);
	while (!up.Mapped && DateTime.UtcNow < until) Thread.Sleep(50);
	Check(up.Mapped, "UPnP maps the port (fell back from leased to permanent): " + up.Status);
	Check(up.ExternalIp == "203.0.113.7", "UPnP reports external IP");
	lock (calls)
	{
		Check(calls.Exists(c => c.Contains("AddPortMapping") && c.Contains("<NewExternalPort>27777<") && c.Contains("<NewProtocol>UDP<") && c.Contains("<NewInternalClient>127.0.0.1<") && c.Contains("<NewLeaseDuration>0<")), "AddPortMapping SOAP is well formed");
	}
	up.Stop();
	lock (calls) Check(calls.Exists(c => c.Contains("DeletePortMapping") && c.Contains("<NewExternalPort>27777<")), "UPnP removes the mapping on stop");
	Check(Upnp.IsPrivate("192.168.1.5") && Upnp.IsPrivate("10.0.0.1") && !Upnp.IsPrivate("203.0.113.7"), "private-address detection");

	// No router at all: must fail quietly and not hang.
	var none = new Upnp { SsdpEndpoint = new IPEndPoint(IPAddress.Loopback, 9), LocalAddresses = () => new[] { IPAddress.Loopback }, DiscoverMs = 300 };
	none.Start(27777, "test");
	Thread.Sleep(1200);
	Check(!none.Mapped && none.Status.Contains("no gateway"), "no router: reports no gateway (" + none.Status + ")");
	none.Stop();
	http.Abort(); // the SSDP responder is a background thread; closing its socket mid-Receive can hang
}

// ---- reliable delivery: lost packets are re-sent, duplicates are appliedCount once ----
{
	int drops = 0;
	c1.DropFilter = b => b[0] == Proto.EnemyDamage && drops++ < 2; // lose the first two transmissions
	c1.SendReliable(c1.HostEp, Proto.EnemyDamage, w => { w.Write(42); w.Write(7); });
	int appliedCount = 0, rxCount = 0;
	var until = DateTime.UtcNow.AddSeconds(5);
	while (DateTime.UtcNow < until && (c1.PendingCount > 0 || rxCount == 0))
	{
		c1.Pump();
		while (host.TryDequeue(out var from2, out var d2))
		{
			var rd = new BinaryReader(new MemoryStream(d2));
			byte ty = rd.ReadByte();
			if (ty == Proto.EnemyDamage)
			{
				uint seq = rd.ReadUInt32(); rxCount++;
				host.SendAck(from2, seq);
				if (host.Fresh(from2, seq)) { appliedCount++; Check(rd.ReadInt32() == 42 && rd.ReadInt32() == 7, "reliable payload intact"); }
			}
		}
		while (c1.TryDequeue(out _, out var d3)) { if (d3[0] == Proto.Ack) c1.Ack(new BinaryReader(new MemoryStream(d3, 1, 4)).ReadUInt32()); }
		Thread.Sleep(10);
	}
	// a late duplicate of the same sequence must not be appliedCount twice
	c1.DropFilter = null;
	Check(appliedCount == 1, $"reliable damage applied exactly once after loss (appliedCount={appliedCount}, rxCount={rxCount})");
	Check(c1.PendingCount == 0, "acked packet is no longer pending");
	Check(drops >= 3, $"sender retried until the third transmission got through (transmissions={drops})");

	// giving up: a peer that never acks is dropped after MaxTries
	c2.DropFilter = b => true;
	c2.SendReliable(c2.HostEp, Proto.PlayerDamage, w => w.Write(1));
	var end2 = DateTime.UtcNow.AddMilliseconds(Transport.ResendMs * (Transport.MaxTries + 3));
	while (DateTime.UtcNow < end2 && c2.PendingCount > 0) { c2.Pump(); Thread.Sleep(20); }
	Check(c2.PendingCount == 0, "unacked packet is abandoned after MaxTries");
	c2.DropFilter = null;
}

host.Close(); c1.Close(); c2.Close();
Console.WriteLine(fails == 0 ? "ALL PASSED" : fails + " FAILED");
return fails;
