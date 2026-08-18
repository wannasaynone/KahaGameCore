using System;
using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.GameEvents.Editor
{
    /// <summary>
    /// 讓獨立的 Game Events Editor 讀寫專案自訂資料 Catalog，
    /// 而不反向依賴具體的 GameFlow 組裝。
    /// </summary>
    public sealed class GameEventEditorDataSource
    {
        private readonly Func<UnityEngine.Object, GameEventCatalogAsset> getEventCatalog;
        private readonly Action<UnityEngine.Object, GameEventCatalogAsset> setEventCatalog;
        private readonly Func<UnityEngine.Object, IReadOnlyList<TextAsset>> getParameterTables;
        private readonly Action<UnityEngine.Object, IReadOnlyList<TextAsset>> setParameterTables;
        private readonly Func<UnityEngine.Object, IReadOnlyList<string>> getCommandNames;
        private readonly Action<UnityEngine.Object, IReadOnlyList<string>> setCommandNames;
        private readonly Func<UnityEngine.Object, IReadOnlyList<string>> getTriggerTimings;

        public GameEventEditorDataSource(
            string displayName,
            Type assetType,
            Func<UnityEngine.Object, GameEventCatalogAsset> getEventCatalog,
            Action<UnityEngine.Object, GameEventCatalogAsset> setEventCatalog,
            Func<UnityEngine.Object, IReadOnlyList<TextAsset>> getParameterTables,
            Action<UnityEngine.Object, IReadOnlyList<TextAsset>> setParameterTables,
            Func<UnityEngine.Object, IReadOnlyList<string>> getCommandNames,
            Action<UnityEngine.Object, IReadOnlyList<string>> setCommandNames,
            Func<UnityEngine.Object, IReadOnlyList<string>> getTriggerTimings)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Display name is required.", nameof(displayName));
            if (assetType == null) throw new ArgumentNullException(nameof(assetType));
            if (!typeof(ScriptableObject).IsAssignableFrom(assetType))
                throw new ArgumentException("Data source asset type must be a ScriptableObject.", nameof(assetType));

            DisplayName = displayName;
            AssetType = assetType;
            this.getEventCatalog = getEventCatalog ?? throw new ArgumentNullException(nameof(getEventCatalog));
            this.setEventCatalog = setEventCatalog ?? throw new ArgumentNullException(nameof(setEventCatalog));
            this.getParameterTables = getParameterTables ?? throw new ArgumentNullException(nameof(getParameterTables));
            this.setParameterTables = setParameterTables ?? throw new ArgumentNullException(nameof(setParameterTables));
            this.getCommandNames = getCommandNames ?? throw new ArgumentNullException(nameof(getCommandNames));
            this.setCommandNames = setCommandNames ?? throw new ArgumentNullException(nameof(setCommandNames));
            this.getTriggerTimings = getTriggerTimings ?? throw new ArgumentNullException(nameof(getTriggerTimings));
        }

        public string DisplayName { get; }
        public Type AssetType { get; }

        public bool IsValidAsset(UnityEngine.Object asset)
        {
            return asset != null && AssetType.IsInstanceOfType(asset);
        }

        public GameEventCatalogAsset GetEventCatalog(UnityEngine.Object asset)
        {
            return getEventCatalog(RequireAsset(asset));
        }

        public void SetEventCatalog(UnityEngine.Object asset, GameEventCatalogAsset catalog)
        {
            setEventCatalog(RequireAsset(asset), catalog);
        }

        public IReadOnlyList<TextAsset> GetParameterTables(UnityEngine.Object asset)
        {
            return getParameterTables(RequireAsset(asset)) ?? Array.Empty<TextAsset>();
        }

        public void SetParameterTables(
            UnityEngine.Object asset,
            IReadOnlyList<TextAsset> tables)
        {
            setParameterTables(RequireAsset(asset), tables ?? throw new ArgumentNullException(nameof(tables)));
        }

        public IReadOnlyList<string> GetCommandNames(UnityEngine.Object asset)
        {
            return getCommandNames(RequireAsset(asset)) ?? Array.Empty<string>();
        }

        public void SetCommandNames(
            UnityEngine.Object asset,
            IReadOnlyList<string> commandNames)
        {
            setCommandNames(
                RequireAsset(asset),
                commandNames ?? throw new ArgumentNullException(nameof(commandNames)));
        }

        public IReadOnlyList<string> GetTriggerTimings(UnityEngine.Object asset)
        {
            return getTriggerTimings(RequireAsset(asset)) ?? Array.Empty<string>();
        }

        private UnityEngine.Object RequireAsset(UnityEngine.Object asset)
        {
            if (!IsValidAsset(asset))
            {
                throw new ArgumentException(
                    $"Data source must be a {AssetType.Name}.",
                    nameof(asset));
            }

            return asset;
        }
    }
}
