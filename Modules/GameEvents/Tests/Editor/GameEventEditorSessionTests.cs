using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KahaGameCore.Effects;
using KahaGameCore.GameEvents.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.GameEvents.Tests
{
    public sealed class GameEventEditorSessionTests
    {
        private const string TestFolder =
            "Assets/KahaGameCore/Modules/GameEvents/Tests/Editor/TempGameEventEditor";

        [SetUp]
        public void SetUp()
        {
            EnsureTestFolder();
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TestFolder);
        }

        [Test]
        public void NewDocument_CreatesValidUnsavedDocument()
        {
            GameEventEditorSession session = new GameEventEditorSession();

            Assert.That(session.HasOpenFile, Is.False);
            session.NewDocument();
            GameEventDocument document = session.ValidateDocument();

            Assert.That(session.HasOpenFile, Is.False);
            Assert.That(document.SchemaVersion, Is.EqualTo(2));
            Assert.That(document.DocumentGuid, Is.Not.EqualTo(Guid.Empty));
            Assert.That(document.DisplayName, Is.EqualTo("New Game Event"));
            Assert.That(session.IsDirty, Is.True);
            Assert.That(session.AssetPath, Is.Null);
        }

        [Test]
        public void SaveAndLoad_RoundTripsCanonicalDocument()
        {
            string assetPath = TestFolder + "/Door.gameevent.json";
            GameEventEditorSession writer = CreateConfiguredSession("Door");

            writer.SaveDocument(assetPath);
            Assert.That(writer.HasOpenFile, Is.True);

            GameEventEditorSession reader = new GameEventEditorSession();
            reader.LoadDocument(assetPath);
            GameEventDocument document = reader.ValidateDocument();

            Assert.That(document.DisplayName, Is.EqualTo("Door"));
            Assert.That(document.TriggerTiming, Is.EqualTo("Interact:Door"));
            Assert.That(document.Condition, Is.EqualTo("$DoorOpen == false"));
            Assert.That(document.Commands, Is.EqualTo("OpenDoor();"));
            Assert.That(reader.IsDirty, Is.False);
            Assert.That(reader.HasOpenFile, Is.True);
            Assert.That(reader.AssetPath, Is.EqualTo(assetPath));
        }

        [Test]
        public void SaveDocument_InvalidCommandsDoesNotCreateAsset()
        {
            string assetPath = TestFolder + "/Invalid.gameevent.json";
            GameEventEditorSession session = CreateConfiguredSession("Invalid");
            session.Commands = "OpenDoor(";

            Assert.Throws<InvalidOperationException>(() => session.SaveDocument(assetPath));
            Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath), Is.Null);
        }

        [Test]
        public void SaveDocument_DuplicateGuidDoesNotCreateSecondAsset()
        {
            string firstPath = TestFolder + "/First.gameevent.json";
            string secondPath = TestFolder + "/Second.gameevent.json";
            GameEventEditorSession first = CreateConfiguredSession("First");
            first.SaveDocument(firstPath);

            GameEventEditorSession second = CreateConfiguredSession("Second");
            second.SetDocumentGuid(first.DocumentGuid);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => second.SaveDocument(secondPath));
            StringAssert.Contains("already used", exception.Message);
            Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>(secondPath), Is.Null);
        }

        [Test]
        public void SaveDocument_RejectsPathOutsideAssets()
        {
            string escapedPath = "Assets/../TempEscaped.gameevent.json";
            string escapedFullPath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "TempEscaped.gameevent.json"));
            GameEventEditorSession session = CreateConfiguredSession("Escaped");

            Assert.Throws<ArgumentException>(() => session.SaveDocument(escapedPath));
            Assert.That(File.Exists(escapedFullPath), Is.False);
        }

        [Test]
        public void ResetIfAssetMissing_ClearsDeletedOpenDocument()
        {
            string assetPath = TestFolder + "/Deleted.gameevent.json";
            GameEventEditorSession session = CreateConfiguredSession("Deleted");
            session.SaveDocument(assetPath);
            session.LoadDocument(assetPath);
            Assert.That(AssetDatabase.DeleteAsset(assetPath), Is.True);

            Assert.That(session.ResetIfAssetMissing(), Is.True);
            Assert.That(session.HasOpenFile, Is.False);
            Assert.That(session.AssetPath, Is.Null);
            Assert.That(session.DisplayName, Is.Empty);
            Assert.That(session.TriggerTiming, Is.Empty);
            Assert.That(session.Condition, Is.Empty);
            Assert.That(session.Commands, Is.Empty);
            Assert.That(session.IsDirty, Is.False);
        }

        [Test]
        public void ExistingSampleDocuments_LoadAndValidate()
        {
            const string sampleFolder =
                "Assets/KahaGameCore/Modules/GameFlowSystem/DefaultViews/SampleData/GameEvents";
            string[] paths = AssetDatabase
                .FindAssets("t:TextAsset", new[] { sampleFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(
                    ".gameevent.json",
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Assert.That(paths, Is.Not.Empty);
            for (int index = 0; index < paths.Length; index++)
            {
                GameEventEditorSession session = new GameEventEditorSession();
                session.LoadDocument(paths[index]);
                GameEventDocument document = null;
                Assert.DoesNotThrow(() => document = session.ValidateDocument());
                Assert.That(document.SchemaVersion, Is.EqualTo(2));
                Assert.DoesNotThrow(
                    () => GameEventConditionDraftCodec.Parse(session.Condition),
                    $"Condition in '{paths[index]}' must open in the structured editor.");
            }
        }

        [Test]
        public void SampleCatalog_PreservesFormerEffectiveExecutionOrder()
        {
            const string catalogPath =
                "Assets/KahaGameCore/Modules/GameFlowSystem/DefaultViews/" +
                "SampleData/GameEvents/GameEventCatalog.asset";
            GameEventCatalogAsset asset =
                AssetDatabase.LoadAssetAtPath<GameEventCatalogAsset>(catalogPath);

            Assert.That(asset, Is.Not.Null);
            Assert.DoesNotThrow(() => new GameEventCatalog(
                asset,
                new GameEventDocumentJsonCodec()));
            GameEventDocumentJsonCodec codec = new GameEventDocumentJsonCodec();
            string[] morningOrder = asset.Files
                .Select(file => codec.Read(file.text))
                .Where(document => document.TriggerTiming == "PhaseStart:Morning")
                .Select(document => document.DisplayName)
                .ToArray();

            CollectionAssert.AreEqual(
                new[] { "結局", "解鎖外出", "早晨自言自語" },
                morningOrder);
        }

        [Test]
        public void CommandDraftCodec_RoundTripsNestedAndQuotedArguments()
        {
            const string source =
                "AddParameter(Supplies,Random(5,15));Monologue(\"Morning, Idle\");";

            var drafts = GameEventCommandDraftCodec.Parse(source);
            string serialized = GameEventCommandDraftCodec.Serialize(drafts);
            var reparsed = GameEventCommandDraftCodec.Parse(serialized);

            Assert.That(reparsed.Count, Is.EqualTo(2));
            Assert.That(reparsed[0].Name, Is.EqualTo("AddParameter"));
            Assert.That(reparsed[0].Arguments[1], Is.EqualTo("Random(5,15)"));
            Assert.That(reparsed[1].Arguments[0], Is.EqualTo("Morning, Idle"));
        }

        [Test]
        public void CommandDraftCodec_RejectsExplicitTimingBlocks()
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => GameEventCommandDraftCodec.Parse("OnUse{OpenDoor();}"));

            StringAssert.Contains("TriggerTiming belongs to the document field", exception.Message);
        }

        [Test]
        public void ConditionDraftCodec_RoundTripsStructuredRows()
        {
            GameEventConditionGroupDraft root = GameEventConditionDraftCodec.Parse(
                "$Day>=2 && !$OutingUnlocked || $Spirit<=0");
            var firstGroup = (GameEventConditionGroupDraft)root.Children[0];
            var day = (GameEventConditionClauseDraft)firstGroup.Children[0];
            var outing = (GameEventConditionClauseDraft)firstGroup.Children[1];

            Assert.That(root.Mode, Is.EqualTo(GameEventConditionGroupMode.Any));
            Assert.That(root.Children, Has.Count.EqualTo(2));
            Assert.That(firstGroup.Mode, Is.EqualTo(GameEventConditionGroupMode.All));
            Assert.That(day.ParameterKey, Is.EqualTo("Day"));
            Assert.That(day.Operator, Is.EqualTo(">="));
            Assert.That(day.Value, Is.EqualTo("2"));
            Assert.That(outing.ParameterKey, Is.EqualTo("OutingUnlocked"));
            Assert.That(outing.Value, Is.EqualTo("false"));

            Assert.That(
                GameEventConditionDraftCodec.Serialize(root),
                Is.EqualTo("($Day>=2 && !$OutingUnlocked) || $Spirit<=0"));
        }

        [Test]
        public void ConditionDraftCodec_EmptyConditionMeansAlways()
        {
            GameEventConditionGroupDraft root =
                GameEventConditionDraftCodec.Parse(string.Empty);

            Assert.That(root.Children, Is.Empty);
            Assert.That(GameEventConditionDraftCodec.Serialize(root), Is.Empty);
        }

        [Test]
        public void ConditionDraftCodec_ParenthesesCreateNestedGroups()
        {
            GameEventConditionGroupDraft root = GameEventConditionDraftCodec.Parse(
                "($DoorOpen || $HasKey) && $Day>=2");

            Assert.That(root.Mode, Is.EqualTo(GameEventConditionGroupMode.All));
            Assert.That(root.Children, Has.Count.EqualTo(2));
            Assert.That(
                ((GameEventConditionGroupDraft)root.Children[0]).Mode,
                Is.EqualTo(GameEventConditionGroupMode.Any));
            Assert.That(
                GameEventConditionDraftCodec.Serialize(root),
                Is.EqualTo("($DoorOpen || $HasKey) && $Day>=2"));
        }

        [Test]
        public void ConditionDraftCodec_RejectsNegatedGroupWithoutChangingIt()
        {
            Assert.Throws<InvalidOperationException>(
                () => GameEventConditionDraftCodec.Parse("!($DoorOpen || $HasKey)"));
        }

        [Test]
        public void ProjectCatalog_NoSelectionDoesNotLoadSampleParameterTables()
        {
            GameEventProjectAuthoringCatalog catalog =
                GameEventProjectAuthoringCatalog.Load(
                    Array.Empty<TextAsset>(),
                    null,
                    null);

            Assert.That(catalog.Parameters, Is.Empty);
        }

        [Test]
        public void ProjectCatalog_NoSelectionDoesNotLoadDefaultCommands()
        {
            GameEventProjectAuthoringCatalog catalog =
                GameEventProjectAuthoringCatalog.Load(
                    Array.Empty<TextAsset>(),
                    null,
                    null);

            Assert.That(catalog.Commands, Is.Empty);
        }

        [Test]
        public void ProjectCatalog_SelectCommandsUsesDataCatalogSelection()
        {
            var registered = new[]
            {
                new EffectCommandDescriptor(
                    "First",
                    "First",
                    "Test",
                    Array.Empty<EffectCommandParameterDefinition>()),
                new EffectCommandDescriptor(
                    "Second",
                    "Second",
                    "Test",
                    Array.Empty<EffectCommandParameterDefinition>())
            };
            var warnings = new List<string>();

            IReadOnlyList<EffectCommandDescriptor> selected =
                GameEventProjectAuthoringCatalog.SelectCommands(
                    registered,
                    new[] { "Second", "Missing" },
                    warnings);

            Assert.That(selected.Select(command => command.Name), Is.EqualTo(new[] { "Second" }));
            Assert.That(warnings, Has.Count.EqualTo(1));
            StringAssert.Contains("Missing", warnings[0]);
        }

        [Test]
        public void HasRequiredDataCatalog_RejectsMissingCatalog()
        {
            GameEventEditorDataSource source = new GameEventEditorDataSource(
                "Test Catalog",
                typeof(GameEventCatalogAsset),
                asset => (GameEventCatalogAsset)asset,
                (asset, eventCatalog) => { },
                asset => Array.Empty<TextAsset>(),
                (asset, tables) => { },
                asset => Array.Empty<string>(),
                (asset, commandNames) => { },
                asset => Array.Empty<string>());
            GameEventCatalogAsset catalog =
                ScriptableObject.CreateInstance<GameEventCatalogAsset>();

            try
            {
                Assert.That(
                    GameEventDocumentEditorWindow.HasRequiredDataCatalog(source, null),
                    Is.False);
                Assert.That(
                    GameEventDocumentEditorWindow.HasRequiredDataCatalog(source, catalog),
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void ProjectCatalog_LoadsOnlySelectedParameterTables()
        {
            const string selectedPath =
                "Assets/KahaGameCore/Modules/GameFlowSystem/DefaultViews/" +
                "SampleData/Parameters/CoreGameplay.parameters.json";
            TextAsset selected = AssetDatabase.LoadAssetAtPath<TextAsset>(selectedPath);

            Assert.That(selected, Is.Not.Null);
            GameEventProjectAuthoringCatalog catalog =
                GameEventProjectAuthoringCatalog.Load(
                    new[] { selected },
                    null,
                    null);

            Assert.That(catalog.Parameters.Count, Is.EqualTo(8));
            Assert.That(
                catalog.Parameters.Select(parameter => parameter.Key),
                Does.Contain("Supplies"));
        }

        [Test]
        public void EventCatalogPanel_AddEventPersistsNewFile()
        {
            string eventPath = TestFolder + "/Cataloged.gameevent.json";
            string catalogPath = TestFolder + "/GameEventCatalog.asset";
            GameEventEditorSession eventSession =
                CreateConfiguredSession("Cataloged");
            eventSession.SaveDocument(eventPath);
            TextAsset eventAsset =
                AssetDatabase.LoadAssetAtPath<TextAsset>(eventPath);
            GameEventCatalogAsset catalogAsset =
                ScriptableObject.CreateInstance<GameEventCatalogAsset>();
            AssetDatabase.CreateAsset(catalogAsset, catalogPath);

            GameEventCatalogEditorPanel panel =
                new GameEventCatalogEditorPanel();
            panel.SetCatalog(catalogAsset);
            panel.AddEvent(eventAsset);

            Assert.That(catalogAsset.Files, Has.Count.EqualTo(1));
            Assert.That(catalogAsset.Files[0], Is.EqualTo(eventAsset));
        }

        [TestCase(
            "Assets/Data/Gameplay.json",
            "Assets/Data/Gameplay.parameters.json")]
        [TestCase(
            "Assets/Data/Gameplay",
            "Assets/Data/Gameplay.parameters.json")]
        [TestCase(
            "Assets/Data/Gameplay.parameters.json",
            "Assets/Data/Gameplay.parameters.json")]
        public void EnsureParameterTableExtension_ProducesCanonicalAssetName(
            string source,
            string expected)
        {
            Assert.That(
                GameEventDocumentEditorWindow.EnsureParameterTableExtension(source),
                Is.EqualTo(expected));
        }

        [TestCase(
            "Assets/Data/Door.json",
            "Assets/Data/Door.gameevent.json")]
        [TestCase(
            "Assets/Data/Door",
            "Assets/Data/Door.gameevent.json")]
        [TestCase(
            "Assets/Data/Door.gameevent.json",
            "Assets/Data/Door.gameevent.json")]
        public void EnsureGameEventExtension_ProducesCanonicalAssetName(
            string source,
            string expected)
        {
            Assert.That(
                GameEventDocumentEditorWindow.EnsureGameEventExtension(source),
                Is.EqualTo(expected));
        }

        [Test]
        public void DefaultEventCatalogPath_UsesGameEventDirectory()
        {
            Assert.That(
                GameEventDocumentEditorWindow.GetDefaultEventCatalogPath(
                    "Assets/Data/GameEvent/Door.gameevent.json"),
                Is.EqualTo("Assets/Data/GameEvent/GameEventCatalog.asset"));
        }

        [Test]
        public void ConditionSetupState_WithSelectedEmptyTable_RequestsParameter()
        {
            Assert.That(
                GameEventDocumentEditorWindow.GetConditionSetupState(true, 0),
                Is.EqualTo(
                    GameEventDocumentEditorWindow.ConditionSetupState
                        .AddConditionParameter));
        }

        [Test]
        public void ConditionSetupState_WithoutTable_RequestsTable()
        {
            Assert.That(
                GameEventDocumentEditorWindow.GetConditionSetupState(false, 0),
                Is.EqualTo(
                    GameEventDocumentEditorWindow.ConditionSetupState
                        .SelectParameterTable));
        }

        [Test]
        public void ConditionSetupState_WithConditionParameter_IsReady()
        {
            Assert.That(
                GameEventDocumentEditorWindow.GetConditionSetupState(true, 1),
                Is.EqualTo(
                    GameEventDocumentEditorWindow.ConditionSetupState.Ready));
        }

        private static GameEventEditorSession CreateConfiguredSession(string displayName)
        {
            GameEventEditorSession session = new GameEventEditorSession();
            session.NewDocument();
            session.DisplayName = displayName;
            session.TriggerTiming = "Interact:Door";
            session.Condition = "$DoorOpen == false";
            session.Commands = "OpenDoor();";
            return session;
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
