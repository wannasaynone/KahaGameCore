using UnityEngine;

namespace KahaGameCore.ActorSystem
{
    public abstract class AGameActor : MonoBehaviour
    {
        private ActorController _controller;
        private bool _isInitialized;

        protected ActorController Controller => _controller;
        public Vector3 FacingDirection => _currentFacingDirection.normalized;
        private Vector3 _currentFacingDirection = Vector3.right;

        public void Initialize()
        {
            if (_isInitialized) return;
            _controller = new ActorController();
            OnInitialize(_controller);
            _isInitialized = true;
        }

        protected abstract void OnInitialize(ActorController controller);
        protected abstract void OnFaceDirectionChanged();
        public abstract void PlayAnimation(string animationName);
        public abstract string GetCurrentAnimation();

        public void SetFacingDirection(Vector3 direction)
        {
            _currentFacingDirection = direction.normalized;
            OnFaceDirectionChanged();
        }

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        public Vector3 GetPosition()
        {
            return transform.position;
        }

        public void Move(Vector3 delta)
        {
            transform.position += delta;
        }
    }
}
