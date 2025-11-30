using KahaGameCore.Common;
using UnityEngine;

namespace KahaGameCore.Package.ActorSystem.Definition
{
    public abstract class ControllerBase : MonoBehaviour
    {
        public Instance ControlTarget { get { return controlTarget; } }
        [OnlyForObserver][SerializeField] private Instance controlTarget;

        public void SetControlTarget(Instance target)
        {
            if (controlTarget != null)
            {
                Debug.LogError($"[{GetType().Name}] Changing control target from {controlTarget.name} to {target.name}, use RemoveControlTarget to remove previously controlled target before binding new target.");
                ActorCollection.Instance.Unbind(controlTarget, this);
            }

            controlTarget = target;
        }

        public void RemoveControlTarget()
        {
            controlTarget = null;
        }

        private int _lockCounter = 0;

        public void Lock()
        {
            _lockCounter++;
            Debug.Log($"[{GetType().Name}] locked. Counter: {_lockCounter}");
        }

        /// <summary>
        /// Unlocks the movement controller by decrementing the lock counter.
        /// Movement is only enabled when the counter reaches zero.
        /// </summary>
        public void Unlock()
        {
            if (_lockCounter > 0)
            {
                _lockCounter--;
                Debug.Log($"[{GetType().Name}] unlocked. Counter: {_lockCounter}");
            }
        }

        private void OnDestroy()
        {
            if (ControlTarget != null)
            {
                ActorCollection.Instance.Unbind(ControlTarget, this);
            }
        }

        /// <summary>
        /// Returns whether the movement is currently locked.
        /// </summary>
        /// <returns>True if movement is locked, false otherwise.</returns>
        public bool IsLocked()
        {
            return _lockCounter > 0;
        }

        private void Update()
        {
            if (IsLocked())
                return;

            OnTick();
        }

        protected abstract void OnEnable();
        protected abstract void OnTick();
        protected abstract void OnDisable();
    }
}