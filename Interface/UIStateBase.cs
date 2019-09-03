using System;

namespace KahaGameCore.Interface
{
    public abstract class UIStateBase : KahaGameCore.Interface.Manager
    {
        public event Action OnStarted = null;
        public event Action OnEnded = null;

        public bool pause = false;

        public void Start()
        {
            OnStart();

            if (OnStarted != null)
            {
                OnStarted();
            }
        }

        public void Stop(UIStateBase nextState = null)
        {
            OnStop();

            if (OnEnded != null)
            {
                OnEnded();
            }

            if (nextState != null)
            {
                nextState.Start();
            }
        }

        protected abstract void OnStart();
        protected abstract void OnStop();
    }
}

