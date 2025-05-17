using System.Collections;
using UnityEngine;
using KahaGameCore.GameEvent;
using KahaGameCore.Package.SideScrollerActor.WeaponScripts;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay
{
    public partial class Actor
    {
        public void SetWeapon(IWeaponCapability weapon)
        {
            if (!IsControlable)
            {
                return;
            }

            this.weaponCapability = weapon;

            DisableIsPreparingAttack();

            if (!isPressingLeft && !isPressingRight)
            {
                SetToIdle();
            }

            EventBus.Publish(new Actor_OnWeaponChanged()
            {
                actorInstanceID = GetInstanceID(),
                weapon = weapon
            });
        }

        public void Reload()
        {
            if (weaponCapability == null)
            {
                return;
            }

            if (!IsControlable)
            {
                return;
            }

            if (weaponCapability.IsRangeWeapon())
            {
                weaponCapability.Reload();
            }
        }

        private void OnReloading(RangeWeapon_OnReloading e)
        {
            if (weaponCapability == null)
            {
                return;
            }

            if (!IsControlable)
            {
                return;
            }

            if (weaponCapability.GetInstanceID() == e.weaponInstanceID)
            {
                StartCoroutine(IEReload(e.maxReloadTime));
            }
        }

        private IEnumerator IEReload(float reloadTime)
        {
            DisableIsPreparingAttack();

            state = State.Reloading;
            animator.Play(weaponCapability.GetReloadAnimationName());
            yield return new WaitForSeconds(reloadTime);

            if (state != State.Reloading)
            {
                yield break;
            }

            state = State.Normal;
            SetToIdle();
        }
    }
}
