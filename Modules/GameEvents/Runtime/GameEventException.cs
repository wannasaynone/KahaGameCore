using System;
using UnityEngine;

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

        /// <summary>
        /// The event file whose document failed, when known. A nested event
        /// (TriggerEvent) keeps the innermost file. Used as the Console context
        /// so clicking the log pings the event.
        /// </summary>
        public TextAsset EventFile { get; internal set; }
    }
}
