using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.ActorSystem
{
    public class BulletHitContext
    {
        public string BulletFaction { get; }
        public Vector3 HitPosition { get; }
        public AGameActor Shooter { get; }

        // 命中傷害原始值：發射端已含攻擊力加成（於生成時烘焙，避免結算時 shooter 已死亡/銷毀）。
        // 防禦端減免與倍率修飾由 Game 層結算（DamageCalculator），本層只運載數值。
        public int Damage { get; }

        private readonly HashSet<string> _effects;

        public BulletHitContext(string bulletFaction, Vector3 hitPosition,
            AGameActor shooter = null, IEnumerable<string> effects = null, int damage = 0)
        {
            BulletFaction = bulletFaction;
            HitPosition = hitPosition;
            Shooter = shooter;
            Damage = damage;
            _effects = effects != null ? new HashSet<string>(effects) : new HashSet<string>();
        }

        public bool HasEffect(string effectTag) => _effects.Contains(effectTag);

        // 命中結果回饋（發射端可讀）：此命中被免傷方（格擋 / 招架 / 護甲面向）消化時，
        // 由防守端結算管線標記。持續型彈體（如残剑）據此決定彈飛等後續；一般子彈可忽略。
        // OnHit 為同步呼叫，發射端在 OnHit 返回後讀取即可。
        public bool WasNegated { get; private set; }
        public void MarkNegated() => WasNegated = true;

        // 越過防禦姿態：帶此標記的命中（突刺 / 重攻擊）使 BlockAction 放行，傷害落到後續 HurtAction。
        // 字面字串對應 Game 層的 HitEffectTag.BypassBlock（KahaGameCore 不反向相依 Game 層）。
        public bool CanBypassBlock => HasEffect("BypassBlock");
    }
}
