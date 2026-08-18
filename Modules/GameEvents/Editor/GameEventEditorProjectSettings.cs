using System;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.GameEvents.Editor
{
    [FilePath(
        "ProjectSettings/KahaGameCore.GameEventEditorSettings.asset",
        FilePathAttribute.Location.ProjectFolder)]
    internal sealed class GameEventEditorProjectSettings :
        ScriptableSingleton<GameEventEditorProjectSettings>
    {
        [SerializeField] private string eventCatalogGuid;

        public GameEventCatalogAsset LoadEventCatalog()
        {
            if (string.IsNullOrEmpty(eventCatalogGuid))
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(eventCatalogGuid);
            GameEventCatalogAsset catalog = string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<GameEventCatalogAsset>(path);
            if (catalog != null)
            {
                return catalog;
            }

            eventCatalogGuid = string.Empty;
            Save(true);
            return null;
        }

        public void SetEventCatalog(GameEventCatalogAsset catalog)
        {
            string newGuid = catalog == null
                ? string.Empty
                : GetValidatedGuid(catalog);
            if (string.Equals(eventCatalogGuid, newGuid, StringComparison.Ordinal))
            {
                return;
            }

            eventCatalogGuid = newGuid;
            Save(true);
        }

        private static string GetValidatedGuid(GameEventCatalogAsset catalog)
        {
            string path = AssetDatabase.GetAssetPath(catalog);
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                throw new ArgumentException(
                    $"Cannot resolve an asset GUID for '{path}'.",
                    nameof(catalog));
            }

            return guid;
        }
    }
}
