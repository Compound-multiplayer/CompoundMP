using System.Collections;
using System.Linq;
using HarmonyLib;
using BepInEx.Logging;
using UnityEngine;

namespace CompoundMP
{
	// Development helper: drives the real wrist-menu code path without a headset (no hand, so selections are invoked
	// directly) and checks that every co-op entry is something MenuSelector's hand-poke overlap test would find.
	static class MenuDump
	{
		public static IEnumerator Run(ManualLogSource log)
		{
			string origAddress = Plugin.Instance.JoinTarget, origPassword = Plugin.Instance.PasswordText, origRecent = Traverse.Create(Plugin.Instance).Field("recentHosts").Property("Value").GetValue<string>();
			var mb = Resources.FindObjectsOfTypeAll<MenuButton>().FirstOrDefault(b => b.MenuPrefab != null && b.MenuTransform != null);
			if (mb == null) { log.LogInfo("[menu] no MenuButton"); yield break; }
			// Headless, the wrist button is never "ready" and MenuButton.Update would close the menu every frame.
			mb.enabled = false;
			// The wrist button shrinks to nothing unless looked at (and always in the StartArea scene this runs in), which would
			// give the menu zero scale: pin the button at its full size instead.
			var grower = mb.Grower;
			var gt = Traverse.Create(grower);
			grower.enabled = false;
			grower.transform.localScale = new Vector3(gt.Field("StartScaleX").GetValue<float>(), gt.Field("StartScaleY").GetValue<float>(), gt.Field("StartScaleZ").GetValue<float>());
			log.LogInfo("[menu] wrist button scale = " + grower.transform.lossyScale);
			yield return new WaitForSeconds(1f);
			// Same steps as MenuButton.ToggleMenu
			var menu = Object.Instantiate(mb.MenuPrefab);
			menu.transform.parent = mb.MenuTransform;
			menu.transform.localPosition = Vector3.zero; menu.transform.localScale = Vector3.one; menu.transform.localEulerAngles = Vector3.zero;
			Traverse.Create(mb).Field("Menu").SetValue(menu);
			yield return new WaitForSeconds(1f);

			var entry = menu.transform.Find("Co-op");
			log.LogInfo("[menu] main menu has Co-op entry: " + (entry != null));
			if (entry == null) yield break;
			Report(log, entry);
			var cam = Shot.MenuCamera(menu.transform, new Vector2(0f, 11f), 13.5f);
			yield return Shot.CaptureRoutine(cam, "menu_1_main");
			entry.GetComponent<MenuOption>().Select();
			yield return new WaitForSeconds(1f);

			var page = Traverse.Create(mb).Field("Menu").GetValue<GameObject>();
			log.LogInfo("[menu] page opened: " + page.name + " children=" + page.transform.childCount);
			foreach (Transform c in page.transform) Report(log, c);
			cam = Shot.MenuCamera(page.transform, new Vector2(0f, 8.5f), 13.5f);
			yield return null;
			yield return Shot.CaptureRoutine(cam, "menu_2_coop_page");

			Press(log, page, "Clear");
			foreach (var k in new[] { "1", "9", "2", ".", "1", "6", "8", ".", "0", ".", "5" }) Press(log, page, "Key" + k);
			Press(log, page, "Del");
			log.LogInfo("[menu] typed address = " + Plugin.Instance.JoinTarget);
			Press(log, page, "Upnp"); Press(log, page, "Upnp");
			Press(log, page, "Host");
			yield return new WaitForSeconds(2f);
			foreach (var l in page.GetComponentsInChildren<CoopLabel>()) log.LogInfo("[menu] label " + l.name + " = '" + l.DisplayText + "'");
			yield return Shot.CaptureRoutine(cam, "menu_3_hosting");
			log.LogInfo("[menu] running=" + Plugin.Running + " host=" + Plugin.IsHost);
			Press(log, page, "Stop");
			log.LogInfo("[menu] after stop running=" + Plugin.Running);
			// ---- the real hand path: put the hand's MenuSelector on a key and pull the (simulated) trigger ----
			// The wrist menu hangs off the left controller, so poke with the other hand (moving the left one would carry the menu away).
			var selectors = Resources.FindObjectsOfTypeAll<MenuSelector>().Where(m => m.gameObject.scene.IsValid()).ToList();
			var selector = selectors.FirstOrDefault(m => !m.transform.IsChildOf(mb.transform.parent));
			var otherSelector = selectors.FirstOrDefault(m => m != selector);
			log.LogInfo("[poke] hand selectors: " + selectors.Count + ", poking with " + (selector != null ? selector.transform.parent.name + "/" + selector.name : "none"));
			if (selector != null && otherSelector != null && Traverse.Create(selector).Field("OtherMenuSelector").GetValue<MenuSelector>() == null)
				Traverse.Create(selector).Field("OtherMenuSelector").SetValue(otherSelector);
			log.LogInfo("[poke] MenuSelector found: " + (selector != null));
			if (selector != null)
			{
				PokeTest.Hand = selector.GetComponentInParent<SteamVRWrapper>();
				PokeTest.Active = true;
				string poked0 = Plugin.Instance.JoinTarget;
				Press(log, page, "Clear");
				foreach (var label in new[] { "7", "2", ".", "1", "DEL", "9" })
					yield return PokeLabel(log, selector, page, label);
				log.LogInfo("[poke] join address after poking 7 2 . 1 DEL 9 = " + Plugin.Instance.JoinTarget + " (expected 72.9)");
				Press(log, page, "Clear");
				Plugin.Instance.JoinTarget = poked0;
				PokeTest.Active = false;
			}

			// ---- keyboard page: hostname with a shifted letter, then the password ----
			var plugin = Plugin.Instance;
			string savedAddress = plugin.JoinTarget, savedPassword = plugin.PasswordText;
			Press(log, page, "Keyboard");
			yield return new WaitForSeconds(1f);
			page = Traverse.Create(mb).Field("Menu").GetValue<GameObject>();
			log.LogInfo("[menu] keyboard page title: " + page.transform.Find("Title").GetComponent<ASCIIText>().DisplayText + ", keys=" + page.GetComponentsInChildren<CoopOption>().Length);
			foreach (var o in page.GetComponentsInChildren<CoopOption>()) { var c = o.GetComponent<BoxCollider>(); if (c == null || !Physics.OverlapSphere(c.bounds.center, 0.0275f, 1 << LayerMask.NameToLayer("Menu")).Any(h => h.gameObject == o.gameObject)) log.LogInfo("[menu] keyboard key not pokeable: " + o.name); }
			PressText(log, page, "CLEAR");
			PressText(log, page, "SHIFT");
			foreach (var ch in "play.example.org") PressText(log, page, ch.ToString());
			log.LogInfo("[menu] keyboard typed address = " + plugin.JoinTarget);
			yield return Shot.CaptureRoutine(Shot.MenuCamera(page.transform, new Vector2(0f, 11f), 13.5f), "menu_5_keyboard");
			PressText(log, page, "DONE");
			yield return new WaitForSeconds(1f);
			page = Traverse.Create(mb).Field("Menu").GetValue<GameObject>();
			Press(log, page, "Password");
			yield return new WaitForSeconds(1f);
			page = Traverse.Create(mb).Field("Menu").GetValue<GameObject>();
			PressText(log, page, "CLEAR");
			foreach (var ch in "pw_1") PressText(log, page, ch.ToString());
			yield return null; // labels refresh in LateUpdate
			log.LogInfo("[menu] keyboard typed password length = " + plugin.PasswordText.Length + ", shown as '" + page.transform.Find("Value").GetComponent<ASCIIText>().DisplayText + "'");
			PressText(log, page, "DONE");
			yield return new WaitForSeconds(1f);
			page = Traverse.Create(mb).Field("Menu").GetValue<GameObject>();
			log.LogInfo("[menu] password button reads: " + page.transform.Find("Password").GetComponent<MenuOption>().DisplayText);
			plugin.JoinTarget = savedAddress; plugin.PasswordText = savedPassword;

			// ---- find games page: a fake host announcing on loopback only ----
			Traverse.Create(plugin).Method("RememberHost", new object[] { "10.0.0.5:27777" }).GetValue();
			Traverse.Create(plugin).Method("RememberHost", new object[] { "play.example.org" }).GetValue();
			var fake = new LanDiscovery.Announcer { Id = "testhost", Name = "Test game", GamePort = 27999, Players = () => 2, Targets = new[] { new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, LanDiscovery.DiscoveryPort) }, Locals = new[] { System.Net.IPAddress.Loopback } };
			fake.Start();
			Press(log, page, "Find");
			yield return new WaitForSeconds(4f);
			page = Traverse.Create(mb).Field("Menu").GetValue<GameObject>();
			yield return Shot.CaptureRoutine(Shot.MenuCamera(page.transform, new Vector2(0f, 8.5f), 13.5f), "menu_4_find_games");
			log.LogInfo("[menu] hosts header: " + page.transform.Find("LanHeader").GetComponent<ASCIIText>().DisplayText);
			log.LogInfo("[menu] hosts row 0: '" + page.transform.Find("Lan0").GetComponent<MenuOption>().DisplayText + "' row 1: '" + page.transform.Find("Lan1").GetComponent<MenuOption>().DisplayText + "'");
			log.LogInfo("[menu] recent rows: '" + page.transform.Find("Recent0").GetComponent<MenuOption>().DisplayText + "', '" + page.transform.Find("Recent1").GetComponent<MenuOption>().DisplayText + "'");
			Press(log, page, "Lan0");
			log.LogInfo("[menu] picked LAN host: join target = " + plugin.JoinTarget + ", session started = " + Plugin.Running);
			plugin.Stop();
			fake.Stop();
			plugin.JoinTarget = savedAddress;
			yield return new WaitForSeconds(1f);
			page = Traverse.Create(mb).Field("Menu").GetValue<GameObject>();
			Press(log, page, "Back");
			yield return new WaitForSeconds(1f);
			var back = Traverse.Create(mb).Field("Menu").GetValue<GameObject>();
			log.LogInfo("[menu] back at main menu: " + (back != null && back.name.StartsWith("BaseMenu")) + " with co-op entry: " + (back != null && back.transform.Find("Co-op") != null));
			plugin.JoinTarget = origAddress; plugin.PasswordText = origPassword; Traverse.Create(plugin).Field("recentHosts").Property("Value").SetValue(origRecent);
		}

