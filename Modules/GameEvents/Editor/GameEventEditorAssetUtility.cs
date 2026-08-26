using System.IO;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.GameEvents.Editor
{
    internal static class GameEventEditorAssetUtility
    {
        public static string GetCategory(TextAsset asset)
        {
            return asset == null
                ? string.Empty
                : GetCategoryFromAssetPath(AssetDatabase.GetAssetPath(asset));
        }

        internal static string GetCategoryFromAssetPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return string.Empty;
            }

            string normalized = assetPath.Replace('\\', '/').TrimEnd('/');
            string directory = Path.GetDirectoryName(normalized);
            return string.IsNullOrWhiteSpace(directory)
                ? string.Empty
                : Path.GetFileName(directory)?.Trim() ?? string.Empty;
        }
    }
}
