using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CompoundMP
{
	// Host-authoritative enemies. Enemies spawn in the same order on every machine (same seed), so the
	// index into Map.Enemies is the network id. The host streams position/alive; clients freeze their AI and follow.
	class EnemySync
	{
		MapBase map;
		readonly Dictionary<Damageable, int> index = new Dictionary<Damageable, int>();
		readonly HashSet<int> frozen = new HashSet<int>();
		Proto.EnemyRec[] targets = new Proto.EnemyRec[0];
		bool[] haveTarget = new bool[0];
		string mismatchLoggedFor;

		public static string MapName => Singleton<GlobalGameInfo>.Instance?.CurrentMapName ?? "";

		public void Reset()
		{
			map = null;
			index.Clear();
			frozen.Clear();
			targets = new Proto.EnemyRec[0];
			haveTarget = new bool[0];
		}

		// Rebuild the index when the map (or its enemy count) changed.
		List<Damageable> Refresh()
		{
			var m = Singleton<GlobalGameInfo>.Instance?.Map;
			if (m == null) return null;
			if (m != map)
			{
				map = m;
				index.Clear();
				frozen.Clear();
				targets = new Proto.EnemyRec[0];
				haveTarget = new bool[0];
			}
			var list = m.Enemies;
			if (index.Count != list.Count)
			{
				index.Clear();
				for (int i = 0; i < list.Count; i++) if (list[i] != null) index[list[i]] = i;
			}
			return list;
		}

		public string Summary() => $"enemies={index.Count} frozen={frozen.Count} targets={targets.Length}";

		public string Probe()
		{
			var list = Refresh();
			if (list == null || list.Count == 0 || list[0] == null) return "enemy0=none";
			var t = RootOf(list[0]).position;
			return $"n={list.Count} enemy0 dead={list[0].IsDead()} pos=({t.x:0.0},{t.y:0.0},{t.z:0.0}) frozen={frozen.Count}";
		}

		public Damageable At(int i)
		{
			var list = Refresh();
			return list != null && i >= 0 && i < list.Count ? list[i] : null;
		}

		public bool TryGetIndex(Damageable d, out int i)
		{
			i = -1;
			if (!Plugin.Running || d == null) return false;
			Refresh();
			return index.TryGetValue(d, out i);
		}

		static Transform RootOf(Damageable d)
		{
			var ec = d.GetComponentInParent<EnemyController>();
			return ec != null ? ec.transform : d.transform;
		}

		// ---- host ----

		public void HostSend(Transport net)
		{
			if (net.Clients.Count == 0) return;
			var list = Refresh();
			if (list == null || list.Count == 0) return;
			var recs = new Proto.EnemyRec[list.Count];
			for (int i = 0; i < list.Count; i++)
			{
				var d = list[i];
				if (d == null) { recs[i] = new Proto.EnemyRec { Alive = false }; continue; }
				var t = RootOf(d);
				recs[i] = new Proto.EnemyRec { Alive = !d.IsDead(), X = t.position.x, Y = t.position.y, Z = t.position.z, Yaw = t.eulerAngles.y };
			}
			string name = MapName;
			for (int s = 0; s < recs.Length; s += Proto.EnemiesPerPacket)
			{
				int start = s, n = Mathf.Min(Proto.EnemiesPerPacket, recs.Length - s);
				net.Broadcast(Proto.Enemies, w => Proto.WriteEnemies(w, name, recs.Length, start, recs, n));
			}
		}

		public void HostApplyDamage(int i, int dmg)
		{
			var list = Refresh();
			if (list == null || i < 0 || i >= list.Count || list[i] == null) return;
			Plugin.Applying = true;
			try { list[i].TakeDamage(dmg); } finally { Plugin.Applying = false; }
		}

		// ---- client ----

		public void ClientReceive(BinaryReader r)
		{
			var recs = Proto.ReadEnemies(r, out string name, out int total, out int start);
			var list = Refresh();
			if (list == null) return;
			if (name != MapName || total != list.Count)
			{
				if (mismatchLoggedFor != name)
				{
					mismatchLoggedFor = name;
					Plugin.Log.LogWarning($"Enemy sync skipped: host map '{name}' x{total}, local '{MapName}' x{list.Count}");
				}
				return;
			}
			if (targets.Length != total) { targets = new Proto.EnemyRec[total]; haveTarget = new bool[total]; }
			for (int i = 0; i < recs.Length && start + i < total; i++) { targets[start + i] = recs[i]; haveTarget[start + i] = true; }
		}

		public void ClientTick()
		{
			var list = Refresh();
			if (list == null) return;
			float k = 1f - Mathf.Exp(-15f * Time.unscaledDeltaTime);
			for (int i = 0; i < list.Count && i < targets.Length; i++)
			{
				var d = list[i];
				if (d == null || !haveTarget[i]) continue;
				Freeze(d, i);
				var rec = targets[i];
				if (!rec.Alive)
				{
					if (!d.IsDead())
					{
						Plugin.Applying = true;
						try { d.TakeDamage(d.MaxHealth * 10); } finally { Plugin.Applying = false; }
					}
					continue;
				}
				var t = RootOf(d);
				t.position = Vector3.Lerp(t.position, new Vector3(rec.X, rec.Y, rec.Z), k);
				t.rotation = Quaternion.Slerp(t.rotation, Quaternion.Euler(0f, rec.Yaw, 0f), k);
			}
		}

		void Freeze(Damageable d, int i)
		{
			if (!frozen.Add(i)) return;
			var root = RootOf(d);
			foreach (var ec in root.GetComponentsInChildren<EnemyController>(true)) ec.enabled = false;
			foreach (var nav in root.GetComponentsInChildren<MapNavigator>(true)) nav.enabled = false;
		}
	}
}
