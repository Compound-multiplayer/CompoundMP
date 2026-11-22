using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace CompoundMP
{
	// Adds a CO-OP MULTIPLAYER page to the game's own VR wrist menu (the ASCII-text menu you poke with a finger and
	// select with the trigger), so hosting/joining never needs a keyboard.

	// Selectable line, built like the game's own MenuOptions so MenuSelector can highlight and trigger it.
	class CoopOption : MenuOption
	{
		public Func<string> Label;
		public Action OnSelect;

		void LateUpdate()
		{
			string t = Label?.Invoke();
			if (t != null && t != DisplayText) { DisplayText = t; UpdateDisplay(); }
		}

		public override void Select() => OnSelect?.Invoke();
	}

	// Plain text line (status readout).
	class CoopLabel : ASCIIText
	{
		public Func<string> Label;

		void LateUpdate()
		{
			string t = Label?.Invoke();
			if (t != null && t != DisplayText) { DisplayText = t; UpdateDisplay(); }
		}
	}

	static class CoopMenu
	{
		const string EntryName = "Co-op";
		const int MaxAddress = 21; // 255.255.255.255:65535

		static MenuOption template;

		static GameObject NewChild(Transform parent, string name, Vector3 pos, float scale)
		{
			var go = new GameObject(name);
			go.layer = LayerMask.NameToLayer("Menu");
			go.transform.SetParent(parent, false);
			go.transform.localPosition = pos;
			go.transform.localScale = Vector3.one * scale;
			return go;
		}

		static CoopOption Option(Transform parent, string name, float x, float y, float scale, Func<string> label, Action onSelect, string tip = "")
		{
			var o = NewChild(parent, name, new Vector3(x, y, 0f), scale).AddComponent<CoopOption>();
			o.ASCIITextNormal = template.ASCIITextNormal;
			o.ASCIITextSelected = template.ASCIITextSelected;
			o.ASCIITextObject = template.ASCIITextNormal;
			o.Options = new string[0];
			o.Alignment = ASCIIText.TextAlignment.AlignCentre;
			o._ToolTipText = tip;
			o.Label = label; o.OnSelect = onSelect;
			o.DisplayText = label();
			return o;
		}

		static CoopLabel Text(Transform parent, string name, float x, float y, float scale, Func<string> label)
		{
			var o = NewChild(parent, name, new Vector3(x, y, 0f), scale).AddComponent<CoopLabel>();
			o.ASCIITextObject = template.ASCIITextNormal;
			o.Alignment = ASCIIText.TextAlignment.AlignCentre;
			o.Label = label;
			o.DisplayText = label();
			return o;
		}

		// Called when the main wrist menu appears: slot a CO-OP entry into the free line between PERFORMANCE and END RUN.
		public static void AddEntry(BaseMenu menu)
		{
			var tmpl = menu.GetComponentsInChildren<MenuOptionChangeMenu>(true).FirstOrDefault();
			var mb = menu.GetComponentInParent<MenuButton>();
			if (tmpl == null || mb == null || menu.transform.Find(EntryName) != null) return;
			template = tmpl;
			Option(menu.transform, EntryName, 0f, 2f, 1.2f, () => "CO-OP MULTIPLAYER", () => OpenPage(mb), "Host or join a friend's game");
		}

		static void OpenPage(MenuButton mb) => Show(mb, BuildMain);

		static void Show(MenuButton mb, Action<Transform, MenuButton> build)
		{
			var field = Traverse.Create(mb).Field("Menu");
			var old = field.GetValue<GameObject>();
			if (old != null) UnityEngine.Object.Destroy(old);
			var page = new GameObject("CoopMenu");
			page.layer = LayerMask.NameToLayer("Menu");
			page.transform.SetParent(mb.MenuTransform, false);
			field.SetValue(page); // MenuButton now owns/destroys this page like any other submenu
			build(page.transform, mb);
		}

		static void BuildMain(Transform t, MenuButton mb)
		{
			var plugin = Plugin.Instance;
			Text(t, "Title", 0f, 20f, 1.2f, () => "CO-OP MULTIPLAYER");
			Text(t, "Status", 0f, 18f, 1f, plugin.StatusLine);
			Text(t, "Net", 0f, 16f, 1f, plugin.NetLine);

			Option(t, "Host", 0f, 13f, 1.2f, () => "HOST GAME", plugin.StartHost, "Start hosting. Friends join with your public IP");
			Option(t, "Upnp", 0f, 11f, 1.2f, () => "UPNP: " + (plugin.UpnpOn ? "ON" : "OFF"), () => plugin.UpnpOn = !plugin.UpnpOn,
				"Automatically open the port on your router when hosting");

			Text(t, "Address", 0f, 8f, 1.2f, () => "JOIN: " + plugin.JoinTarget + "_");
			string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", ".", "0", ":" };
			for (int i = 0; i < keys.Length; i++)
			{
				string k = keys[i];
				Option(t, "Key" + k, -3f + 3f * (i % 3), 6f - 2f * (i / 3), 1.2f, () => k, () =>
				{
					if (plugin.JoinTarget.Length < MaxAddress) plugin.JoinTarget += k;
				}, "Type the host's IP address");
			}
			Option(t, "Del", 9f, 6f, 1.2f, () => "DEL", () => { var a = plugin.JoinTarget; if (a.Length > 0) plugin.JoinTarget = a.Substring(0, a.Length - 1); });
			Option(t, "Clear", 9f, 4f, 1.2f, () => "CLEAR", () => plugin.JoinTarget = "");
			Option(t, "Join", 9f, 2f, 1.2f, () => "JOIN GAME", plugin.StartClient, "Join the host at the address shown");
			Option(t, "Stop", 9f, 0f, 1.2f, () => "DISCONNECT", plugin.Stop, "Leave the co-op session");

			Option(t, "Find", -10.5f, 6f, 1.2f, () => "FIND GAMES", () => Show(mb, BuildHosts), "Games on your network and hosts you joined before");
			Option(t, "Keyboard", -10.5f, 4f, 1.2f, () => "KEYBOARD", () => { kbTarget = KbTarget.Address; Show(mb, BuildKeyboard); }, "Type a hostname with letters");
			Option(t, "Password", -10.5f, 2f, 1.2f, () => "PASSWORD:" + (plugin.HasPassword ? "ON" : "OFF"), () => { kbTarget = KbTarget.Password; Show(mb, BuildKeyboard); },
				"Shared secret: the host and the players joining must use the same one");
			Option(t, "Back", 0f, -3f, 1f, () => "Back To Main Menu", () => mb.SwitchMenu(MenuButton.Submenu.BaseMenu));
		}

		// ---- hosts page: games heard on the LAN, then hosts joined before ----

		static List<LanDiscovery.HostInfo> lanCache = new List<LanDiscovery.HostInfo>();
		static int lanFrame = -1;
		static List<LanDiscovery.HostInfo> Lan()
		{
			if (Time.frameCount != lanFrame) { lanFrame = Time.frameCount; lanCache = Plugin.Instance.LanHosts(); }
			return lanCache;
		}

		static string Clip(string s, int n) => s.Length <= n ? s : s.Substring(0, n);

		static void BuildHosts(Transform t, MenuButton mb)
		{
			var plugin = Plugin.Instance;
			Text(t, "Title", 0f, 20f, 1.2f, () => "FIND GAMES");
			Text(t, "LanHeader", 0f, 18f, 1f, () => Lan().Count == 0 ? "Looking for games on your network..." : "Games on your network");
			for (int i = 0; i < 4; i++)
			{
				int row = i;
				Option(t, "Lan" + i, 0f, 16f - 2f * i, 1f, () =>
				{
					var l = Lan();
					if (row >= l.Count) return "";
					var h = l[row];
					return Clip(h.Name, 18) + "  " + h.Address + "  " + h.Players + "P" + (h.NeedsPassword ? " PW" : "");
				}, () =>
				{
					var l = Lan();
					if (row >= l.Count) return;
					plugin.JoinTarget = l[row].Target;
					plugin.StartClient();
					Show(mb, BuildMain);
				}, "Join this game");
			}
			Text(t, "RecentHeader", 0f, 7f, 1f, () => plugin.RecentHosts().Count == 0 ? "No recent hosts yet" : "Recent hosts");
			for (int i = 0; i < 3; i++)
			{
				int row = i;
				Option(t, "Recent" + i, 0f, 5f - 2f * i, 1f, () => { var r = plugin.RecentHosts(); return row < r.Count ? Clip(r[row], 34) : ""; }, () =>
				{
					var r = plugin.RecentHosts();
					if (row >= r.Count) return;
					plugin.JoinTarget = r[row];
					plugin.StartClient();
					Show(mb, BuildMain);
				}, "Join this host again");
			}
			Option(t, "Back", 0f, -3f, 1f, () => "Back", () => Show(mb, BuildMain));
		}

		// ---- keyboard page: letters, digits and . - : _ for hostnames and the password ----

		enum KbTarget { Address, Password }
		static KbTarget kbTarget;
		static bool caps;

		static string KbGet() => kbTarget == KbTarget.Address ? Plugin.Instance.JoinTarget : Plugin.Instance.PasswordText;
		static void KbSet(string v) { if (kbTarget == KbTarget.Address) Plugin.Instance.JoinTarget = v; else Plugin.Instance.PasswordText = v; }

		static void BuildKeyboard(Transform t, MenuButton mb)
		{
			int max = kbTarget == KbTarget.Address ? 60 : 32;
			caps = false;
			Text(t, "Title", 0f, 20f, 1.2f, () => kbTarget == KbTarget.Address ? "TYPE HOST ADDRESS" : "TYPE PASSWORD");
			Text(t, "Value", 0f, 18f, 1.2f, () =>
			{
				string v = KbGet();
				if (kbTarget == KbTarget.Password) v = new string('*', v.Length);
				return Clip(v.Length > 24 ? v.Substring(v.Length - 24) : v, 24) + "_";
			});
			string[] rows = { "1234567890", "qwertyuiop", "asdfghjkl_", "zxcvbnm.-:" };
			for (int r = 0; r < rows.Length; r++)
				for (int c = 0; c < rows[r].Length; c++)
				{
					char ch = rows[r][c];
					Func<string> label = () => char.IsLetter(ch) && caps ? char.ToUpper(ch).ToString() : ch.ToString();
					Option(t, "K" + r + "_" + c, (c - (rows[r].Length - 1) / 2f) * 2.4f, 15f - 2f * r, 1.2f, label, () =>
					{
						if (KbGet().Length >= max) return;
						KbSet(KbGet() + label());
						caps = false; // shift applies to one letter
					});
				}
			Option(t, "Shift", -8f, 6f, 1.2f, () => caps ? "SHIFT*" : "SHIFT", () => caps = !caps);
			Option(t, "Del", -2f, 6f, 1.2f, () => "DEL", () => { var v = KbGet(); if (v.Length > 0) KbSet(v.Substring(0, v.Length - 1)); });
			Option(t, "Clear", 3.5f, 6f, 1.2f, () => "CLEAR", () => KbSet(""));
			Option(t, "Done", 9.5f, 6f, 1.2f, () => "DONE", () => Show(mb, BuildMain));
		}
}

	[HarmonyPatch(typeof(BaseMenu), "Start")]
	static class BaseMenuPatch
	{
		static void Postfix(BaseMenu __instance)
		{
			try { CoopMenu.AddEntry(__instance); }
			catch (Exception e) { Plugin.Log.LogError("Could not add co-op menu entry: " + e); }
		}
	}
}
