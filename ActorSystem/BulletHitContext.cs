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

        // [B4DBG] 暫時診斷用，事後移除。
        public string DebugEffects() => string.Join(",", _effects);

        // 越過防禦姿態：帶此標記的命中（突刺 / 重攻擊）使 BlockAction 放行，傷害落到後續 HurtAction。
        // 字面字串對應 Game 層的 HitEffectTag.BypassBlock（KahaGameCore 不反向相依 Game 層）。
        public bool CanBypassBlock => HasEffect("BypassBlock");
    }
}
