using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.GameEvents
{
    [CreateAssetMenu(
        fileName = "GameEventCatalog",
        menuName = "Kaha Game Core/Game Events/Catalog")]
    public sealed class GameEventCatalogAsset : ScriptableObject
    {
        [SerializeField] private List<TextAsset> files = new List<TextAsset>();

        public IReadOnlyList<TextAsset> Files => files;
    }
}
