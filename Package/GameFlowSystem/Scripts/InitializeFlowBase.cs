using System;
using KahaGameCore.Processor;

namespace KahaGameCore.Package.GameFlowSystem
{
    public abstract class InitializeFlowBase : IProcessable
    {
        public abstract void Process(Action onComplete, Action onForceQuit);
    }
}