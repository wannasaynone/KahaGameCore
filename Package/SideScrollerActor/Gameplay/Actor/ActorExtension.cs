using System;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay.Extension
{
    public abstract class ActorExtension : MonoBehaviour
    {
        public string Key => key;
        [SerializeField] private string key;

        public abstract void Process(object data, Action onEnded);
        public abstract void ForceEnd();
    }
}