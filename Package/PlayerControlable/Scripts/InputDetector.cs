using UnityEngine;

namespace KahaGameCore.Package.PlayerControlable
{
    public abstract class InputDetector : ScriptableObject
    {
        public bool IsPressedUp { get; protected set; }
        public bool IsPressingUp { get; protected set; }
        public bool IsPressedDown { get; protected set; }
        public bool IsPressingDown { get; protected set; }
        public bool IsPressedLeft { get; protected set; }
        public bool IsPressingLeft { get; protected set; }
        public bool IsPressedRight { get; protected set; }
        public bool IsPressingRight { get; protected set; }
        public bool Selected { get; protected set; }

        public abstract void Tick();
        public abstract void Reset();
    }
}