using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;

namespace CompoundMP
{
	// Projectiles are simulated only where they were fired (the host for enemy fire, each player's own machine for their
	// guns). Everyone else gets a harmless visual copy: the spawn is sent over the network and replayed as a clone of the
	// same prefab with its bullet logic and colliders switched off.
	static class ProjectileSync
	{
		const int MaxPerSecond = 80;

		static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
		static readonly Dictionary<string, float> missing = new Dictionary<string, float>(); // key -> next retry time
		static readonly HashSet<int> sent = new HashSet<int>();
		static float windowStart; static int windowCount;
		static int sentCount, recvCount, trailSentCount, trailRecvCount, droppedCount, replicaLogs;
		internal static bool Replaying;

		public static string Stats() => $"proj={sentCount}/{recvCount} trail={trailSentCount}/{trailRecvCount} dropped={droppedCount}";

		static string KeyOf(GameObject go) => go.name.EndsWith("(Clone)") ? go.name.Substring(0, go.name.Length - 7) : go.name;

		static bool Allow()
		{
			if (Time.unscaledTime - windowStart >= 1f) { windowStart = Time.unscaledTime; windowCount = 0; }
			if (windowCount >= MaxPerSecond) { droppedCount++; return false; }
			windowCount++;
			return true;
		}

		static GameObject FindPrefab(string key)
		{
			if (prefabs.TryGetValue(key, out var g) && g != null) return g;
			// The prefab is only loaded once the client's own level exists, so a miss is retried rather than remembered.
			if (missing.TryGetValue(key, out float retryAt) && Time.unscaledTime < retryAt) return null;
			foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
				if (go.name == key && !go.scene.IsValid() && go.GetComponent<BulletController>() != null) { prefabs[key] = go; return go; }
			if (!missing.ContainsKey(key)) Plugin.Log.LogWarning("Projectile prefab not found yet: " + key);
			missing[key] = Time.unscaledTime + 2f;
			return null;
		}

		// ---- sending (called from Harmony postfixes) ----

		public static void OnBulletStart(BulletController b)
		{
			if (!Plugin.Running || Replaying || b is HitscanBulletController) return; // hitscan shots are sent as trails
			if (b.GetComponent<ReplicaProjectile>() != null) return;
			var rb = b.GetComponent<Rigidbody>();
			if (rb == null) return;
			if (sent.Count > 4000) sent.Clear();
			if (!sent.Add(b.GetInstanceID()) || !Allow()) return;
			string key = KeyOf(b.gameObject);
			var t = b.transform;
			Vector3 pos = t.position, vel = rb.velocity; Quaternion rot = t.rotation;
			Plugin.NetSend(Proto.Projectile, w => { w.Write(key); Write(w, pos); Write(w, vel); Write(w, rot); });
			sentCount++;
		}

		public static void OnTrail(HitscanBulletController b, Vector3 p1, Vector3 p2)
		{
			if (!Plugin.Running || Replaying || !Allow()) return;
			string key = KeyOf(b.gameObject);
			Plugin.NetSend(Proto.Trail, w => { w.Write(key); Write(w, p1); Write(w, p2); });
			trailSentCount++;
		}

		// ---- receiving ----

		public static void ReceiveProjectile(BinaryReader r)
		{
			string key = r.ReadString();
			Vector3 pos = ReadV3(r), vel = ReadV3(r); Quaternion rot = ReadQ(r);
			var prefab = FindPrefab(key);
			if (prefab == null) return;
			var go = Object.Instantiate(prefab, pos, rot);
			foreach (var b in go.GetComponentsInChildren<BulletController>()) b.enabled = false;
			foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;
			var rb = go.GetComponent<Rigidbody>();
			if (rb != null) rb.velocity = vel;
			var rp = go.AddComponent<ReplicaProjectile>();
			rp.Key = key; rp.Start0 = pos;
			recvCount++;
			if (replicaLogs++ < 6) Plugin.Log.LogInfo($"[proj] replica {key} at {pos} vel {vel.magnitude:0.0}");
		}

		public static void ReceiveTrail(BinaryReader r)
		{
			string key = r.ReadString();
			Vector3 p1 = ReadV3(r), p2 = ReadV3(r);
			var prefab = FindPrefab(key);
			if (!(prefab?.GetComponent<HitscanBulletController>() is HitscanBulletController)) return;
			// Run the bullet class's own CreateTrail on a throwaway copy so every trail style is reproduced exactly.
			Replaying = true;
			GameObject tmp = null;
			try
			{
				tmp = Object.Instantiate(prefab);
				var h = tmp.GetComponent<HitscanBulletController>();
				h.enabled = false;
				Traverse.Create(h).Method("CreateTrail", p1, p2).GetValue();
				trailRecvCount++;
			}
			finally { if (tmp != null) Object.Destroy(tmp); Replaying = false; }
		}

		static void Write(BinaryWriter w, Vector3 v) { w.Write(v.x); w.Write(v.y); w.Write(v.z); }
		static void Write(BinaryWriter w, Quaternion q) { w.Write(q.x); w.Write(q.y); w.Write(q.z); w.Write(q.w); }
		static Vector3 ReadV3(BinaryReader r) => new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
		static Quaternion ReadQ(BinaryReader r) => new Quaternion(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
	}

	// A visual-only copy of a remote projectile: flies on the prefab's own rigidbody settings, vanishes on hitting level geometry.
	class ReplicaProjectile : MonoBehaviour
	{
		public string Key;
		public Vector3 Start0;
		static int endLogs;
		Vector3 prev;
		float born;
		int mask;

		void Start()
		{
			prev = transform.position; born = Time.time;
			mask = (1 << LayerMask.NameToLayer("Default")) | (1 << LayerMask.NameToLayer("MapPathSolid")) | (1 << LayerMask.NameToLayer("MapPathInvisible")) | (1 << LayerMask.NameToLayer("EnemyShield"));
		}

		void Update()
		{
			var rb = GetComponent<Rigidbody>();
			if (rb != null && rb.velocity.sqrMagnitude > 0.01f) transform.forward = rb.velocity;
		}

		void LateUpdate()
		{
			Vector3 now = transform.position, d = now - prev;
			bool hit = d.sqrMagnitude > 0f && Physics.Raycast(prev, d.normalized, d.magnitude, mask);
			if (hit || Time.time - born > 8f || (now - Start0).sqrMagnitude > 600f * 600f)
			{
				if (endLogs++ < 6) Plugin.Log.LogInfo($"[proj] replica {Key} ended after {Time.time - born:0.00}s, {(now - Start0).magnitude:0.0}m");
				Destroy(gameObject);
			}
			prev = now;
		}
	}
}
