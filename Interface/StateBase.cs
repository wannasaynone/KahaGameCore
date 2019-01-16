
namespace KahaGameCore.Manager.State
{
    public abstract class StateBase : Manager
    {
        public StateBase nextState = null;
        public bool pause = false;

        private StateTicker m_ticker = null;

        public virtual void Start()
        {
            m_ticker = GameObjectPoolManager.GetUseableObject<StateTicker>("[State Ticker]");
            m_ticker.StartTick(this);
        }

        public virtual void Stop()
        {
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

            Do();
        }

        protected abstract void Do();
    }
}

