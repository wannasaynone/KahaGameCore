using System;

namespace KahaGameCore.GameEvents
{
    public sealed class GameEventException : Exception
    {
        public GameEventException(string code, string message)
            : base(message)
        {
            Code = code;
        }

        public GameEventException(string code, string message, Exception innerException)
            : base(message, innerException)
        {
            Code = code;
        }

        public string Code { get; }
    }
}
