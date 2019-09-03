namespace KahaGameCore.Interface
{
    public interface IProcessable
    {
        void Process(System.Action onCompleted);
    }
}
