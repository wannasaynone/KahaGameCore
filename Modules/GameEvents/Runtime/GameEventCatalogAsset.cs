using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KahaGameCore.GameEvents
{
    [CreateAssetMenu(
        fileName = "GameEventCatalog",
        menuName = "Kaha Game Core/Game Events/Catalog")]
    public sealed class GameEventCatalogAsset : ScriptableObject
    {
        [SerializeField] private List<TextAsset> files = new List<TextAsset>();
        [SerializeField] private List<TextAsset> parameterTables = new List<TextAsset>();
        [SerializeField] private List<string> triggerTimings = new List<string>();
        [SerializeField] private List<string> commandAssemblyNames = new List<string>();
        [SerializeField] private List<string> enabledCommandNames = new List<string>();

        public IReadOnlyList<TextAsset> Files => files;
        public IReadOnlyList<TextAsset> ParameterTables => parameterTables;
        public IReadOnlyList<string> TriggerTimings => triggerTimings;
        public IReadOnlyList<string> CommandAssemblyNames => commandAssemblyNames;
        public IReadOnlyList<string> EnabledCommandNames => enabledCommandNames;

        public void SetParameterTables(IEnumerable<TextAsset> values)
        {
            parameterTables = DistinctAssets(values);
        }

        public void SetTriggerTimings(IEnumerable<string> values)
        {
            triggerTimings = DistinctStrings(values);
        }

        public void SetCommandAssemblyNames(IEnumerable<string> values)
        {
            commandAssemblyNames = DistinctStrings(values);
        }

        public void SetEnabledCommandNames(IEnumerable<string> values)
        {
            enabledCommandNames = DistinctStrings(values);
        }

        private void OnValidate()
        {
            files = DistinctAssets(files);
            parameterTables = DistinctAssets(parameterTables);
            triggerTimings = DistinctStrings(triggerTimings);
            commandAssemblyNames = DistinctStrings(commandAssemblyNames);
            enabledCommandNames = DistinctStrings(enabledCommandNames);
        }

        private static List<TextAsset> DistinctAssets(IEnumerable<TextAsset> values)
        {
            return (values ?? Enumerable.Empty<TextAsset>())
                .Where(value => value != null)
                .Distinct()
                .ToList();
        }

        private static List<string> DistinctStrings(IEnumerable<string> values)
        {
            return (values ?? Enumerable.Empty<string>())
                .Select(value => value?.Trim())
                .Where(value => !string.IsNullOrEmpty(value))
                .Distinct(System.StringComparer.Ordinal)
                .ToList();
        }
    }
}
