using System;

namespace KahaGameCore.Combat.Processor.EffectProcessor
{
    public abstract class EffectCommandBase
    {
        public ProcessData processData = null;
        public bool IsIfCommand { get; protected set; } = false;
        public abstract void Process(string[] vars, Action onCompleted, Action onForceQuit);
    }
}
