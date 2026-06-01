using UnityEngine;

namespace KahaGameCore.ActorSystem
{
    public class BulletHitContext
    {
        public string BulletFaction { get; }
        public Vector3 HitPosition { get; }
        public Vector3 SourcePosition { get; }

        public BulletHitContext(string bulletFaction, Vector3 hitPosition, Vector3 sourcePosition)
        {
            BulletFaction = bulletFaction;
            HitPosition = hitPosition;
            SourcePosition = sourcePosition;
        }
    }
}
