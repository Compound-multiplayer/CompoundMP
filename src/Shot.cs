using System.IO;
using UnityEngine;

namespace CompoundMP
{
	// Development helper: renders a camera into a texture and saves a PNG (works without a visible window).
	static class Shot
	{
		public static string Dir = "";

		// Lets the camera render through the normal frame loop (an enabled camera with a target texture), then reads the result.
		public static System.Collections.IEnumerator CaptureRoutine(Camera cam, string name, int w = 1280, int h = 720)
		{
			if (Dir == "") yield break;
			var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
			cam.targetTexture = rt;
			cam.enabled = true;
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame();
			Save(rt, name, w, h);
			cam.enabled = false;
			cam.targetTexture = null;
			rt.Release(); Object.Destroy(rt);
		}

		static void Save(RenderTexture rt, string name, int w, int h)
		{
			var prev = RenderTexture.active;
			RenderTexture.active = rt;
			var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
			tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
			tex.Apply();
			RenderTexture.active = prev;
			File.WriteAllBytes(Path.Combine(Dir, name + ".png"), tex.EncodeToPNG());
			Plugin.Log.LogInfo("[shot] saved " + name + ".png, centre pixel " + tex.GetPixel(w / 2, h / 2));
			Object.Destroy(tex);
		}

		public static void Capture(Camera cam, string name, int w = 1280, int h = 720)
		{
			if (Dir == "") return;
			var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
			var old = cam.targetTexture;
			cam.targetTexture = rt;
			cam.Render();
			var prev = RenderTexture.active;
			RenderTexture.active = rt;
			var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
			tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
			tex.Apply();
			RenderTexture.active = prev;
			cam.targetTexture = old;
			File.WriteAllBytes(Path.Combine(Dir, name + ".png"), tex.EncodeToPNG());
			Object.Destroy(tex); rt.Release(); Object.Destroy(rt);
			Plugin.Log.LogInfo("[shot] saved " + name + ".png, centre pixel " + tex.GetPixel(w / 2, h / 2));
		}

		// Orthographic camera looking at a menu laid out in its local XY plane (viewer on -Z, like the player).
		public static Camera MenuCamera(Transform menu, Vector2 centre, float halfHeightUnits)
		{
			var go = new GameObject("ShotCam");
			var cam = go.AddComponent<Camera>();
			float s = menu.lossyScale.x;
			go.transform.position = menu.TransformPoint(new Vector3(centre.x, centre.y, -20f));
			go.transform.rotation = menu.rotation;
			cam.orthographic = true;
			cam.orthographicSize = halfHeightUnits * s;
			cam.nearClipPlane = 0.01f; cam.farClipPlane = 100f * s + 50f;
			cam.clearFlags = CameraClearFlags.SolidColor;
			cam.backgroundColor = new Color(0.05f, 0.06f, 0.08f);
			cam.cullingMask = 1 << LayerMask.NameToLayer("Menu") | 1;
			cam.enabled = false;
			return cam;
		}
	}
}
