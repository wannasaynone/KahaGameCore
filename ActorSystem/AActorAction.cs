using System;
using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.ActorSystem
{
    public abstract class AActorAction : MonoBehaviour
    {
        [SerializeField] private string _actionId = "";
        public string ActionId => _actionId;

        // 編輯期動畫下拉選單的來源：拖入此 Action 所用角色的 AnimatorController（如 PlayerAnimator.controller）。
        // 標了 [AnimationSelector] 的動畫名稱欄位會列出此 Controller 的全部 state 名稱。執行期不使用。
        [SerializeField] private RuntimeAnimatorController _animatorController;

        private readonly List<ChannelBinding> _bindings = new();
        public bool IsInitialized => _isInitialized;
        private bool _isInitialized;
        private bool _ownershipCheckReady;

        public IReadOnlyList<ChannelBinding> Bindings => _bindings;
        public bool IsActive { get; internal set; }
        public int ActivationOrder { get; internal set; }
        public ActorController OwningController { get; internal set; }


        public virtual bool IsInterruptible => false;
        public event Action<AActorAction> Completed;

        protected AGameActor CurrentReferenceActor => _currentReferenceActor;
        private AGameActor _currentReferenceActor;

        public void Init(AGameActor actor)
        {
            if (_isInitialized) return;
            _currentReferenceActor = actor;
            OnBind();
            _isInitialized = true;
        }

        internal void ResetChannelOwnershipCheck()
        {
            _ownershipCheckReady = false;
        }

        protected abstract void OnBind();

        protected void BindChannel<TChannel>(TChannel channel, int priority, Action<ActionContext> handler)
            where TChannel : Enum
        {
            _bindings.Add(new ChannelBinding(Convert.ToInt32(channel), priority, handler));
        }

        public const int AnimationChannelId = -1;

        protected void BindAnimationChannel(int priority, Func<string> animProvider)
        {
            AnimationChannelHandler handler = new AnimationChannelHandler(this, animProvider);
            _bindings.Add(new ChannelBinding(AnimationChannelId, priority, handler.Handle));
        }

        private sealed class AnimationChannelHandler
        {
            private readonly AActorAction _owner;
            private readonly Func<string> _provider;

            public AnimationChannelHandler(AActorAction owner, Func<string> provider)
            {
                _owner = owner;
                _provider = provider;
            }

            public void Handle(ActionContext context)
            {
                string anim = _provider();
                if (!string.IsNullOrEmpty(anim))
                    _owner._currentReferenceActor.PlayAnimation(anim);
            }
        }

        public void Active(ActionContext context)
        {
            if (!IsInitialized)
            {
                UnityEngine.Debug.LogError("Can't Active " + GetType().Name + ", make sure you active a action after Bind(actor).");
                return;
            }

            OnActivate(context);
        }
        public void Tick(ActionContext context)
        {
            if (!IsInitialized)
            {
                UnityEngine.Debug.LogError("Can't Tick " + GetType().Name + ", make sure you active a action after Bind(actor).");
                return;
            }

            OnTick(context);
        }
        public void Deactivate(ActionContext context)
        {
            if (!IsInitialized)
            {
                UnityEngine.Debug.LogError("Can't Deactivate " + GetType().Name + ", make sure you active a action after Bind(actor).");
                return;
            }

            OnDeactivate(context);
        }
        protected abstract void OnActivate(ActionContext context);
        protected abstract void OnTick(ActionContext context);
        protected abstract void OnDeactivate(ActionContext context);

        protected void Complete()
        {
            Completed?.Invoke(this);
        }

        // 自我啟動：只作用於 this（無實例參數），action 結構上無法啟動別人。
        // 自我結束沿用 Complete()（觸發 Completed → controller SetActionInactive）。
        protected void ActivateSelf(ActionContext context)
        {
            _currentReferenceActor.ActivateInstance(this, context);
        }
    }
}
