using UnityEngine;

namespace KahaGameCore.ActorSystem
{
    [System.Serializable]
    public class FactionCollisionRule
    {
        public string bulletFaction;
        public string targetFaction;
        public FactionCollisionResult result = FactionCollisionResult.Skip;
        public GameObject explosionPrefab;
    }
}
