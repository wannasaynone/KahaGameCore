using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay.Controller
{
    public abstract class ActorTickerBase : MonoBehaviour
    {
        [SerializeField] protected Actor controlTarget = null;

        public void Bind(Actor controlTarget)
        {
            this.controlTarget = controlTarget;
        }

        public bool IsControling(Actor actor)
        {
            return controlTarget == actor;
        }

        public abstract void Tick();
    }
}