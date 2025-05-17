using UnityEngine;
using DG.Tweening;
using KahaGameCore.Common;
using KahaGameCore.Package.SideScrollerActor.WeaponScripts;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay
{
    public partial class Actor
    {
        [Space(10)]
        [Header("動畫名稱設置")]
        [Header("部分動畫設置在武器身上")]
        [Space(10)]
        [Tooltip("與面向同方向衝刺時播放")]
        [Rename("向前衝刺(與面向同方向衝刺)")][SerializeField] private string dashAnimationName_front = "Dash";
        [Tooltip("與面向反方向衝刺時播放")]
        [Rename("向後衝刺(與面向反方向衝刺)")][SerializeField] private string dashAnimationName_back = "Dash";
        [Tooltip("由受到的攻擊決定受傷時間長短，不是動畫長度。")]
        [Rename("受傷動畫")][SerializeField] private string hurtAnimationName = "Hurt";
        [Tooltip("死亡後會播放這段動畫。")]
        [Rename("死亡動畫")][SerializeField] private string dieAnimationName = "Die";
        [Tooltip("跳躍時會先播放這段，在準備跳躍時間結束之後接入跳躍中動畫，不是自動抓取準備跳躍動畫長度，因此須注意準備跳躍時間與準備跳躍動畫時間長度。")]
        [Rename("準備跳躍動畫")][SerializeField] private string jumpUpAnimationName = "JumpUp";
        [Rename("跳躍中動畫")][SerializeField] private string jumpingAnimationName = "Jumping";
        [Tooltip("受到重攻擊後角色會被擊飛，被擊飛時會播放這段動畫。")]
        [Rename("擊飛動畫")][SerializeField] private string flyAnimationName = "Fly";
        [Tooltip("被擊飛後，倒地時會播放這段動畫。")]
        [Rename("倒地動畫")][SerializeField] private string downAnimationName = "Down";
        [Tooltip("目前設定是按下防禦鍵後會持續一段防禦時間，不是自動抓取防禦動畫長度，因此須注意防禦持續時間與防禦動畫時間長度。")]
        [Rename("防禦動畫")][SerializeField] private string defenseAnimationName = "Defense";

        public void SetAlpha(float alpha)
        {
            DOTween.To(GetSpriteAlpha, SetSpriteAlpha, alpha, 1f).SetEase(Ease.Linear);
        }

        private float GetSpriteAlpha()
        {
            return spriteRenderer.color.a;
        }

        private void SetSpriteAlpha(float alpha)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }

        public void SetFacingRight(bool force = false)
        {
            if (force)
            {
                animator.transform.eulerAngles = new Vector3(0f, 0f, 0f);
                dashEffect_back.flip = dashEffect_front.flip = new Vector3(0f, 0f);
                return;
            }

            if (state == State.Dead)
            {
                return;
            }

            SetFlipX(true);
        }

        public void SetFacingLeft(bool force = false)
        {
            if (force)
            {
                animator.transform.eulerAngles = new Vector3(0f, 180f, 0f);
                dashEffect_back.flip = dashEffect_front.flip = new Vector3(1f, 0f);
                return;
            }

            if (state == State.Dead)
            {
                return;
            }

            SetFlipX(false);
        }

        public void SetFlipX(bool isRight)
        {
            if (state == State.Dead || state == State.SimplePauseMove || state == State.WaitInitialize)
            {
                return;
            }

            LockLookDirection lockLookDirectionTemp = state == State.Running ? LockLookDirection.None : lockLookDirection;

            switch (lockLookDirectionTemp)
            {
                case LockLookDirection.Left:
                    animator.transform.eulerAngles = new Vector3(0f, 180f, 0f);
                    if (dashEffect_back != null) dashEffect_back.flip = new Vector3(1f, 0f);
                    if (dashEffect_front != null) dashEffect_front.flip = new Vector3(1f, 0f);
                    break;
                case LockLookDirection.Right:
                    animator.transform.eulerAngles = new Vector3(0f, 0f, 0f);
                    if (dashEffect_back != null) dashEffect_back.flip = new Vector3(0f, 0f);
                    if (dashEffect_front != null) dashEffect_front.flip = new Vector3(0f, 0f);
                    break;
                case LockLookDirection.None:
                    animator.transform.eulerAngles = new Vector3(0f, isRight ? 0f : 180f, 0f);
                    if (dashEffect_back != null) dashEffect_back.flip = new Vector3(isRight ? 0f : 1f, 0f);
                    if (dashEffect_front != null) dashEffect_front.flip = new Vector3(isRight ? 0f : 1f, 0f);
                    break;
                default:
                    break;
            }
        }

        private void PlayMoveAnimation(bool isRight)
        {
            if (isHoldingPrepareAttackButton && !isPreparingAttack)
            {
                StartPrepareAttack();
            }

            if (isPreparingAttack)
            {
                IAttackInfo nextAttackInfo = weaponCapability.PeekNextAttackInfo();

                if (nextAttackInfo == null || string.IsNullOrEmpty(nextAttackInfo.GetPrepareAndMoveAnimationName()))
                {
                    animator.Play(nextAttackInfo?.GetPrepareAnimationName());
                }
                else
                {
                    if (isRight == IsFacingRight)
                    {
                        animator.Play(nextAttackInfo.GetPrepareAndMoveAnimationName(false));
                    }
                    else
                    {
                        animator.Play(nextAttackInfo.GetPrepareAndMoveAnimationName(true));
                    }
                }
            }
            else
            {
                if (isRight == IsFacingRight)
                {
                    animator.Play(weaponCapability.GetWalkAnimationName(false));
                }
                else
                {
                    animator.Play(weaponCapability.GetWalkAnimationName(true));
                }
            }
        }

        private void TickLockDirection(bool forceUpdate = false)
        {
            switch (lockDirectionType)
            {
                case LockDirectionType.None:
                    break;
                case LockDirectionType.LookAtOpponent:
                    TickLockDirection_LookAtOpponent(forceUpdate);
                    break;
                case LockDirectionType.LookAtMouse:
                    TickLockDirection_LookAtMouse(forceUpdate);
                    break;
            }
        }

        private void TickLockDirection_LookAtOpponent(bool forceUpdate = false)
        {
            Actor target = camp == Camp.Monster ? ActorContainer.GetActorByCamp(Camp.Hero) : ActorContainer.GetActorByCamp(Camp.Monster);
            if (target != null)
            {
                if (target.transform.position.x > transform.position.x)
                {
                    lockLookDirection = LockLookDirection.Right;
                }
                else
                {
                    lockLookDirection = LockLookDirection.Left;
                }
            }
            else
            {
                lockLookDirection = LockLookDirection.None;
            }

            if (lastLockLookDirection != lockLookDirection || forceUpdate)
            {
                lastLockLookDirection = lockLookDirection;
                if (target != null)
                {
                    SetFlipX(target.transform.position.x > transform.position.x);
                }
            }
        }

        private void TickLockDirection_LookAtMouse(bool forceUpdate = false)
        {
            if (mainCamera == null)
            {
                mainCamera = UnityEngine.Camera.main;
            }

            if (mainCamera == null)
            {
                return;
            }

            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            if (mousePos.x > transform.position.x)
            {
                lockLookDirection = LockLookDirection.Right;
            }
            else
            {
                lockLookDirection = LockLookDirection.Left;
            }

            if (lastLockLookDirection != lockLookDirection || forceUpdate)
            {
                lastLockLookDirection = lockLookDirection;
                SetFlipX(mousePos.x > transform.position.x);
            }
        }
    }
}
