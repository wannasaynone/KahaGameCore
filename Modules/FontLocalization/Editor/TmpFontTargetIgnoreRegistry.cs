using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace KahaGameCore.FontLocalization.Editor
{
    [Serializable]
    internal sealed class TmpFontTargetIgnoredEntry
    {
        public string StableId;
        public string AssetPath;
        public string HierarchyPath;

        public TmpFontTargetIgnoredEntry(
            string stableId,
            string assetPath,
            string hierarchyPath)
        {
            StableId = stableId;
            AssetPath = assetPath;
            HierarchyPath = hierarchyPath;
        }
    }

    internal interface ITmpFontTargetIgnoreStore
    {
        IReadOnlyList<TmpFontTargetIgnoredEntry> Entries { get; }
        bool Contains(string stableId);
        void Ignore(IEnumerable<TmpFontTargetScanResult> results);
        bool Remove(string stableId);
    }

    [FilePath(
        "ProjectSettings/TmpFontTargetScannerIgnoreRegistry.asset",
        FilePathAttribute.Location.ProjectFolder)]
    internal sealed class TmpFontTargetIgnoreRegistry
        : ScriptableSingleton<TmpFontTargetIgnoreRegistry>,
            ITmpFontTargetIgnoreStore
    {
        [UnityEngine.SerializeField]
        private List<TmpFontTargetIgnoredEntry> entries = new();

        public IReadOnlyList<TmpFontTargetIgnoredEntry> Entries => entries;

        public bool Contains(string stableId)
        {
            return entries.Any(entry =>
                string.Equals(entry.StableId, stableId, StringComparison.Ordinal));
        }

        public void Ignore(IEnumerable<TmpFontTargetScanResult> results)
        {
            var knownIds = new HashSet<string>(
                entries.Select(entry => entry.StableId),
                StringComparer.Ordinal);

            foreach (TmpFontTargetScanResult result in results)
            {
                if (knownIds.Add(result.StableId))
                {
                    entries.Add(new TmpFontTargetIgnoredEntry(
                        result.StableId,
                        result.AssetPath,
                        result.HierarchyPath));
                }
            }

            entries.Sort((left, right) =>
            {
                int pathComparison = string.Compare(
                    left.AssetPath,
                    right.AssetPath,
                    StringComparison.OrdinalIgnoreCase);
                return pathComparison != 0
                    ? pathComparison
                    : string.Compare(
                        left.HierarchyPath,
                        right.HierarchyPath,
                        StringComparison.OrdinalIgnoreCase);
            });
            Save(true);
        }

        public bool Remove(string stableId)
        {
            int removed = entries.RemoveAll(entry =>
                string.Equals(entry.StableId, stableId, StringComparison.Ordinal));
            if (removed == 0)
            {
                return false;
            }

            Save(true);
            return true;
        }
    }
}
