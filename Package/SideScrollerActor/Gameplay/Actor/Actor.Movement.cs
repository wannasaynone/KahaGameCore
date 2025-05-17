using System.Collections;
using UnityEngine;
using DG.Tweening;
using KahaGameCore.GameEvent;
using KahaGameCore.Package.SideScrollerActor.Camera;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;
using KahaGameCore.Package.SideScrollerActor.WeaponScripts;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay
{
    public partial class Actor
    {
        public void MoveRight()
        {
            isPressingRight = true;

            if (state != State.Normal && state != State.Running)
            {
                return;
            }

            state = State.Normal;

            SetFlipX(true);
            float moveSpeed = GetCurrentMoveSpeed(true);

            if (moveSpeed <= 0f)
            {
                return;
            }

            AddPosition(moveSpeed * Time.deltaTime * Vector3.right);
            PlayMoveAnimation(true);
        }

        public void MoveLeft()
        {
            isPressingLeft = true;

            if (state != State.Normal && state != State.Running)
            {
                return;
            }

            state = State.Normal;

            SetFlipX(false);
            float moveSpeed = GetCurrentMoveSpeed(false);

            if (moveSpeed <= 0f)
            {
                return;
            }

            AddPosition(moveSpeed * Time.deltaTime * Vector3.left);
            PlayMoveAnimation(false);
        }

        public void RunRight()
        {
            if (state != State.Normal && state != State.Running)
            {
                return;
            }

            state = State.Running;
            DisableIsPreparingAttack();
            AddPosition(actorSetting.runSpeed * Time.deltaTime * Vector3.right);
            SetStamina(currentStamina - actorSetting.runCostPerSecond * Time.deltaTime);

            if (state != State.SimpleJumping)
            {
                SetFlipX(true);
                animator.Play(weaponCapability.GetRunAnimationName());
            }
        }

        public void RunLeft()
        {
            if (state != State.Normal && state != State.Running)
            {
                return;
            }

            state = State.Running;
            DisableIsPreparingAttack();
            AddPosition(actorSetting.runSpeed * Time.deltaTime * Vector3.left);
            SetStamina(currentStamina - actorSetting.runCostPerSecond * Time.deltaTime);

            if (state != State.SimpleJumping)
            {
                SetFlipX(false);
                animator.Play(weaponCapability.GetRunAnimationName());
            }
        }

        public void DashRight()
        {
            if (!allowDash)
            {
                return;
            }

            if ((state != State.Normal && state != State.SimpleJumping) || dashCooldownTimer > 0f)
            {
                return;
            }

            state = State.Dashing;
            StartCoroutine(IEDash(true));
        }

        public void DashLeft()
        {
            if (!allowDash)
            {
                return;
            }

            if ((state != State.Normal && state != State.SimpleJumping) || dashCooldownTimer > 0f)
            {
                return;
            }

            state = State.Dashing;
            StartCoroutine(IEDash(false));
        }

        public void SimpleJumpUp()
        {
            if (!allowJump)
            {
                return;
            }

            if (state != State.Normal)
            {
                return;
            }

            state = State.SimpleJumping;
            StartCoroutine(IESimpleJumpUp(actorSetting.simpleJumpAddY, actorSetting.simpleJumpDuration));
        }

        public void JumpTo(JumpInfo jumpInfo)
        {
            if (state != State.Normal)
            {
                return;
            }

            state = State.JumpingToTargetX;
            StartCoroutine(IEJumpTo(jumpInfo.targetX, jumpInfo.height, jumpInfo.duration));
        }

        public void PauseMove(bool pause)
        {
            if (pause)
            {
                pauseLocks.Add(new object());
            }
            else
            {
                if (pauseLocks.Count > 0)
                {
                    pauseLocks.RemoveAt(pauseLocks.Count - 1);
                }
            }

            if (pauseLocks.Count > 0)
            {
                animator.Play(weaponCapability.GetIdleAnimationName());
                if (cachedEnabledPrepareGameObject != null)
                {
                    cachedEnabledPrepareGameObject.gameObject.SetActive(false);
                    cachedEnabledPrepareGameObject = null;
                }
                isPreparingAttack = false;
                state = State.SimplePauseMove;
            }
            else
            {
                state = State.Normal;
            }
        }

        private float GetCurrentMoveSpeed(bool isGoingRight)
        {
            if (state == State.SimplePauseMove)
            {
                return 0f;
            }

            float moveSpeed = actorSetting.moveSpeed;

            if (isGoingRight && !IsFacingRight)
            {
                moveSpeed = actorSetting.backSpeed;
            }
            else if (!isGoingRight && IsFacingRight)
            {
                moveSpeed = actorSetting.backSpeed;
            }

            if (isPreparingAttack)
            {
                if (currentAttackInfo != null)
                {
                    moveSpeed += currentAttackInfo.GetAddMoveSpeedWhenPrepare();
                }
                else if (weaponCapability != null)
                {
                    IAttackInfo attackInfo = weaponCapability.PeekNextAttackInfo();
                    if (attackInfo != null)
                    {
                        moveSpeed += attackInfo.GetAddMoveSpeedWhenPrepare();
                    }
                }
            }

            return moveSpeed;
        }

        private void AddPosition(Vector3 add)
        {
            SetPosition(transform.position + add);
        }

        private void SetPosition(Vector3 pos)
        {
            if (pos.x > BoardSetter.MAX_X)
            {
                pos.x = BoardSetter.MAX_X;
            }
            else if (pos.x < BoardSetter.MIN_X)
            {
                pos.x = BoardSetter.MIN_X;
            }

            transform.position = pos;
        }

        private void ContinueSimpleJump()
        {
            state = State.SimpleJumping;
            StartCoroutine(IESimpleFall());
        }

        private IEnumerator IEDash(bool isGoingRight)
        {
            float time = 0f;

            DisableIsPreparingAttack();
            SetFlipX(isGoingRight);

            ParticleSystemRenderer cloneEffect;

            if (spriteRenderer.flipX == !isGoingRight)
            {
                animator.Play(dashAnimationName_front);
                cloneEffect = Instantiate(dashEffect_front, dashEffect_front.transform.position, Quaternion.identity);
                cloneEffect.gameObject.SetActive(true);
                cloneEffect.transform.SetParent(null);
            }
            else
            {
                animator.Play(dashAnimationName_back);
                cloneEffect = Instantiate(dashEffect_back, dashEffect_front.transform.position, Quaternion.identity);
                cloneEffect.gameObject.SetActive(true);
                cloneEffect.transform.SetParent(null);
            }

            Destroy(cloneEffect.gameObject, actorSetting.dashDuration + 0.5f);

            Actor opponent = ActorContainer.GetActorByCamp(camp == Camp.Monster ? Camp.Hero : Camp.Monster);
            bool isOpponentInFront = opponent != null && ((IsFacingRight && opponent.transform.position.x > transform.position.x) || (!IsFacingRight && opponent.transform.position.x < transform.position.x));
            bool isDashingToOpponent = opponent != null && ((isGoingRight && opponent.transform.position.x > transform.position.x) || (!isGoingRight && opponent.transform.position.x < transform.position.x));

            while (time < actorSetting.dashDuration)
            {
                Vector3 nextPositon = transform.position + (isGoingRight ? Vector3.right : Vector3.left) * actorSetting.dashSpeed * Time.fixedDeltaTime;

                if (isOpponentInFront && isDashingToOpponent && opponent.transform.position.y <= 0.1f && transform.position.y <= 0.1f)
                {
                    if (isGoingRight && nextPositon.x > opponent.transform.position.x + 1f)
                    {
                        nextPositon.x = opponent.transform.position.x + 1f;
                        SetPosition(nextPositon);
                        cloneEffect.transform.position = dashEffect_front.transform.position;
                        break;
                    }
                    else if (!isGoingRight && nextPositon.x < opponent.transform.position.x - 1f)
                    {
                        nextPositon.x = opponent.transform.position.x - 1f;
                        SetPosition(nextPositon);
                        cloneEffect.transform.position = dashEffect_front.transform.position;
                        break;
                    }
                }

                SetPosition(nextPositon);
                cloneEffect.transform.position = dashEffect_front.transform.position;
                time += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
                if (state != State.Dashing)
                {
                    yield break;
                }
            }

            if (IsGrounded)
            {
                state = State.Normal;
                SetToIdle();
            }
            else
            {
                ContinueSimpleJump();
            }

            dashCooldownTimer = actorSetting.dashCooldown;
        }

        private IEnumerator IESimpleJumpUp(float addY, float duration)
        {
            DisableIsPreparingAttack();

            animator.Play(jumpUpAnimationName);
            jumpState = JumpState.Prepare;

            yield return new WaitForSeconds(actorSetting.readyJumpTime);

            Vector3 startPos = transform.position;
            float hoveredDuration = 0.1f; // fixed time for now
            float ascendDuration = duration - hoveredDuration;

            float ascendTimer = 0f;
            while (ascendTimer < ascendDuration)
            {
                ascendTimer += Time.fixedDeltaTime;
                float t = Mathf.Clamp01(ascendTimer / ascendDuration);

                float smoothT = 1f - Mathf.Pow(1f - t, 2f);
                float currentHeight = addY * smoothT;

                Vector3 newPos = new Vector3(transform.position.x, startPos.y + currentHeight, transform.position.z);
                SetPosition(newPos);

                if (isPressingLeft)
                {
                    AddPosition(GetCurrentMoveSpeed(false) * Time.fixedDeltaTime * Vector3.left);
                }

                if (isPressingRight)
                {
                    AddPosition(GetCurrentMoveSpeed(true) * Time.fixedDeltaTime * Vector3.right);
                }

                yield return new WaitForFixedUpdate();
            }

            yield return new WaitForSeconds(hoveredDuration);

            jumpState = JumpState.Hover; // update will detect falling
        }

        private IEnumerator IESimpleFall()
        {
            jumpState = JumpState.Falling;
            float fallSpeed = 0f;

            while (!IsGrounded)
            {
                fallSpeed = Mathf.Clamp(fallSpeed + actorSetting.fallSpeed * Time.fixedDeltaTime, 0f, actorSetting.fallSpeed);

                AddPosition(fallSpeed * Time.fixedDeltaTime * Vector3.down);

                if (isPressingLeft)
                {
                    AddPosition(GetCurrentMoveSpeed(false) * Time.fixedDeltaTime * Vector3.left);
                }

                if (isPressingRight)
                {
                    AddPosition(GetCurrentMoveSpeed(true) * Time.fixedDeltaTime * Vector3.right);
                }

                if (state != State.SimpleJumping && state != State.Normal)
                {
                    yield break;
                }

                yield return new WaitForFixedUpdate();
            }

            SetToIdle(true);
        }

        private IEnumerator IEJumpTo(float targetX, float height, float duration)
        {
            DisableIsPreparingAttack();
            animator.Play(jumpUpAnimationName);

            yield return new WaitForSeconds(actorSetting.readyJumpTime);

            if (state != State.JumpingToTargetX)
            {
                yield break;
            }

            animator.Play(jumpingAnimationName);

            float timer = duration;

            if (targetX > BoardSetter.MAX_X)
            {
                targetX = BoardSetter.MAX_X;
            }
            else if (targetX < BoardSetter.MIN_X)
            {
                targetX = BoardSetter.MIN_X;
            }

            transform.DOJump(new Vector3(targetX, transform.position.y, 0f), height, 1, duration).SetEase(Ease.Linear);

            while (timer > 0)
            {
                timer -= Time.fixedDeltaTime;
                if (state != State.JumpingToTargetX)
                {
                    yield break;
                }

                yield return new WaitForFixedUpdate();
            }

            state = State.Normal;
            SetToIdle();

            EventBus.Publish(new Actor_OnJumpEnded() { instanceID = GetInstanceID() });
        }

        public void BeThrown(JumpInfo jumpInfo, bool recoverWhenGetUp)
        {
            state = State.Flying;
            StartCoroutine(IEBeThrown(jumpInfo.targetX, jumpInfo.height, jumpInfo.duration, recoverWhenGetUp));
        }

        private IEnumerator IEBeThrown(float targetX, float height, float duration, bool recoverWhenGetUp)
        {
            DisableIsPreparingAttack();

            animator.Play(flyAnimationName);

            if (targetX > BoardSetter.MAX_X)
            {
                targetX = BoardSetter.MAX_X;
            }
            else if (targetX < BoardSetter.MIN_X)
            {
                targetX = BoardSetter.MIN_X;
            }

            transform.DOJump(new Vector3(targetX, transform.position.y, 0f), height, 1, duration).SetEase(Ease.Linear);

            yield return new WaitForSeconds(duration);

            animator.Play(downAnimationName);

            yield return new WaitForSeconds(actorSetting.downDuration);

            animator.Play(weaponCapability.GetIdleAnimationName());
            state = State.Normal;

            if (recoverWhenGetUp)
            {
                RecoverFullHealth(actorSetting.downDuration);
                yield return new WaitForSeconds(actorSetting.downDuration);
            }

            EventBus.Publish(new Actor_OnStunEnded() { instanceID = GetInstanceID() });
        }
    }
}
