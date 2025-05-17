using System.Collections;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;
using KahaGameCore.Package.SideScrollerActor.WeaponScripts;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay
{
    public partial class Actor
    {
        public void SetToIdle(bool forceStop = false)
        {
            isPressingLeft = false;
            isPressingRight = false;

            if (!forceStop && state != State.Normal && state != State.Running)
            {
                return;
            }

            if (currentActorExtension != null)
            {
                currentActorExtension.ForceEnd();
            }

            state = State.Normal;

            if (isHoldingPrepareAttackButton && !isPreparingAttack)
            {
                StartPrepareAttack();
            }

            if (isPreparingAttack)
            {
                if (weaponCapability != null)
                {
                    IAttackInfo nextAttackInfo = weaponCapability.PeekNextAttackInfo();

                    if (nextAttackInfo == null || string.IsNullOrEmpty(nextAttackInfo.GetPrepareAnimationName()))
                    {
                        animator.Play(weaponCapability.GetIdleAnimationName());
                    }
                    else
                    {
                        animator.Play(nextAttackInfo.GetPrepareAnimationName());
                    }
                }
                else
                {
                    animator.Play(weaponCapability.GetIdleAnimationName());
                }
            }
            else
            {
                if (cachedEnabledPrepareGameObject != null)
                {
                    cachedEnabledPrepareGameObject.gameObject.SetActive(false);
                    cachedEnabledPrepareGameObject = null;
                }

                animator.Play(weaponCapability.GetIdleAnimationName());
            }

            TickLockDirection(true);
        }

        public void SetForceInvincible(float time)
        {
            if (state == State.Dead)
            {
                return;
            }

            if (isForceInvincible)
            {
                return;
            }

            isForceInvincible = true;
            StartCoroutine(IESetForceInvincible(time));
        }

        private IEnumerator IESetForceInvincible(float time)
        {
            spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);
            yield return new WaitForSeconds(time);
            isForceInvincible = false;
            spriteRenderer.color = new Color(1f, 1f, 1f, 1f);
        }

        public void SetWaitingInteractObject(IInteractableObject interactableObject)
        {
            if (interactableObject == null)
            {
                return;
            }

            waitInteractableObject = interactableObject;
        }

        public void Interact()
        {
            if (waitInteractableObject == null)
            {
                return;
            }

            if (state != State.Normal && state != State.Running)
            {
                return;
            }

            waitInteractableObject.Interact();
        }
    }
}
