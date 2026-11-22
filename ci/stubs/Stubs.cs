// Reference-only stand-in for the game's Assembly-CSharp: signatures the plugin uses, empty bodies.
#pragma warning disable
using System;
using System.Collections.Generic;
using UnityEngine;

public class ASCIIText : MonoBehaviour {
	public enum TextAlignment
	{
		AlignLeft,
		AlignCentre,
		AlignRight
	}
	public string DisplayText;
	public void UpdateDisplay() { }
	public GameObject ASCIITextObject;
	public TextAlignment Alignment;
	public int MaxLength;
}

public class BaseMenu : MonoBehaviour {
}

public class BulletController : MonoBehaviour {
}

public class Damageable : MonoBehaviour {
	public void TakeDamage(int damage, Collider hitCollider = default, GameObject damageDealer = default, bool neverLessThanDefault = default) { }
	public bool IsDead() { throw null; }
	public int MaxHealth;
	public void Revive() { }
	public bool IsImmortal;
	public float InvincibilityCoolDown;
	public int GetCurrentHealth() { throw null; }
	public bool OnDamageCoolDown() { throw null; }
}

public class EnemyController : MonoBehaviour {
	public virtual void MakeAwareOfPlayer() { }
	public bool IsAlert;
}

public class GameController : MonoBehaviour {
	public int RandomSeed;
	public MapFader MapFaderController;
	public MapInfo CurrentMapInfo;
	public bool RandomSeedEnabled;
	public MessageDisplay MessageSystem;
	public GameObject ShieldPowerupEffect;
}

public class GlobalGameInfo : Singleton<GlobalGameInfo> {
	public string CurrentMapName;
	public MapBase Map;
	public GameController GameControllerInstance;
	public PlayerController PlayerControllerInstance;
	public GameObject VRCameraEye;
	public Settings GameSettings;
	public GameObject LeftController;
	public GameObject RightController;
}

public class GrowWhenFacing : MonoBehaviour {
	public void SetOverrideBig(bool overrideBig) { }
}

public class HitscanBulletController : BulletController {
}

public class MapBase : MonoBehaviour {
	public List<Damageable> Enemies;
	public virtual bool AllEnemiesDead() { throw null; }
}

public class MapFader : MonoBehaviour {
	public delegate void FadeEventHandler();
	public bool IsFading;
	public FadeEventHandler OnFadeOutComplete;
	public void FadeOut() { }
}

public class MapGenerator : MapBase {
}

public class MapInfo {
}

public class MapNavigator : MonoBehaviour {
}

public class MenuButton : MonoBehaviour {
	public void SwitchMenu(Submenu submenu) { }
	public enum Submenu
	{
		BaseMenu,
		GameMenu,
		MovementMenu,
		TurningMenu,
		AudioMenu,
		PerformanceMenu
	}
	public Transform MenuTransform;
	public GameObject MenuPrefab;
	public GrowWhenFacing Grower;
}

public class MenuOption : ASCIIText, MenuOptionInterface {
	public virtual void Select() { }
	public GameObject ASCIITextNormal;
	public GameObject ASCIITextSelected;
	public string DisplayText;
	public GameObject ASCIITextObject;
	public string[] Options;
	public TextAlignment Alignment;
	public string _ToolTipText;
}

public class MenuOptionChangeMenu : MenuOption {
}

public interface MenuOptionInterface {
}

public class MenuSelector : MonoBehaviour {
}

public class MessageDisplay : MonoBehaviour {
	public void AddMessage(string message) { }
}

public class NextLevelTrigger : MonoBehaviour {
	public string NextMapName;
	public bool RequireAllEnemiesDead;
	public void ChangeLevel() { }
}

public class PlayerController : MonoBehaviour {
	public bool IsDead() { throw null; }
	public Damageable DamageableComponent { get { throw null; } set { } }
}

public class Settings {
	public bool IMMORTAN;
}

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour {
	public static T Instance { get { throw null; } set { } }
}

public class SteamManager : MonoBehaviour {
	public static bool Initialized;
}

public class SteamVRWrapper : MonoBehaviour {
}
