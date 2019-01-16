namespace KahaGameCore.View
{
    using Manager = Manager.Manager;
    public abstract class View : UnityEngine.MonoBehaviour
    {
        protected virtual void Awake()
        {
            Manager.RegisterView(this);
        }

        protected virtual void OnDestroy()
        {
            Manager.UnregisterView(this);
        }
    }
}

