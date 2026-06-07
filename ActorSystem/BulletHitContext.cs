using UnityEngine;

namespace KahaGameCore.ActorSystem
{
    public class BulletHitContext
    {
        public string BulletFaction { get; }
        public Vector3 HitPosition { get; }
        public AGameActor Shooter { get; }
        public bool CanCauseUnbalance { get; }

        public BulletHitContext(string bulletFaction, Vector3 hitPosition, bool canCauseUnbalance, AGameActor shooter = null)
        {
            BulletFaction = bulletFaction;
            HitPosition = hitPosition;
            Shooter = shooter;
            CanCauseUnbalance = canCauseUnbalance;
        }
    }
}
