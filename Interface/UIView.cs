namespace KahaGameCore.Interface
{
    public abstract class UIView : View
    {
        public abstract bool IsShowing { get; }
        public abstract void ForceShow(Manager manager, bool show);
        public abstract void Show(Manager manager, bool show, System.Action onCompleted);
    }
}
