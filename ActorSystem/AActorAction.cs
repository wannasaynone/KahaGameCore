using System;
using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.ActorSystem
{
    public abstract class AActorAction : MonoBehaviour
    {
        private readonly List<ChannelBinding> _bindings = new();
        private bool _isInitialized;

        public IReadOnlyList<ChannelBinding> Bindings => _bindings;
        public bool IsActive { get; internal set; }
        public int ActivationOrder { get; internal set; }
        public event Action<AActorAction> Completed;

        public void Init()
        {
            if (_isInitialized) return;
            OnBind();
            _isInitialized = true;
        }

        protected abstract void OnBind();

        protected void BindChannel<TChannel>(TChannel channel, int priority, Action<IActor> handler)
            where TChannel : Enum
        {
            _bindings.Add(new ChannelBinding(Convert.ToInt32(channel), priority, handler));
        }

        public virtual void OnStart(IActor actor) { }
        public virtual void OnTick() { }
        public virtual void OnEnd(IActor actor) { }

        protected void Complete()
        {
            Completed?.Invoke(this);
        }
    }
}
