using KahaGameCore.GameEvent;

namespace KahaGameCore.Package.SideScrollerActor.InGameEvent
{
    public class EnhanceView_OnNextButtonClicked : GameEventBase { }

    public class EnhanceView_OnLevelUpButtonPressed : GameEventBase
    {
        public string selectedMonsterGuid;
    }
    public class EnhanceView_OnInfoButtonPressed : GameEventBase
    {
        public string selectedMonsterGuid;
    }
    public class EnhanceView_OnHeroInfoButtonPressed : GameEventBase { }

    public class GameEndView_OnConfirmClicked : GameEventBase { }

    public class Actor_OnHealthChanged : GameEventBase
    {
        public int instanceID;
        public int currentHealth;
        public int maxHealth;
    }

    public class Actor_OnStaminaChanged : GameEventBase
    {
        public int instanceID;
        public int currentStamina;
        public int maxStamina;
    }

    public class Actor_OnJumpEnded : GameEventBase
    {
        public int instanceID;
    }

    public class Actor_OnStunStarted : GameEventBase
    {
        public int instanceID;
    }

    public class Actor_OnStunEnded : GameEventBase
    {
        public int instanceID;
    }

    public class RangeWeapon_OnAmmoAmountChanged : GameEventBase
    {
        public int weaponInstanceID;
        public int currentAmmo;
        public int maxAmmo;
        public int remainingAmmo;
    }

    public class RangeWeapon_OnReloading : GameEventBase
    {
        public int weaponInstanceID;
        public float currentReloadTime;
        public float maxReloadTime;
    }

    public class Weapon_AttackDurationChanged : GameEventBase
    {
        public int weaponInstanceID;
        public float currentAttackDuration;
        public float maxAttackDuration;
    }

    public class RangeWeapon_OnReloadInterupted : GameEventBase
    {
        public int weaponInstanceID;
    }

    public class DesireController_OnAddDesireCalled : GameEventBase
    {
        public float addDesireDelta;
    }

    public class DesireController_OnDesireChanged : GameEventBase
    {
        public int desireValue;
    }

    public class InGameItem_OnAmountChanged : GameEventBase
    {
        public int itemID;
        public int addAmount;
    }

    public class InteractableObject_OnInteractedWithButton : GameEventBase
    {
        public string interactedCommand;
    }

    public class OnItemRecordChanged : GameEventBase
    {
        public int itemID;
        public int currentAmount;
    }

    public class WeaponSwitcher_OnWeaponRequested : GameEventBase
    {
        public int actorInstanceID;
        public WeaponScripts.IWeaponCapability weapon;
    }

    public class Actor_OnWeaponChanged : GameEventBase
    {
        public int actorInstanceID;
        public WeaponScripts.IWeaponCapability weapon;
    }

    public class TitleView_OnStartGameRequested : GameEventBase
    {
        public string sequenceName;
    }

    public class Game_TriggerSequence : GameEventBase
    {
        public string sequenceName;
    }

    public class Game_GameEnd : GameEventBase
    {
        public bool isWin;
    }

    public class Game_CallHitPause : GameEventBase
    {
        public float duration;
    }

    public class Game_OnPortalEntered : GameEventBase
    {
        public int actorInstanceID;
        public UnityEngine.Vector3 targetPosition;
        public bool isBackPortal;
        public bool enableWhiteNoise;
        public float board_max;
        public float board_min;
        public UnityEngine.AudioClip enterSound;
#if UNIVERSAL_PIPELINE_CORE_INCLUDED
        public UnityEngine.Rendering.VolumeProfile volumeProfile;
#endif
        public UnityEngine.AudioClip backgroundMusic;
    }

    public class Game_OnNextLevelRequested : GameEventBase
    {
        public string nextLevelName;
        public int heroHealth;
        public float heroStamina;
    }

    public class Game_OnLevelLoaded : GameEventBase
    {
        public string levelName;
    }

    public class Weapon_CallSimplePingPong : GameEventBase
    {
        public int weaponInstanceID;
    }
}