		static System.Collections.IEnumerator PokeLabel(ManualLogSource log, MenuSelector selector, GameObject page, string label)
		{
			CoopOption target = null;
			foreach (var o in page.GetComponentsInChildren<CoopOption>())
				if (o.DisplayText == label) { target = o; break; }
			var col = target != null ? target.GetComponent<BoxCollider>() : null;
			if (col == null) { log.LogInfo("[poke] no pokeable option " + label); yield break; }
			Vector3 at = col.bounds.center;
			for (int i = 0; i < 3; i++) { selector.transform.position = at; yield return null; }
			var closest = Traverse.Create(selector).Field("ClosestSelectable").GetValue<GameObject>();
			bool hit = closest == target.gameObject;
			if (!hit)
			{
				var tr = Traverse.Create(selector);
				int near = Physics.OverlapSphere(selector.transform.position, 0.15f, 1 << LayerMask.NameToLayer("Menu")).Length;
				log.LogInfo($"[poke] diag: hand at {selector.transform.position} target {at} pointingMode={tr.Method("IsInPointingMode").GetValue<bool>()} menuColliders<=0.15m={near} other={(tr.Field("OtherMenuSelector").GetValue<MenuSelector>() != null)} vrwrapper={(tr.Field("VRWrapper").GetValue<SteamVRWrapper>() != null)} enabled={selector.enabled} active={selector.gameObject.activeInHierarchy}");
			}
			PokeTest.Pull();
			for (int i = 0; i < 3; i++) { selector.transform.position = at; yield return null; }
			if (!hit) log.LogInfo("[poke] hand at '" + label + "' selected " + (closest != null ? closest.name : "nothing"));
		}

