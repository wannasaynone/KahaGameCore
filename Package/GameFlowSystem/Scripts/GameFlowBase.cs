using System;

namespace KahaGameCore.Package.GameFlowSystem
{
    public abstract class GameFlowBase
    {
        public abstract void Start();
        public abstract void Update();
        public abstract void FixedUpdate();
        public abstract void LateUpdate();
    }
}