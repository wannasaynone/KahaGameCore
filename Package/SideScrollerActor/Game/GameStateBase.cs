using System;

namespace KahaGameCore.Package.SideScrollerActor.Game
{
    public abstract class GameStateBase
    {
        private Action onEnded;

        public void Start(Action onEnded)
        {
            this.onEnded = onEnded;
            OnStart();
        }

        protected void End()
        {
            OnEnd();
            onEnded?.Invoke();
        }

        protected abstract void OnStart();
        protected abstract void OnEnd();
    }
}