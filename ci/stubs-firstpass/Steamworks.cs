using System;

namespace Steamworks
{
	public struct CSteamID { }

	public struct GameRichPresenceJoinRequested_t
	{
		public CSteamID m_steamIDFriend;
		public string m_rgchConnect;
	}

	public abstract class Callback { }

	public sealed class Callback<T> : Callback, IDisposable
	{
		public delegate void DispatchDelegate(T param);
		public static Callback<T> Create(DispatchDelegate func) { throw null; }
		public void Dispose() { }
	}

	public static class SteamFriends
	{
		public static bool SetRichPresence(string pchKey, string pchValue) { throw null; }
		public static void ClearRichPresence() { }
	}
}
