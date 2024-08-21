using UnityEngine;

namespace KahaGameCore.Package.GameActor
{
    public class Follower : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 offset;
        [SerializeField] private float stopDistance = 0.1f;
        [SerializeField] private float delta = 0.1f;

        private float zPosition;

        private void OnEnable()
        {
            zPosition = transform.position.z;
        }

        private void FixedUpdate()
        {
            if (target != null)
            {
                Vector3 targetPos = new Vector3(target.position.x + offset.x, target.position.y + offset.y, zPosition);
                if (Vector3.Distance(transform.position, targetPos) > stopDistance)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, delta);
                }
            }
        }
    }
}