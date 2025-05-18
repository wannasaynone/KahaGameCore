using System.Collections;
using UnityEngine;
using KahaGameCore.GameEvent;
using KahaGameCore.Package.SideScrollerActor.WeaponScripts;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay
{
    public partial class Actor
    {
        public void StartPrepareAttack()
        {
            if (weaponCapability == null)
            {
                Debug.LogError("No weapon found in Actor: " + gameObject.name);
                return;
            }

            IAttackInfo attackInfo = weaponCapability.PeekNextAttackInfo();
            if (attackInfo == null || string.IsNullOrEmpty(attackInfo.GetPrepareAnimationName()))
            {
                return;
            }

            if (state != State.Normal)
            {
                isHoldingPrepareAttackButton = true;
                return;
            }

            if (attackInfo.GetAnimatorController() != null)
            {
                cachedOriginalController = animator.runtimeAnimatorController;
                animator.runtimeAnimatorController = attackInfo.GetAnimatorController();
            }

            animator.Play(attackInfo.GetPrepareAnimationName());
            isPreparingAttack = true;
            isHoldingPrepareAttackButton = true;

            GameObject enableWhenPrepare = attackInfo.GetEnableWhenPrepare();
            if (enableWhenPrepare != null)
            {
                cachedEnabledPrepareGameObject = enableWhenPrepare;
                cachedEnabledPrepareGameObject.gameObject.SetActive(true);
            }
        }

        public void EndPrepareAttack()
        {
            DisableIsPreparingAttack();
            isHoldingPrepareAttackButton = false;

            if (state == State.Normal)
            {
                animator.Play(weaponCapability.GetIdleAnimationName());
            }
        }

        private void DisableIsPreparingAttack()
        {
            isPreparingAttack = false;

            // 恢復原始的 Animator Controller
            if (cachedOriginalController != null && currentAttackInfo == null)
            {
                animator.runtimeAnimatorController = cachedOriginalController;
                cachedOriginalController = null;
            }

            if (cachedEnabledPrepareGameObject != null && currentAttackInfo == null)
            {
                cachedEnabledPrepareGameObject.gameObject.SetActive(false);
                cachedEnabledPrepareGameObject = null;
            }
        }

        private bool IsBlockingAttack()
        {
            return (state == State.Attacking && (currentAttackInfo == null || attackTimer < currentAttackInfo.GetAllowNextAttackTime()))
                || (state != State.Normal
                    && state != State.Running
                    && state != State.SimpleJumping
                    && state != State.Dashing
                    && state != State.Extension
                    && state != State.Hurting
                    && state != State.JumpingToTargetX
                    && state != State.WaitInitialize
                    && state != State.Flying
                    && state != State.Reloading
                    && state != State.Recovering
                    && state != State.Defending
                    && state != State.Stunned
                    && state != State.SimplePauseMove);
        }

        private void CreateNoStaminaHint()
        {
            Utlity.GeneralHintDisplayer.Instance.Create("No Stamina", transform.position, new Color(87f / 255f, 107f / 255f, 255f / 255f, 1f));
        }

        public void AttackWithWeapon()
        {
            if (IsBlockingAttack())
            {
                return;
            }

            if (weaponCapability == null)
            {
                Debug.LogError("No weapon found in Actor: " + gameObject.name);
                return;
            }

            IAttackInfo attackInfo = weaponCapability.PeekNextAttackInfo();
            if (attackInfo == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(attackInfo.GetPrepareAnimationName()) && !isPreparingAttack)
            {
                return;
            }

            if (attackInfo.GetStaminaCost() > currentStamina)
            {
                CreateNoStaminaHint();
                return;
            }

            if (attackInfo.GetHealthCost() > currentHealth)
            {
                return;
            }

            Attack(weaponCapability.GetNextAttackInfo());
        }

        public void Attack(IAttackInfo attackInfo)
        {
            if (attackInfo == null)
            {
                return;
            }

            if (IsBlockingAttack())
            {
                return;
            }

            if (attackInfo.GetStaminaCost() > currentStamina)
            {
                CreateNoStaminaHint();
                return;
            }

            if (attackInfo.GetHealthCost() > currentHealth)
            {
                return;
            }

            currentAttackInfo = attackInfo;

            if (currentAttackInfo == null)
            {
                return;
            }

            attackTimer = 0f;
            StartCoroutine(IEAttack());
        }

        private IEnumerator IEAttack()
        {
            state = State.Attacking;

            if (weaponCapability.IsRangeWeapon())
            {
                EventBus.Publish(new Weapon_CallSimplePingPong() { weaponInstanceID = weaponCapability.GetInstanceID() });
            }

            TickLockDirection(true);

            if (cachedEnabledPrepareGameObject != null)
            {
                cachedEnabledPrepareGameObject.gameObject.SetActive(false);
                cachedEnabledPrepareGameObject = null;
            }

            if (currentAttackInfo.GetEnableWhenAttacking() != null)
            {
                currentAttackInfo.GetEnableWhenAttacking().SetActive(true);
                cachedEnabledAttackGameObject = currentAttackInfo.GetEnableWhenAttacking();
            }

            bool createdBullet = false;

            RuntimeAnimatorController originalController = null;

            if (!isPreparingAttack && currentAttackInfo.GetAnimatorController() != null)
            {
                originalController = animator.runtimeAnimatorController;
                animator.runtimeAnimatorController = currentAttackInfo.GetAnimatorController();
            }

            animator.Play(currentAttackInfo.GetAnimationName());
            while (currentAttackInfo != null && attackTimer <= currentAttackInfo.GetDuration())
            {
                attackTimer += Time.fixedDeltaTime;

                if (!createdBullet && attackTimer >= currentAttackInfo.GetCreateBulletTime() && state == State.Attacking)
                {
                    createdBullet = true;
                    CreateBulletWithCurrentAttackInfo();
                }

                if (state != State.Attacking)
                {
                    if (cachedEnabledAttackGameObject != null)
                    {
                        cachedEnabledAttackGameObject.gameObject.SetActive(false);
                        cachedEnabledAttackGameObject = null;
                    }

                    if (originalController != null)
                    {
                        animator.runtimeAnimatorController = originalController;
                    }

                    yield break;
                }

                yield return new WaitForFixedUpdate();
            }

            if (cachedEnabledAttackGameObject != null)
            {
                cachedEnabledAttackGameObject.gameObject.SetActive(false);
                cachedEnabledAttackGameObject = null;
            }

            if (state != State.Attacking)
            {
                // 如果攻擊被中斷，恢復原始的 Animator Controller
                if (originalController != null)
                {
                    animator.runtimeAnimatorController = originalController;
                }
                yield break;
            }

            // 攻擊結束，恢復原始的 Animator Controller
            if (originalController != null)
            {
                animator.runtimeAnimatorController = originalController;
            }

            if (IsGrounded)
            {
                state = State.Normal;

                if (isHoldingPrepareAttackButton)
                {
                    StartPrepareAttack();
                }
                else
                {
                    SetToIdle();
                }
            }
            else
            {
                ContinueSimpleJump();
            }

            currentAttackInfo = null;
        }
    }
}
