using System.Collections.Generic;
using System.Linq;
using KahaGameCore.Parameters;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.GameEvents.Tests
{
    /// <summary>
    /// The launcher flattens every Parameter Table in a catalog into one store,
    /// so a Key repeated across tables only fails at Play. This catches it for
    /// tables edited by hand or merged in from git.
    /// </summary>
    public sealed class CatalogParameterKeyUniquenessTests
    {
        [Test]
        public void EveryCatalog_ParameterKeysAreUniqueAcrossTables()
        {
            ParameterTableJsonCodec codec = new ParameterTableJsonCodec();
            List<string> problems = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(GameEventCatalogAsset)))
            {
                string catalogPath = AssetDatabase.GUIDToAssetPath(guid);
                GameEventCatalogAsset catalog =
                    AssetDatabase.LoadAssetAtPath<GameEventCatalogAsset>(catalogPath);
                Dictionary<string, string> tableByKey = new Dictionary<string, string>();

                foreach (TextAsset tableAsset in catalog.ParameterTables.Where(asset => asset != null))
                {
                    string tablePath = AssetDatabase.GetAssetPath(tableAsset);
                    foreach (ParameterDefinition definition in codec.Read(tableAsset.text).Definitions)
                    {
                        if (tableByKey.TryGetValue(definition.Key, out string firstTablePath))
                        {
                            problems.Add(
                                $"{catalogPath}: '{definition.Key}' is in both {firstTablePath} and {tablePath}");
                        }
                        else
                        {
                            tableByKey.Add(definition.Key, tablePath);
                        }
                    }
                }
            }

            Assert.IsEmpty(
                problems,
                "Parameter Keys must be unique across all tables in a catalog:\n" +
                string.Join("\n", problems));
        }
    }
}
