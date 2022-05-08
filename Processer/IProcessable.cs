namespace KahaGameCore.Processer
{
    public interface IProcessable
    {
        void Process(System.Action onCompleted, System.Action onForceQuit);
    }
}
