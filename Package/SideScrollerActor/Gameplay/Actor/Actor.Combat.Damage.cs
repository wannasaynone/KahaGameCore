using System.Collections;
using UnityEngine;
using DG.Tweening;
using KahaGameCore.GameEvent;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;
using KahaGameCore.Package.SideScrollerActor.Camera;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay
{
    public partial class Actor
    {
        public void TakeDamage(Actor attacker, Bullet bullet)
        {
            if (IsInvincible)
            {
                return;
            }

            if (currentAttackInfo != null)
            {
                currentAttackInfo = null;
            }

            if (state == State.Reloading)
            {
                EventBus.Publish(new RangeWeapon_OnReloadInterupted() { weaponInstanceID = weaponCapability.GetInstanceID() });
            }

            if (state == State.Defending)
            {
                if (bullet.IsMelee)
                {
                    attacker.PushStun(attacker, 1.5f);

                    GameObject cloneEffect = Instantiate(perfectGuardEffect, bullet.transform.position, Quaternion.identity);
                    cloneEffect.gameObject.SetActive(true);
                    cloneEffect.transform.SetParent(null);
                    Destroy(cloneEffect, 1f);
                }

                return;
            }

            int rawDamage = bullet.damage - actorSetting.defense;

            if (rawDamage <= 0)
            {
                rawDamage = 1;
            }

            SetHealth(currentHealth - rawDamage);
            SetStamina(currentStamina - bullet.deductStamina);

            if (bullet.pauseWhenHit)
            {
                EventBus.Publish(new Game_CallHitPause() { duration = bullet.pauseDurationWhenHit });
            }

            if (currentHealth <= 0f)
            {
                SetHealth(0);
                state = State.Dead;
                EndPrepareAttack();
                animator.Play(dieAnimationName);

                if (transform.position.y > 0f)
                {
                    transform.DOKill();
                    transform.DOMoveY(0f, 0.1f);
                }

                if (currentHurtCoroutine != null)
                {
                    StopCoroutine(currentHurtCoroutine);
                }
            }
            else
            {
                if (currentHurtCoroutine != null)
                {
                    StopCoroutine(currentHurtCoroutine);
                }

                currentHurtCoroutine = IETakeDamage(attacker, bullet);
                StartCoroutine(currentHurtCoroutine);
            }
        }

        private IEnumerator IETakeDamage(Actor attacker, Bullet bullet)
        {
            DisableIsPreparingAttack();

            state = State.Hurting;
            SetFlipX(!attacker.IsFacingRight);

            if (bullet.isHeavyHit && allowKnockBack)
            {
                CameraController.Instance.Shake(0.5f, 0.025f, 10, 0.1f);

                BeThrown(new JumpInfo
                {
                    targetX = transform.position.x + (IsFacingRight ? -bullet.hitForce_power : bullet.hitForce_power),
                    height = 0.5f,
                    duration = bullet.hitForce_duration
                }, false);

                yield break;
            }

            animator.Play(hurtAnimationName);

            if (currentStamina <= 0)
            {
                state = State.Stunned;
                if (downWhenStunned) animator.Play(downAnimationName);
            }

            if (allowKnockBack)
            {
                float targetX;
                if (attacker.IsFacingRight)
                {
                    targetX = Mathf.Clamp(transform.position.x + bullet.hitForce_power, BoardSetter.MIN_X, BoardSetter.MAX_X);
                }
                else
                {
                    targetX = Mathf.Clamp(transform.position.x - bullet.hitForce_power, BoardSetter.MIN_X, BoardSetter.MAX_X);
                }

                transform.DOMoveX(targetX, bullet.hitForce_duration);

                yield return new WaitForSeconds(bullet.hitForce_duration);
            }

            if (state != State.Hurting)
            {
                yield break;
            }

            state = State.Normal;
            SetToIdle();
        }

        public void PushStun(Actor attacker, float duration)
        {
            if (state == State.Reloading)
            {
                EventBus.Publish(new RangeWeapon_OnReloadInterupted() { weaponInstanceID = weaponCapability.GetInstanceID() });
            }

            StartCoroutine(IEPushStun(attacker, duration));
        }

        private IEnumerator IEPushStun(Actor attacker, float duration)
        {
            EventBus.Publish(new Actor_OnStunStarted() { instanceID = GetInstanceID() });

            DisableIsPreparingAttack();

            state = State.Stunned;
            animator.Play(hurtAnimationName);
            float targetX;
            if (attacker.transform.position.x > transform.position.x)
            {
                targetX = Mathf.Clamp(transform.position.x - 0.1f, BoardSetter.MIN_X, BoardSetter.MAX_X);
            }
            else
            {
                targetX = Mathf.Clamp(transform.position.x + 0.1f, BoardSetter.MIN_X, BoardSetter.MAX_X);
            }
            transform.DOMoveX(targetX, duration);
            yield return new WaitForSeconds(duration);

            if (state != State.Stunned)
            {
                yield break;
            }

            state = State.Normal;
            SetToIdle();

            EventBus.Publish(new Actor_OnStunEnded() { instanceID = GetInstanceID() });
        }

        public void Defense()
        {
            if (state != State.Normal
                && state != State.Attacking
                && state != State.Defending)
            {
                return;
            }

            state = State.Defending;
            defenseTimer = 0f;
            StartCoroutine(IEDefense());
        }

        private IEnumerator IEDefense()
        {
            animator.Play(defenseAnimationName);

            while (defenseTimer < actorSetting.defenseDuration)
            {
                defenseTimer += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();

                if (state != State.Defending)
                {
                    yield break;
                }
            }

            animator.Play(weaponCapability.GetIdleAnimationName());
            state = State.Normal;
        }

        private void RecoverFullHealth(float recoverTime)
        {
            StartCoroutine(IERecoverFullHealth(recoverTime));
        }

        private IEnumerator IERecoverFullHealth(float recoverTime)
        {
            state = State.Recovering;

            currentMaxHealth = actorSetting.health;

            float totalFrame = recoverTime / Time.fixedDeltaTime;
            float addHealthPerFrame = (float)currentMaxHealth / totalFrame;
            for (int i = 0; i < totalFrame; i++)
            {
                SetHealth(currentHealth + (int)addHealthPerFrame);
                yield return new WaitForFixedUpdate();
            }

            SetHealth(currentMaxHealth);

            state = State.Normal;
            SetToIdle();
        }
    }
}
