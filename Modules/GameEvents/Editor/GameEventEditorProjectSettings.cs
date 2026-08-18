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
        [SerializeField] private string dataCatalogGuid;

        public UnityEngine.Object LoadDataCatalog()
        {
            GameEventEditorDataSource source =
                GameEventEditorCommandCatalog.GetDataSource();
            if (source == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(dataCatalogGuid))
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(dataCatalogGuid);
            UnityEngine.Object catalog = string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath(path, source.AssetType);
            if (catalog != null)
            {
                return catalog;
            }

            dataCatalogGuid = string.Empty;
            Save(true);
            return null;
        }

        public void SetDataCatalog(UnityEngine.Object catalog)
        {
            string newGuid = catalog == null
                ? string.Empty
                : GetValidatedGuid(catalog);
            if (string.Equals(dataCatalogGuid, newGuid, StringComparison.Ordinal))
            {
                return;
            }

            dataCatalogGuid = newGuid;
            Save(true);
        }

        private static string GetValidatedGuid(UnityEngine.Object catalog)
        {
            GameEventEditorDataSource source =
                GameEventEditorCommandCatalog.GetDataSource();
            if (source == null)
            {
                throw new InvalidOperationException(
                    "No Game Event Editor data source is registered.");
            }

            if (!source.IsValidAsset(catalog))
            {
                throw new ArgumentException(
                    $"Data Catalog must be a {source.AssetType.Name}.",
                    nameof(catalog));
            }

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
