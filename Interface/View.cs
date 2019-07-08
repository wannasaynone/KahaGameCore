namespace KahaGameCore.Interface
{
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

