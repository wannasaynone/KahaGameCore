using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KahaGameCore.Effects;
using KahaGameCore.GameEvents.Editor;
using KahaGameCore.Parameters;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.GameEvents.Tests
{
    /// <summary>
    /// Events refer to parameters by Key as plain text, so a Key renamed or
    /// removed outside the editor's Rename leaves events pointing at nothing
    /// until Play. This catches it for events and tables edited by hand or
    /// merged in from git.
    /// </summary>
    public sealed class CatalogParameterReferenceTests
    {
        [Test]
        public void EveryCatalog_EventsOnlyReferenceExistingParameterKeys()
        {
            ParameterTableJsonCodec codec = new ParameterTableJsonCodec();
            List<string> problems = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(GameEventCatalogAsset)))
            {
                string catalogPath = AssetDatabase.GUIDToAssetPath(guid);
                GameEventCatalogAsset catalog =
                    AssetDatabase.LoadAssetAtPath<GameEventCatalogAsset>(catalogPath);

                HashSet<string> definedKeys = new HashSet<string>(
                    catalog.ParameterTables
                        .Where(asset => asset != null)
                        .SelectMany(asset => codec.Read(asset.text).Definitions)
                        .Select(definition => definition.Key),
                    StringComparer.Ordinal);

                IReadOnlyList<EffectCommandDescriptor> descriptors =
                    EffectCommandAssemblyCatalog.GetDescriptors(
                        catalog.CommandAssemblyNames,
                        warnings: null);
                GameEventParameterUsageIndex index =
                    GameEventParameterUsageIndex.Build(catalog, descriptors);

                problems.AddRange(index.Warnings.Select(warning => $"{catalogPath}: {warning}"));
                foreach (string key in index.ReferencedKeys
                             .Where(key => !definedKeys.Contains(key))
                             .OrderBy(key => key, StringComparer.Ordinal))
                {
                    string events = string.Join(
                        ", ",
                        index.Find(key).Select(reference => reference.AssetPath));
                    problems.Add($"{catalogPath}: '{key}' is not in any table, used by {events}");
                }
            }

            Assert.IsEmpty(
                problems,
                "Events must only reference parameter Keys defined in their catalog's tables:\n" +
                string.Join("\n", problems));
        }

        /// <summary>
        /// Formulas on scene and prefab components (e.g. ParameterStateBinder
        /// conditions) only fail when that object loads at Play. A scene can be
        /// launched with any catalog, so a Key only has to exist in some catalog.
        /// </summary>
        [Test]
        public void EveryScenesAndPrefab_FormulasOnlyReferenceExistingParameterKeys()
        {
            ParameterTableJsonCodec codec = new ParameterTableJsonCodec();
            HashSet<string> definedKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(GameEventCatalogAsset)))
            {
                GameEventCatalogAsset catalog = AssetDatabase.LoadAssetAtPath<GameEventCatalogAsset>(
                    AssetDatabase.GUIDToAssetPath(guid));
                definedKeys.UnionWith(catalog.ParameterTables
                    .Where(asset => asset != null)
                    .SelectMany(asset => codec.Read(asset.text).Definitions)
                    .Select(definition => definition.Key));
            }

            List<string> problems = new List<string>();
            foreach (string path in GameEventParameterRenamer.FindScenesAndPrefabs())
            {
                foreach (string key in GameEventParameterUsageIndex
                             .ExtractExpressionKeys(File.ReadAllText(path))
                             .Where(key => !definedKeys.Contains(key))
                             .OrderBy(key => key, StringComparer.Ordinal))
                {
                    problems.Add($"{path}: '${key}' is not in any catalog's tables");
                }
            }

            Assert.IsEmpty(
                problems,
                "Scene and prefab formulas must only reference existing parameter Keys:\n" +
                string.Join("\n", problems));
        }
    }
}
