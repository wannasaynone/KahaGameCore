
namespace KahaGameCore.Manager.State
{
    public abstract class StateBase : Manager
    {
        public bool pause = false;

        private StateTicker m_ticker = null;

        public void Start()
        {
            OnStart();
            m_ticker = GameObjectPoolManager.GetUseableObject<StateTicker>("[State Ticker]");
            m_ticker.StartTick(this);
        }

        public void Stop(StateBase nextState = null)
        {
            OnStop();
            m_ticker.gameObject.SetActive(false);
            if(nextState != null)
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

