using System;
using System.IO;
using System.Linq;
using KahaGameCore.Parameters.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.Parameters.Tests
{
    public class ParameterTableEditorWindowTests
    {
        [Test]
        public void NewTable_CreatesValidEmptyTable()
        {
            ParameterTableEditorWindow window =
                ScriptableObject.CreateInstance<ParameterTableEditorWindow>();

            try
            {
                window.NewTable();

                ParameterTable table = window.ValidateTable();

                Assert.That(Guid.TryParse(table.TableGuid, out _), Is.True);
                Assert.That(table.DisplayName, Is.EqualTo("New Parameter Table"));
                Assert.That(table.Definitions, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void AddAndRemoveParameters_ProducesTypedTableRows()
        {
            ParameterTableEditorWindow window =
                ScriptableObject.CreateInstance<ParameterTableEditorWindow>();

            try
            {
                window.SetTableDisplayName("Gameplay");
                window.AddInt("Supplies", "物資", 60, 0, 9999);
                window.AddFloat("Speed", "速度", 1.5f, 0f, 2.5f);
                window.AddBool("OutingUnlocked", "外出解鎖", false);
                window.AddString("PlayerName", "玩家名稱", "Mia");
                window.RemoveParameterAt(1);

                ParameterTable table = window.ValidateTable();

                Assert.That(table.Definitions.Select(x => x.Key),
                    Is.EqualTo(new[] { "Supplies", "OutingUnlocked", "PlayerName" }));
                Assert.That(table.Definitions[0].InitialValue.AsInt(), Is.EqualTo(60));
                Assert.That(table.Definitions[1].InitialValue.AsBool(), Is.False);
                Assert.That(table.Definitions[2].InitialValue.AsString(), Is.EqualTo("Mia"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void SaveAndLoadTable_RoundTripsMultipleRowsInOneAsset()
        {
            const string assetPath =
                "Assets/KahaGameCore/Modules/Parameters/Tests/Editor/TempGameplay.parameters.json";
            ParameterTableEditorWindow writer =
                ScriptableObject.CreateInstance<ParameterTableEditorWindow>();
            ParameterTableEditorWindow reader =
                ScriptableObject.CreateInstance<ParameterTableEditorWindow>();

            try
            {
                writer.SetTableDisplayName("Gameplay");
                writer.AddInt("Day", "天數", 1, 1, 999);
                writer.AddBool("OutingUnlocked", "外出解鎖", false);
                writer.SaveTable(assetPath);

                reader.LoadTable(assetPath);
                ParameterTable table = reader.ValidateTable();

                Assert.That(table.DisplayName, Is.EqualTo("Gameplay"));
                Assert.That(table.Definitions, Has.Count.EqualTo(2));
                Assert.That(table.Definitions.Select(x => x.Key),
                    Is.EqualTo(new[] { "Day", "OutingUnlocked" }));
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
                UnityEngine.Object.DestroyImmediate(writer);
                UnityEngine.Object.DestroyImmediate(reader);
            }
        }

        [Test]
        public void ReusablePanel_TracksChangesAndBecomesCleanAfterSave()
        {
            const string assetPath =
                "Assets/KahaGameCore/Modules/Parameters/Tests/Editor/TempPanel.parameters.json";
            ParameterTableEditorPanel panel = new ParameterTableEditorPanel();

            try
            {
                panel.InitializeIfNeeded();
                Assert.That(panel.IsDirty, Is.False);

                panel.SetTableDisplayName("Embedded Gameplay");
                panel.AddBool("DoorOpen", "門已開啟", false);
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
            ParameterTableEditorWindow window =
                ScriptableObject.CreateInstance<ParameterTableEditorWindow>();

            try
            {
                window.AddInt("Supplies", "物資", 1, 0, 9);
                window.AddInt("Supplies", "另一個物資", 2, 0, 9);

                Assert.Throws<InvalidParameterTableException>(() => window.SaveTable(assetPath));
                Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath), Is.Null);
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void SaveTable_RejectsPathThatEscapesAssets()
        {
            const string escapedAssetPath = "Assets/../TempEscaped.parameters.json";
            string escapedFullPath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "TempEscaped.parameters.json"));
            ParameterTableEditorWindow window =
                ScriptableObject.CreateInstance<ParameterTableEditorWindow>();

            try
            {
                Assert.Throws<ArgumentException>(() => window.SaveTable(escapedAssetPath));
                Assert.That(File.Exists(escapedFullPath), Is.False);
            }
            finally
            {
                if (File.Exists(escapedFullPath))
                {
                    File.Delete(escapedFullPath);
                }

                UnityEngine.Object.DestroyImmediate(window);
            }
        }
    }
}
