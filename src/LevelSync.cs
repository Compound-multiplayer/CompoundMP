using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CompoundMP
{
	// The host leads level changes. When its NextLevelTrigger changes level it announces the scene and the seed; each
	// client then runs its own level trigger towards that scene (same fade, same save handling as walking into the
	// elevator) and cannot leave a level the host is still in.
	static class LevelSync
	{
		public static string HostMap = "";   // last level the host announced
		static string pending;                // level the host moved to that this client still has to reach
		static float pendingSince, lastToast;
		static bool started;
		public static string Pending => pending;
		static bool Follows(string map) => map != null && map.StartsWith("map"); // runs and HQ; not practice/tutorial areas

		// ---- host ----

		public static void OnHostChanged()
		{
			var g = Singleton<GlobalGameInfo>.Instance;
			string map = g.CurrentMapName;
			if (!Plugin.IsHost || !Follows(map)) return;
			HostMap = map;
			int seed = g.GameControllerInstance.RandomSeed;
			Plugin.Instance.SendLevelChange(map, seed);
			Plugin.Log.LogInfo($"[level] host moved to {map} (seed {seed})");
		}

		// ---- client ----

		public static void OnHostAnnounced(string map, int seed)
		{
			HostMap = map;
			if (!Plugin.IsClient || !Follows(map)) return;
			Plugin.HostSeed = seed; Plugin.HasHostSeed = true;
			var g = Singleton<GlobalGameInfo>.Instance;
			if (g.CurrentMapName == map) { pending = null; return; }
			pending = map; pendingSince = Time.unscaledTime; started = false;
			Plugin.Log.LogInfo($"[level] following host to {map} (from {g.CurrentMapName})");
			Plugin.Instance.Toast("The host is changing level");
		}

		public static void Tick()
		{
			if (pending == null || !Plugin.IsClient) return;
			var g = Singleton<GlobalGameInfo>.Instance;
			if (g == null || g.GameControllerInstance == null) return;
			if (g.CurrentMapName == pending) { if (g.Map != null || Time.unscaledTime - pendingSince > 30f) pending = null; return; }
			if (!started)
			{
				var trig = Object.FindObjectsOfType<NextLevelTrigger>().OrderBy(t => t.NextMapName == pending ? 0 : t.NextMapName == "" ? 1 : 2).FirstOrDefault();
				if (trig != null && !g.GameControllerInstance.MapFaderController.IsFading)
				{
					started = true;
					trig.NextMapName = pending;
					var fader = g.GameControllerInstance.MapFaderController;
					fader.OnFadeOutComplete = (MapFader.FadeEventHandler)System.Delegate.Combine(fader.OnFadeOutComplete, new MapFader.FadeEventHandler(trig.ChangeLevel));
					fader.FadeOut();
					Plugin.Log.LogInfo("[level] client started its level trigger towards " + pending);
				}
			}
			// No usable trigger (or it did not get us there): load the scene directly, as the debug loader does.
			if (Time.unscaledTime - pendingSince > 8f && g.CurrentMapName != pending)
			{
				Plugin.Log.LogWarning("[level] trigger did not reach " + pending + "; loading it directly");
				string from = g.CurrentMapName;
				SceneManager.UnloadSceneAsync(from);
				SceneManager.LoadScene(pending, LoadSceneMode.Additive);
				g.CurrentMapName = pending; g.Map = null; g.GameControllerInstance.CurrentMapInfo = null;
			}
		}

		public static bool ShouldBlock(NextLevelTrigger t, Collider other)
		{
			if (!Plugin.IsClient || !Plugin.Instance.Welcomed || pending != null || HostMap == "") return false;
			var g = Singleton<GlobalGameInfo>.Instance;
			if (g.CurrentMapName != HostMap) return false; // the host is elsewhere: let the client catch up
			if (other.GetComponent<PlayerController>() == null || (t.RequireAllEnemiesDead && !g.Map.AllEnemiesDead())) return false;
			if (Time.unscaledTime - lastToast > 5f) { lastToast = Time.unscaledTime; Plugin.Instance.Toast("Only the host can start the next level"); }
			return true;
		}
	}

	[HarmonyPatch(typeof(NextLevelTrigger), "OnTriggerStay")]
	static class LevelTriggerPatch
	{
		static bool Prefix(NextLevelTrigger __instance, Collider collider) => !LevelSync.ShouldBlock(__instance, collider);
	}

	[HarmonyPatch(typeof(NextLevelTrigger), "ChangeLevel")]
	static class ChangeLevelPatch
	{
		static void Postfix() => LevelSync.OnHostChanged();
	}

	// In the chaos mutator the next level is rolled at random; use the level the host chose instead.
	[HarmonyPatch(typeof(NextLevelTrigger), "GetRandomMapName")]
	static class RandomMapPatch
	{
		static bool Prefix(ref string __result)
		{
			if (LevelSync.Pending == null) return true;
			__result = LevelSync.Pending;
			return false;
		}
	}
}
