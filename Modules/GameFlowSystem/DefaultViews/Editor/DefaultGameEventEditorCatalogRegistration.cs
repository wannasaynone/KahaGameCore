using System;
using System.Collections.Generic;
using KahaGameCore.GameEvents.Editor;
using KahaGameCore.GameFlowSystem;
using KahaGameCore.GameFlowSystem.DefaultImplements;
using KahaGameCore.GameFlowSystem.DefaultImplements.Data;
using KahaGameCore.GameFlowSystem.DefaultImplements.DataAccess;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.GameFlowSystem.DefaultViews.Editor
{
    [InitializeOnLoad]
    internal static class DefaultGameEventEditorCatalogRegistration
    {
        static DefaultGameEventEditorCatalogRegistration()
        {
            GameEventEditorCommandCatalog.Register(
                () => EffectCommandRegistrar.Descriptors,
                new GameEventEditorDataSource(
                    "GameFlow Data Catalog",
                    typeof(GameFlowDataCatalogAsset),
                    asset => GetCatalog(asset).GameEventCatalog,
                    (asset, eventCatalog) =>
                    {
                        GameFlowDataCatalogAsset catalog = GetCatalog(asset);
                        catalog.SetGameEventCatalog(eventCatalog);
                        SaveCatalog(catalog);
                    },
                    asset => GetCatalog(asset).ParameterTables,
                    (asset, parameterTables) =>
                    {
                        GameFlowDataCatalogAsset catalog = GetCatalog(asset);
                        catalog.SetParameterTables(parameterTables);
                        SaveCatalog(catalog);
                    },
                    asset => GetCatalog(asset).GameEventCommands,
                    (asset, commandNames) =>
                    {
                        GameFlowDataCatalogAsset catalog = GetCatalog(asset);
                        catalog.SetGameEventCommands(commandNames);
                        SaveCatalog(catalog);
                    },
                    BuildTriggerTimings));
        }

        private static IReadOnlyList<string> BuildTriggerTimings(UnityEngine.Object sourceAsset)
        {
            SortedSet<string> timings = new SortedSet<string>(StringComparer.Ordinal)
            {
                GameFlowTimings.GameStart,
                GameFlowTimings.PhaseStart,
                GameFlowTimings.AfterAction
            };

            GameFlowDataCatalogAsset catalog = GetCatalog(sourceAsset);
            if (catalog.TimePhaseData != null)
            {
                TimePhaseData[] phases = new TextAssetJsonStaticDataHandler(
                    catalog.TimePhaseData).Load<TimePhaseData>();
                for (int index = 0; index < phases.Length; index++)
                {
                    if (!string.IsNullOrWhiteSpace(phases[index].Key))
                    {
                        timings.Add(GameFlowTimings.PhaseStartFor(phases[index].Key));
                    }
                }
            }

            if (catalog.LocationData != null)
            {
                LocationData[] locations = new TextAssetJsonStaticDataHandler(
                    catalog.LocationData).Load<LocationData>();
                for (int index = 0; index < locations.Length; index++)
                {
                    timings.Add(GameFlowTimings.EnterLocation(locations[index].ID));
                }
            }

            return new List<string>(timings).AsReadOnly();
        }

        private static GameFlowDataCatalogAsset GetCatalog(UnityEngine.Object asset)
        {
            return (GameFlowDataCatalogAsset)asset;
        }

        private static void SaveCatalog(GameFlowDataCatalogAsset catalog)
        {
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
        }
    }
}
