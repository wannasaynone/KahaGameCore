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
            if (actor == null)
            {
                UnityEngine.Debug.Log("[ActorHitbox] " + gameObject.name + " is getting hit but reference actor is null, will skip");
                return;
            }
            actor.OnHitByBullet(context);
        }
    }
}
