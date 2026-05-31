using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.ActorSystem
{
    [CreateAssetMenu(fileName = "NewFactionCollisionTable", menuName = "Kaha Game Core/Faction Collision Table")]
    public class FactionCollisionTable : ScriptableObject
    {
        [SerializeField] private List<FactionCollisionRule> rules = new();
        [SerializeField] private FactionCollisionResult defaultResult = FactionCollisionResult.Skip;
        [SerializeField] private GameObject defaultExplosionPrefab;

        public FactionCollisionResult DefaultResult => defaultResult;

        public bool TryGetRule(string bulletFaction, string targetFaction, out FactionCollisionRule rule)
        {
            for (int i = 0; i < rules.Count; i++)
            {
                if (rules[i].bulletFaction == bulletFaction && rules[i].targetFaction == targetFaction)
                {
                    rule = rules[i];
                    return true;
                }
            }

            rule = null;
            return false;
        }

        public FactionCollisionResult GetCollisionResult(string bulletFaction, string targetFaction)
        {
            if (TryGetRule(bulletFaction, targetFaction, out var rule))
            {
                return rule.result;
            }
            return defaultResult;
        }

        public GameObject GetExplosionPrefab(string bulletFaction, string targetFaction)
        {
            if (TryGetRule(bulletFaction, targetFaction, out var rule))
            {
                return rule.explosionPrefab;
            }
            return defaultExplosionPrefab;
        }
    }
}
