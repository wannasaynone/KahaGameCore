using System;

namespace KahaGameCore.EffectCommand
{
    public abstract class EffectCommandBase
    {
        public EffectProcesser.ProcessData processData = null;
        public bool IsIfCommand { get; protected set; } = false;
        public abstract void Process(string[] vars, Action onCompleted, Action onForceQuit);
    }
}
