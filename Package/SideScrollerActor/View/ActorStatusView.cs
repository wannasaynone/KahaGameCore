using KahaGameCore.GameEvent;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;
using KahaGameCore.Package.SideScrollerActor.WeaponScripts;
using UnityEngine;
using UnityEngine.UI;

namespace KahaGameCore.Package.SideScrollerActor.View
{
    public class ActorStatusView : MonoBehaviour
    {
        [SerializeField] private Object actor;
        [SerializeField] private GameObject ammoTextObject;
        [SerializeField] private TMPro.TextMeshProUGUI ammoText;
        [SerializeField] private Image reloadHintImage;
        [SerializeField] private Image attackDurationHintImage;
        [SerializeField] private GameObject staminaBarObject;
        [SerializeField] private Image staminaBarImage;

        private IWeaponCapability weapon;

        private void Awake()
        {
            EventBus.Subscribe<Actor_OnWeaponChanged>(OnWeaponSwitched);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<RangeWeapon_OnAmmoAmountChanged>(OnRangeWeaponAmmoAmountChanged);
            EventBus.Subscribe<RangeWeapon_OnReloading>(OnRangeWeaponReloading);
            EventBus.Subscribe<Weapon_AttackDurationChanged>(OnWeaponAttackDurationChanged);
            EventBus.Subscribe<Actor_OnStaminaChanged>(OnActorStaminaChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RangeWeapon_OnAmmoAmountChanged>(OnRangeWeaponAmmoAmountChanged);
            EventBus.Unsubscribe<RangeWeapon_OnReloading>(OnRangeWeaponReloading);
            EventBus.Unsubscribe<Weapon_AttackDurationChanged>(OnWeaponAttackDurationChanged);
            EventBus.Unsubscribe<Actor_OnStaminaChanged>(OnActorStaminaChanged);
        }

        private void OnWeaponSwitched(Actor_OnWeaponChanged onWeaponSwitched)
        {
            if (actor == null)
            {
                return;
            }

            if (onWeaponSwitched.actorInstanceID != actor.GetInstanceID())
            {
                return;
            }

            weapon = onWeaponSwitched.weapon;

            if (weapon is not RangeWeapon)
            {
                ammoTextObject.SetActive(false);
            }
            else
            {
                ammoTextObject.SetActive(true);
                OnRangeWeaponAmmoAmountChanged(new RangeWeapon_OnAmmoAmountChanged()
                {
                    weaponInstanceID = weapon.GetInstanceID(),
                    currentAmmo = (weapon as RangeWeapon).CurrentAmmo,
                    maxAmmo = (weapon as RangeWeapon).MaxAmmo,
                    remainingAmmo = (weapon as RangeWeapon).RemainingAmmo
                });
            }
        }

        private void OnRangeWeaponAmmoAmountChanged(RangeWeapon_OnAmmoAmountChanged changed)
        {
            if (weapon == null)
            {
                return;
            }

            if (changed.weaponInstanceID != weapon.GetInstanceID())
            {
                return;
            }

            ammoText.text = $"{changed.currentAmmo}/{changed.maxAmmo}";
            if (changed.remainingAmmo >= 0)
            {
                ammoText.text += $" ({changed.remainingAmmo})";
            }
        }

        private void OnRangeWeaponReloading(RangeWeapon_OnReloading reloading)
        {
            if (weapon == null)
            {
                return;
            }

            if (reloading.weaponInstanceID != weapon.GetInstanceID())
            {
                return;
            }

            reloadHintImage.fillAmount = reloading.currentReloadTime / reloading.maxReloadTime;
            reloadHintImage.gameObject.SetActive(reloading.currentReloadTime > 0);
        }

        private void OnWeaponAttackDurationChanged(Weapon_AttackDurationChanged changed)
        {
            if (weapon == null)
            {
                return;
            }

            if (changed.weaponInstanceID != weapon.GetInstanceID())
            {
                return;
            }

            attackDurationHintImage.fillAmount = changed.currentAttackDuration / changed.maxAttackDuration;
            attackDurationHintImage.gameObject.SetActive(changed.currentAttackDuration <= changed.maxAttackDuration);
        }

        private void OnActorStaminaChanged(Actor_OnStaminaChanged e)
        {
            if (e.instanceID != actor.GetInstanceID())
            {
                return;
            }

            staminaBarImage.fillAmount = (float)e.currentStamina / e.maxStamina;
            staminaBarObject.SetActive(e.currentStamina < e.maxStamina);
        }
    }
}