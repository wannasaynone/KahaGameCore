using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.ActorSystem
{
    public class BulletHitContext
    {
        public string BulletFaction { get; }
        public Vector3 HitPosition { get; }
        public AGameActor Shooter { get; }

        private readonly HashSet<string> _effects;

        public BulletHitContext(string bulletFaction, Vector3 hitPosition,
            AGameActor shooter = null, IEnumerable<string> effects = null)
        {
            BulletFaction = bulletFaction;
            HitPosition = hitPosition;
            Shooter = shooter;
            _effects = effects != null ? new HashSet<string>(effects) : new HashSet<string>();
        }

        public bool HasEffect(string effectTag) => _effects.Contains(effectTag);
    }
}
