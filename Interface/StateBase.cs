using System;

namespace KahaGameCore.Manager.State
{
    public abstract class StateBase : Manager
    {
        public event Action OnStarted = null;
        public event Action OnEnded = null;

        public bool pause = false;

        private StateTicker m_ticker = null;
        private bool m_skipTicker = false;

        public void Start()
        {
            m_skipTicker = false;

            OnStart();
            m_ticker = GameObjectPoolManager.GetUseableObject<StateTicker>("[State Ticker]");

            if(OnStarted != null)
            {
                OnStarted();
            }

            if(!m_skipTicker)
            {
                m_ticker.StartTick(this);
            }
        }

        public void Stop(StateBase nextState = null)
        {
            OnStop();

            if(m_ticker != null)
            {
                m_ticker.gameObject.SetActive(false);
            }
            else
            {
                m_skipTicker = true; // might be a Stop() in Start()
            }

            if (OnEnded != null)
            {
                OnEnded();
            }

            if (nextState != null)
            {
                nextState.Start();
            }
        }

        public void Tick()
        {
            if(pause)
            {
                return;
            }

            OnTick();
        }

        protected abstract void OnStart();
        protected abstract void OnTick();
        protected abstract void OnStop();
    }
}

