using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CompoundMP
{
	// Force every client to generate the host's map; tell clients when the host makes a new one.
	[HarmonyPatch(typeof(MapGenerator), "CreateMap")]
	static class CreateMapPatch
	{
		static void Prefix()
		{
			if (!Plugin.IsClient || !Plugin.HasHostSeed) return;
			var gc = Singleton<GlobalGameInfo>.Instance.GameControllerInstance;
			gc.RandomSeedEnabled = true;
			gc.RandomSeed = Plugin.HostSeed;
		}

		static void Postfix() => Plugin.Instance?.HostMapCreated();
	}

	// Damage routing:
	//  - hits on a remote player's ghost (host only, from enemy bullets) are forwarded to that player;
	//  - hits a client lands on an enemy are forwarded to the host, which owns the enemy.
	[HarmonyPatch(typeof(Damageable), nameof(Damageable.TakeDamage))]
	static class TakeDamagePatch
	{
		static bool Prefix(Damageable __instance, int damage, GameObject damageDealer)
		{
			if (Plugin.Applying || !Plugin.Running) return true;

			var ghost = __instance.GetComponent<GhostMarker>();
			if (ghost != null)
			{
				if (Plugin.IsHost) { Plugin.Log.LogInfo($"[debug] ghost {ghost.Id} hit for {damage} by {(damageDealer != null ? damageDealer.name : "?")}"); Plugin.Instance.SendPlayerDamage(ghost.Id, damage); }
				return false;
			}
			if (Plugin.IsClient && Plugin.Instance.TryEnemyIndex(__instance, out int idx))
			{
				Plugin.Instance.SendEnemyDamage(idx, damage);
				return false;
			}
			return true;
		}
	}

	// Enemy AI only knows about the local player. While an enemy thinks, swap the player's position for the
	// nearest living player (host or a ghost), so enemies hunt every player. Restored in the finalizer.
	[HarmonyPatch]
	static class EnemyTargetPatch
	{
		static int depth;

		static IEnumerable<MethodBase> TargetMethods()
		{
			var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
			foreach (var t in typeof(EnemyController).Assembly.GetTypes())
			{
				if (t != typeof(EnemyController) && !t.IsSubclassOf(typeof(EnemyController))) continue;
				foreach (var name in new[] { "Update", "FixedUpdate" })
				{
					var m = t.GetMethod(name, flags, null, Type.EmptyTypes, null);
					if (m != null) yield return m;
				}
			}
		}

		static void Prefix(EnemyController __instance, ref Vector3[] __state)
		{
			depth++;
			__state = null;
			if (depth != 1 || !Plugin.IsHost || Plugin.Instance.Avatars.Count == 0) return;

			var g = Singleton<GlobalGameInfo>.Instance;
			var playerT = g.PlayerControllerInstance.transform;
			Vector3 enemyPos = __instance.transform.position;
			Vector3 offset = playerT.position - g.VRCameraEye.transform.position; // capsule vs. head

			Vector3 best = playerT.position;
			float bestDist = g.PlayerControllerInstance.IsDead() ? float.MaxValue : (best - enemyPos).sqrMagnitude;
			bool swap = bestDist == float.MaxValue;
			foreach (var a in Plugin.Instance.Avatars.Values)
			{
				if (a.Dead) continue;
				Vector3 p = a.HeadPos + offset;
				float d = (p - enemyPos).sqrMagnitude;
				if (d < bestDist) { bestDist = d; best = p; swap = true; }
			}
			if (!swap) return;
			__state = new[] { playerT.position };
			playerT.position = best;
		}

		static Exception Finalizer(Exception __exception, Vector3[] __state)
		{
			depth--;
			if (__state != null) Singleton<GlobalGameInfo>.Instance.PlayerControllerInstance.transform.position = __state[0];
			return __exception;
		}
	}
}

namespace CompoundMP
{
	// Projectile replication hooks. Subclasses that hide or override Start/CreateTrail are patched individually;
	// ProjectileSync de-duplicates when an override calls its base.
	[HarmonyPatch]
	static class BulletStartPatch
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
			foreach (var t in typeof(BulletController).Assembly.GetTypes())
			{
				if (t != typeof(BulletController) && !t.IsSubclassOf(typeof(BulletController))) continue;
				var m = t.GetMethod("Start", flags, null, Type.EmptyTypes, null);
				if (m != null) yield return m;
			}
		}

		static void Postfix(BulletController __instance) => ProjectileSync.OnBulletStart(__instance);
	}

	[HarmonyPatch]
	static class TrailPatch
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
			foreach (var t in typeof(HitscanBulletController).Assembly.GetTypes())
			{
				if (t != typeof(HitscanBulletController) && !t.IsSubclassOf(typeof(HitscanBulletController))) continue;
				var m = t.GetMethod("CreateTrail", flags, null, new[] { typeof(Vector3), typeof(Vector3) }, null);
				if (m != null) yield return m;
			}
		}

		static void Postfix(HitscanBulletController __instance, Vector3 p1, Vector3 p2) => ProjectileSync.OnTrail(__instance, p1, p2);
	}
}
