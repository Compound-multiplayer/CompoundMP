using System;
using Steamworks;

namespace CompoundMP
{
	// Steam friends integration through the game's own Steamworks.NET: while hosting, the join address is published as rich
	// presence (`connect`), which makes "Join Game" appear in the friends list; Steam then starts the game with
	// `+connect <address>` (handled in Plugin) or, if the game is already running, raises GameRichPresenceJoinRequested.
	static class SteamJoin
	{
		static Callback<GameRichPresenceJoinRequested_t> joinRequested;
		static string published = "";
		static float nextTry;
		public static string Status = "Steam: waiting for Steam";

		// Call regularly; registers the callback once the game's SteamManager is up. Safe when Steam is not running.
		public static void Tick(float now)
		{
			if (joinRequested != null || now < nextTry) return;
			nextTry = now + 3f;
			try
			{
				if (!SteamManager.Initialized) { Status = "Steam: not running, friend invites unavailable"; return; }
				joinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(OnJoinRequested);
				Status = "Steam: ready";
				Plugin.Log.LogInfo("Steam friend join: callback registered");
			}
			catch (Exception e) { Status = "Steam: " + e.Message; }
		}

		static void OnJoinRequested(GameRichPresenceJoinRequested_t p)
		{
			Plugin.Log.LogInfo("Steam join request: " + p.m_rgchConnect);
			Plugin.Instance.JoinFromSteam(p.m_rgchConnect);
		}

		// `connect` is host:port, or null/empty to stop advertising.
		public static void Publish(string connect)
		{
			connect = connect ?? "";
			if (connect == published) return;
			try
			{
				if (!SteamManager.Initialized) return;
				if (connect == "") SteamFriends.ClearRichPresence();
				else
				{
					SteamFriends.SetRichPresence("connect", connect);
					SteamFriends.SetRichPresence("status", "Hosting co-op");
				}
				published = connect;
				Plugin.Log.LogInfo("Steam rich presence " + (connect == "" ? "cleared" : "connect=" + connect));
			}
			catch (Exception e) { Plugin.Log.LogWarning("Steam rich presence failed: " + e.Message); }
		}
	}
}
