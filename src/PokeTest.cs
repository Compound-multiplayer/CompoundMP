using HarmonyLib;

namespace CompoundMP
{
	// Test-only: lets the menu driver press the (absent) controller trigger and keeps the hand-pose calls, which need SteamVR
	// hardware, from throwing. Everything stays inert unless Active is set, so normal play is untouched.
	static class PokeTest
	{
		public static bool Active;
		public static SteamVRWrapper Hand; // only this hand's trigger is pulled; the other hand must not select anything
		static bool trigger;
		public static void Pull() => trigger = true;
		public static bool Consume() { bool t = trigger; trigger = false; return t; }
	}

	[HarmonyPatch(typeof(SteamVRWrapper), "JustTriggerPulled")]
	static class PokeTriggerPatch
	{
		static bool Prefix(SteamVRWrapper __instance, ref bool __result)
		{
			if (!PokeTest.Active) return true;
			__result = __instance == PokeTest.Hand && PokeTest.Consume();
			return false;
		}
	}

	[HarmonyPatch(typeof(SteamVRWrapper), "SetHandPosePointing")]
	static class PokePosePointingPatch { static bool Prefix() => !PokeTest.Active; }

	[HarmonyPatch(typeof(SteamVRWrapper), "SetHandPoseFist")]
	static class PokePoseFistPatch { static bool Prefix() => !PokeTest.Active; }

	[HarmonyPatch(typeof(SteamVRWrapper), "SetForcePoseOverride")]
	static class PokePoseOverridePatch { static bool Prefix() => !PokeTest.Active; }
}
