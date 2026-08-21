using System;
using System.IO;
using System.Linq;
using System.Reflection;
using KahaGameCore.GameEvents;
using KahaGameCore.Parameters;
using KahaGameCore.Parameters.Editor;
using KahaGameCore.Presentation.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.Presentation.Tests
{
    public sealed class ParameterStateBinderEditorTests
    {
        private const string TestFolder =
            "Assets/KahaGameCore/Modules/Presentation/Tests/Editor/__TempParameterStateBinderEditorTests";

        [TestCase(
            false,
            0,
            (int)ParameterStateBinderEditor.SetupState.MissingEventCatalog)]
        [TestCase(
            true,
            0,
            (int)ParameterStateBinderEditor.SetupState.MissingConditionParameters)]
        [TestCase(
            true,
            1,
            (int)ParameterStateBinderEditor.SetupState.Ready)]
        public void GetSetupState_ReturnsExpectedState(
            bool hasEventCatalog,
            int conditionParameterCount,
            int expectedValue)
        {
            ParameterStateBinderEditor.SetupState expected =
                (ParameterStateBinderEditor.SetupState)expectedValue;
            Assert.That(
                ParameterStateBinderEditor.GetSetupState(
                    hasEventCatalog,
                    conditionParameterCount),
                Is.EqualTo(expected));
        }

        [Test]
        public void SavingParameterTable_RefreshesOpenInspectorParameterOptions()
        {
            EnsureTestFolder();
            string tablePath = TestFolder + "/Binder.parameters.json";
            ParameterTable initialTable = new ParameterTable(
                Guid.NewGuid().ToString(),
                "Binder Parameters",
                new[]
                {
                    ParameterDefinition.Bool("InitiallyAvailable", "Initially Available", false)
                });
            File.WriteAllText(
                ToFullPath(tablePath),
                new ParameterTableJsonCodec().Write(initialTable));
            AssetDatabase.ImportAsset(
                tablePath,
                ImportAssetOptions.ForceSynchronousImport);

            TextAsset tableAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(tablePath);
            string catalogPath = TestFolder + "/GameEventCatalog.asset";
            GameEventCatalogAsset catalog =
                ScriptableObject.CreateInstance<GameEventCatalogAsset>();
            catalog.SetParameterTables(new[] { tableAsset });
            AssetDatabase.CreateAsset(catalog, catalogPath);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            object projectSettings = GetGameEventEditorProjectSettings();
            GameEventCatalogAsset originalCatalog =
                InvokeCatalogSettingsMethod<GameEventCatalogAsset>(
                    projectSettings,
                    "LoadEventCatalog");
            GameObject host = new GameObject("Parameter State Binder Test Host");
            ParameterStateBinder binder = host.AddComponent<ParameterStateBinder>();
            ParameterStateBinderEditor editor = null;

            try
            {
                InvokeCatalogSettingsMethod<object>(
                    projectSettings,
                    "SetEventCatalog",
                    catalog);
                editor = (ParameterStateBinderEditor)UnityEditor.Editor.CreateEditor(
                    binder,
                    typeof(ParameterStateBinderEditor));

                Assert.That(GetConditionParameterKeys(editor),
                    Is.EqualTo(new[] { "InitiallyAvailable" }));

                ParameterTableEditorPanel panel = new ParameterTableEditorPanel();
                panel.LoadTable(tablePath);
                panel.AddBool("AvailableAfterSave", "Available After Save", false);
                panel.SaveTable(tablePath);
                EnsureCatalogIsCurrent(editor);

                Assert.That(GetConditionParameterKeys(editor),
                    Does.Contain("AvailableAfterSave"));
            }
            finally
            {
                if (editor != null)
                {
                    UnityEngine.Object.DestroyImmediate(editor);
                }

                InvokeCatalogSettingsMethod<object>(
                    projectSettings,
                    "SetEventCatalog",
                    originalCatalog);
                UnityEngine.Object.DestroyImmediate(host);
                AssetDatabase.DeleteAsset(TestFolder);
            }
        }

        private static void EnsureCatalogIsCurrent(
            ParameterStateBinderEditor editor)
        {
            MethodInfo method = typeof(ParameterStateBinderEditor).GetMethod(
                "EnsureCatalogIsCurrent",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(editor, null);
        }

        private static string[] GetConditionParameterKeys(
            ParameterStateBinderEditor editor)
        {
            FieldInfo field = typeof(ParameterStateBinderEditor).GetField(
                "conditionParameters",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return ((Array)field.GetValue(editor))
                .Cast<object>()
                .Select(entry =>
                {
                    PropertyInfo definitionProperty = entry.GetType().GetProperty(
                        "Definition",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);
                    Assert.That(definitionProperty, Is.Not.Null);
                    return ((ParameterDefinition)definitionProperty.GetValue(entry)).Key;
                })
                .ToArray();
        }

        private static object GetGameEventEditorProjectSettings()
        {
            Type settingsType = Type.GetType(
                "KahaGameCore.GameEvents.Editor.GameEventEditorProjectSettings, " +
                "KahaGameCore.Modules.GameEvents.Editor");
            Assert.That(settingsType, Is.Not.Null);
            PropertyInfo instanceProperty = settingsType.GetProperty(
                "instance",
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.FlattenHierarchy);
            Assert.That(instanceProperty, Is.Not.Null);
            return instanceProperty.GetValue(null);
        }

        private static T InvokeCatalogSettingsMethod<T>(
            object settings,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method = settings.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null);
            return (T)method.Invoke(settings, arguments);
        }

        private static string ToFullPath(string assetPath)
        {
            return Path.Combine(
                Application.dataPath,
                assetPath.Substring("Assets/".Length));
        }

        private static void EnsureTestFolder()
        {
            string[] parts = TestFolder.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }
    }
}
