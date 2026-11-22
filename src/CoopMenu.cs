using System;
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

		static void OpenPage(MenuButton mb)
		{
			var field = Traverse.Create(mb).Field("Menu");
			var old = field.GetValue<GameObject>();
			if (old != null) UnityEngine.Object.Destroy(old);
			var page = new GameObject("CoopMenu");
			page.layer = LayerMask.NameToLayer("Menu");
			page.transform.SetParent(mb.MenuTransform, false);
			field.SetValue(page); // MenuButton now owns/destroys this page like any other submenu
			Build(page.transform, mb);
		}

		static void Build(Transform t, MenuButton mb)
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
			Option(t, "Back", 0f, -3f, 1f, () => "Back To Main Menu", () => mb.SwitchMenu(MenuButton.Submenu.BaseMenu));
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
