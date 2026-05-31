using UnityEngine;

namespace KahaGameCore.ActorSystem
{
    public class BulletHitContext
    {
        public string BulletFaction { get; }
        public Vector3 HitPosition { get; }

        public BulletHitContext(string bulletFaction, Vector3 hitPosition)
        {
            BulletFaction = bulletFaction;
            HitPosition = hitPosition;
        }
    }
}