		// Presses the option whose label is `text` (case-insensitive, ignoring a trailing '*').
		static void PressText(ManualLogSource log, GameObject page, string text)
		{
			foreach (var o in page.GetComponentsInChildren<CoopOption>())
				if (string.Equals(o.DisplayText.TrimEnd('*'), text, System.StringComparison.OrdinalIgnoreCase) && o.DisplayText.Length > 0)
				{
					o.Select();
					o.SendMessage("LateUpdate"); // refresh labels now, as the next frame would
					return;
				}
			log.LogInfo("[menu] no option labelled " + text);
		}

		static void Press(ManualLogSource log, GameObject page, string name)
		{
			var o = page.transform.Find(name)?.GetComponent<MenuOption>();
			if (o == null) { log.LogInfo("[menu] missing option " + name); return; }
			o.Select();
		}

		static void Report(ManualLogSource log, Transform t)
		{
			var o = t.GetComponent<MenuOption>();
			if (o == null) { log.LogInfo($"[menu]   label {t.name} text='{t.GetComponent<ASCIIText>()?.DisplayText}'"); return; }
			var col = t.GetComponent<BoxCollider>();
			bool found = false;
			if (col != null)
			{
				// What MenuSelector does: sphere overlap at the fingertip against the Menu layer.
				var hits = Physics.OverlapSphere(col.bounds.center, 0.0275f, 1 << LayerMask.NameToLayer("Menu"));
				found = hits.Any(h => h.gameObject == t.gameObject);
			}
			log.LogInfo($"[menu]   option {t.name} text='{o.DisplayText}' collider={(col != null)} hasMeshRenderer={t.GetComponent<MeshRenderer>() != null} pokeFound={found} layer={LayerMask.LayerToName(t.gameObject.layer)}");
		}
	}
}
