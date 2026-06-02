using UnityEngine;

namespace KahaGameCore.ActorSystem
{
    public class BulletHitContext
    {
        public string BulletFaction { get; }
        public Vector3 HitPosition { get; }
        public AGameActor Shooter { get; }

        public BulletHitContext(string bulletFaction, Vector3 hitPosition, AGameActor shooter = null)
        {
            BulletFaction = bulletFaction;
            HitPosition = hitPosition;
            Shooter = shooter;
        }
    }
}
