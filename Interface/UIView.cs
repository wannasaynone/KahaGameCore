namespace KahaGameCore.Interface
{
    public abstract class UIView : View
    {
        public abstract bool IsEnabled { get; }
        public abstract void EnablePage(Manager manager, bool enable);
    }
}
