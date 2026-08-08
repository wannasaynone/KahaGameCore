namespace KahaGameCore.Effects.Processor
{
    public interface IProcessable
    {
        void Process(System.Action onCompleted, System.Action onForceQuit);
    }
}
