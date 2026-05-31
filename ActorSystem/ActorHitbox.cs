using UnityEngine;

namespace KahaGameCore.ActorSystem
{
    [RequireComponent(typeof(Collider2D))]
    public class ActorHitbox : MonoBehaviour
    {
        [SerializeField] private AGameActor actor;

        public AGameActor Actor => actor;

        public void OnHit(BulletHitContext context)
        {
            if (actor == null) return;
            actor.OnHitByBullet(context);
        }
    }
}
