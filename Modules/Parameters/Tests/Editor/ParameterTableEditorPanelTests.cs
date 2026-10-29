using System;
using System.IO;
using System.Linq;
using KahaGameCore.Parameters.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.Parameters.Tests
{
    public class ParameterTableEditorPanelTests
    {
        private static ParameterTableEditorPanel CreatePanel()
        {
            ParameterTableEditorPanel panel = new ParameterTableEditorPanel();
            panel.InitializeIfNeeded();
            return panel;
        }

        [Test]
        public void NewTable_CreatesValidEmptyTable()
        {
            ParameterTableEditorPanel panel = CreatePanel();
            panel.NewTable();

            ParameterTable table = panel.ValidateTable();

            Assert.That(Guid.TryParse(table.TableGuid, out _), Is.True);
            Assert.That(table.DisplayName, Is.EqualTo("新參數表"));
            Assert.That(table.Definitions, Is.Empty);
        }

        [Test]
        public void AddAndRemoveParameters_ProducesTypedTableRows()
        {
            ParameterTableEditorPanel panel = CreatePanel();
            panel.SetTableDisplayName("Gameplay");
            panel.AddInt(key: "Supplies", displayName: "物資", initialValue: 60, minValue: 0, maxValue: 9999);
            panel.AddFloat(key: "Speed", displayName: "速度", initialValue: 1.5f, minValue: 0f, maxValue: 2.5f);
            panel.AddBool(key: "OutingUnlocked", displayName: "外出解鎖", initialValue: false);
            panel.AddString(key: "PlayerName", displayName: "玩家名稱", initialValue: "Mia");
            panel.RemoveParameterAt(1);

            ParameterTable table = panel.ValidateTable();

            Assert.That(table.Definitions.Select(x => x.Key),
                Is.EqualTo(new[] { "Supplies", "OutingUnlocked", "PlayerName" }));
            Assert.That(table.Definitions[0].InitialValue.AsInt(), Is.EqualTo(60));
            Assert.That(table.Definitions[1].InitialValue.AsBool(), Is.False);
            Assert.That(table.Definitions[2].InitialValue.AsString(), Is.EqualTo("Mia"));
        }

        [Test]
        public void SaveAndLoadTable_RoundTripsMultipleRowsInOneAsset()
        {
            const string assetPath =
                "Assets/KahaGameCore/Modules/Parameters/Tests/Editor/TempGameplay.parameters.json";
            ParameterTableEditorPanel writer = CreatePanel();
            ParameterTableEditorPanel reader = CreatePanel();

            try
            {
                writer.SetTableDisplayName("Gameplay");
                writer.AddInt(key: "Day", displayName: "天數", initialValue: 1, minValue: 1, maxValue: 999);
                writer.AddBool(key: "OutingUnlocked", displayName: "外出解鎖", initialValue: false);
                writer.SaveTable(assetPath);

                reader.LoadTable(assetPath);
                ParameterTable table = reader.ValidateTable();

                Assert.That(table.DisplayName, Is.EqualTo("Gameplay"));
                Assert.That(table.Definitions.Select(x => x.Key),
                    Is.EqualTo(new[] { "Day", "OutingUnlocked" }));
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        [Test]
        public void TracksChangesAndBecomesCleanAfterSave()
        {
            const string assetPath =
                "Assets/KahaGameCore/Modules/Parameters/Tests/Editor/TempPanel.parameters.json";
            ParameterTableEditorPanel panel = CreatePanel();

            try
            {
                Assert.That(panel.IsDirty, Is.False);

                panel.SetTableDisplayName("Embedded Gameplay");
                panel.AddBool(key: "DoorOpen", displayName: "門已開啟", initialValue: false);
                Assert.That(panel.IsDirty, Is.True);

                panel.SaveTable(assetPath);
                Assert.That(panel.IsDirty, Is.False);
                Assert.That(panel.AssetPath, Is.EqualTo(assetPath));

                panel.SetTableDisplayName("Changed");
                panel.Reload();
                Assert.That(panel.IsDirty, Is.False);
                Assert.That(panel.TableDisplayName, Is.EqualTo("Embedded Gameplay"));
                Assert.That(panel.ParameterCount, Is.EqualTo(1));
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        [Test]
        public void SaveTable_DuplicateKeysDoNotCreateAsset()
        {
            const string assetPath =
                "Assets/KahaGameCore/Modules/Parameters/Tests/Editor/TempInvalid.parameters.json";
            ParameterTableEditorPanel panel = CreatePanel();

            try
            {
                panel.AddInt(key: "Supplies", displayName: "物資", initialValue: 1, minValue: 0, maxValue: 9);
                panel.AddInt(key: "Supplies", displayName: "另一個物資", initialValue: 2, minValue: 0, maxValue: 9);

                Assert.Throws<InvalidParameterTableException>(() => panel.SaveTable(assetPath));
                Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath), Is.Null);
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        [Test]
        public void SaveTable_RejectsPathThatEscapesAssets()
        {
            const string escapedAssetPath = "Assets/../TempEscaped.parameters.json";
            string escapedFullPath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "TempEscaped.parameters.json"));
            ParameterTableEditorPanel panel = CreatePanel();

            try
            {
                Assert.Throws<ArgumentException>(() => panel.SaveTable(escapedAssetPath));
                Assert.That(File.Exists(escapedFullPath), Is.False);
            }
            finally
            {
                if (File.Exists(escapedFullPath))
                {
                    File.Delete(escapedFullPath);
                }
            }
        }
    }
}
