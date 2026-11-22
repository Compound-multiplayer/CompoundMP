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
			var mb = Resources.FindObjectsOfTypeAll<MenuButton>().FirstOrDefault(b => b.MenuPrefab != null && b.MenuTransform != null);
			if (mb == null) { log.LogInfo("[menu] no MenuButton"); yield break; }
			// Headless, the wrist button is never "ready" and MenuButton.Update would close the menu every frame.
			mb.enabled = false;
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
			var cam = Shot.MenuCamera(menu.transform, new Vector2(0f, 11f), 13f);
			Shot.Capture(cam, "menu_1_main");
			entry.GetComponent<MenuOption>().Select();
			yield return new WaitForSeconds(1f);

			var page = Traverse.Create(mb).Field("Menu").GetValue<GameObject>();
			log.LogInfo("[menu] page opened: " + page.name + " children=" + page.transform.childCount);
			foreach (Transform c in page.transform) Report(log, c);
			cam = Shot.MenuCamera(page.transform, new Vector2(4.5f, 8.5f), 13f);
			yield return null;
			Shot.Capture(cam, "menu_2_coop_page");

			Press(log, page, "Clear");
			foreach (var k in new[] { "1", "9", "2", ".", "1", "6", "8", ".", "0", ".", "5" }) Press(log, page, "Key" + k);
			Press(log, page, "Del");
			log.LogInfo("[menu] typed address = " + Plugin.Instance.JoinTarget);
			Press(log, page, "Upnp"); Press(log, page, "Upnp");
			Press(log, page, "Host");
			yield return new WaitForSeconds(2f);
			foreach (var l in page.GetComponentsInChildren<CoopLabel>()) log.LogInfo("[menu] label " + l.name + " = '" + l.DisplayText + "'");
			Shot.Capture(cam, "menu_3_hosting");
			log.LogInfo("[menu] running=" + Plugin.Running + " host=" + Plugin.IsHost);
			Press(log, page, "Stop");
			log.LogInfo("[menu] after stop running=" + Plugin.Running);
			Press(log, page, "Back");
			yield return new WaitForSeconds(1f);
			var back = Traverse.Create(mb).Field("Menu").GetValue<GameObject>();
			log.LogInfo("[menu] back at main menu: " + (back != null && back.name.StartsWith("BaseMenu")) + " with co-op entry: " + (back != null && back.transform.Find("Co-op") != null));
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
