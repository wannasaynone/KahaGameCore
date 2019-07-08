using System;
using KahaGameCore.Static;

namespace KahaGameCore.Interface
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

            if(OnStarted != null)
            {
                OnStarted();
            }

            if(!m_skipTicker)
            {
                m_ticker = StateTicker.GetUseableTicker();
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

