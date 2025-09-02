using UnityEngine;

namespace KahaGameCore.Package.ActorSystem.Definition
{
    public abstract class ControllerBase : MonoBehaviour
    {
        public Instance controlTarget;

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

        private void Start()
        {
            if (controlTarget != null)
            {
                ActorCollection.Instance.Bind(controlTarget, this);
            }
        }

        private void OnDestroy()
        {
            if (controlTarget != null)
            {
                ActorCollection.Instance.Unbind(controlTarget, this);
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

        protected abstract void OnTick();
    }
}