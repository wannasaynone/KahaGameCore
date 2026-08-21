using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KahaGameCore.Effects;
using KahaGameCore.GameEvents.Editor;
using KahaGameCore.Parameters;
using KahaGameCore.Parameters.EffectsIntegration;
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
            Assert.That(document.DisplayName, Is.EqualTo("新遊戲事件"));
            Assert.That(session.IsDirty, Is.True);
            Assert.That(session.AssetPath, Is.Null);
        }

        [Test]
        public void CommandArgumentEditor_SetParameterBoolValue_UsesBooleanChoice()
        {
            EffectCommandDescriptor descriptor = ParameterEffectCommandRegistrar
                .Descriptors
                .Single(command => command.Name == "SetParameter");
            GameEventProjectAuthoringCatalog authoringCatalog =
                GameEventProjectAuthoringCatalog.Load(
                    null,
                    new[]
                    {
                        new ParameterAuthoringEntry(
                            "table-guid",
                            "測試參數表",
                            "Assets/Test.parameters.json",
                            ParameterDefinition.Bool("IsUnlocked", "已解鎖", false))
                    },
                    Array.Empty<string>());
            GameEventCommandDraft draft = new GameEventCommandDraft
            {
                Name = "SetParameter",
                Arguments = new List<string> { "IsUnlocked", string.Empty }
            };

            Assert.That(
                GameEventDocumentEditorWindow.GetCommandArgumentEditorKind(
                    draft,
                    descriptor,
                    1,
                    authoringCatalog),
                Is.EqualTo(
                    GameEventDocumentEditorWindow.CommandArgumentEditorKind.Boolean));
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
        public void CommandDraftOperations_InsertBlank_InsertsAtRequestedPosition()
        {
            List<GameEventCommandDraft> drafts = GameEventCommandDraftCodec.Parse(
                "OpenDoor();CloseDoor();");

            GameEventCommandDraftOperations.InsertBlank(drafts, 1);

            Assert.That(drafts, Has.Count.EqualTo(3));
            Assert.That(drafts[0].Name, Is.EqualTo("OpenDoor"));
            Assert.That(drafts[1].Name, Is.Null);
            Assert.That(drafts[1].Arguments, Is.Empty);
            Assert.That(drafts[2].Name, Is.EqualTo("CloseDoor"));
        }

        [Test]
        public void CommandDraftOperations_Duplicate_InsertsIndependentCopyAfterSource()
        {
            List<GameEventCommandDraft> drafts = GameEventCommandDraftCodec.Parse(
                "Monologue(Opening);OpenDoor();");

            GameEventCommandDraftOperations.Duplicate(drafts, 0);
            drafts[1].Arguments[0] = "Copied";

            Assert.That(drafts, Has.Count.EqualTo(3));
            Assert.That(drafts[0].Name, Is.EqualTo("Monologue"));
            Assert.That(drafts[0].Arguments[0], Is.EqualTo("Opening"));
            Assert.That(drafts[1].Name, Is.EqualTo("Monologue"));
            Assert.That(drafts[1].Arguments[0], Is.EqualTo("Copied"));
            Assert.That(drafts[2].Name, Is.EqualTo("OpenDoor"));
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
                GameEventProjectAuthoringCatalog.Load(null);

            Assert.That(catalog.Parameters, Is.Empty);
        }

        [Test]
        public void ProjectCatalog_NoSelectionDoesNotLoadDefaultCommands()
        {
            GameEventProjectAuthoringCatalog catalog =
                GameEventProjectAuthoringCatalog.Load(null);

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
        public void CommandCatalog_ScansOnlySelectedAsmdef()
        {
            var warnings = new List<string>();

            IReadOnlyList<EffectCommandDescriptor> none =
                EffectCommandAssemblyCatalog.GetDescriptors(Array.Empty<string>(), warnings);
            IReadOnlyList<EffectCommandDescriptor> parameters =
                EffectCommandAssemblyCatalog.GetDescriptors(
                    new[] { "KahaGameCore.Modules.Parameters.EffectsIntegration" },
                    warnings);

            Assert.That(none, Is.Empty);
            Assert.That(
                parameters.Select(item => item.Name),
                Is.EquivalentTo(new[] { "AddParameter", "SetParameter" }));
            Assert.That(warnings, Is.Empty);
        }

        [Test]
        public void GameEventCatalog_NormalizesEditableAuthoringSettings()
        {
            GameEventCatalogAsset catalog =
                ScriptableObject.CreateInstance<GameEventCatalogAsset>();

            try
            {
                catalog.SetTriggerTimings(new[] { " GameStart ", "GameStart", "" });
                catalog.SetCommandAssemblyNames(new[] { "Project.Game", "Project.Game" });
                catalog.SetEnabledCommandNames(new[] { "AddParameter", "AddParameter" });

                Assert.That(catalog.TriggerTimings, Is.EqualTo(new[] { "GameStart" }));
                Assert.That(catalog.CommandAssemblyNames, Is.EqualTo(new[] { "Project.Game" }));
                Assert.That(catalog.EnabledCommandNames, Is.EqualTo(new[] { "AddParameter" }));
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
            GameEventCatalogAsset asset = ScriptableObject.CreateInstance<GameEventCatalogAsset>();
            try
            {
                asset.SetParameterTables(new[] { selected });
                GameEventProjectAuthoringCatalog catalog =
                    GameEventProjectAuthoringCatalog.Load(asset);

                Assert.That(catalog.Parameters.Count, Is.EqualTo(8));
                Assert.That(
                    catalog.Parameters.Select(parameter => parameter.Key),
                    Does.Contain("Supplies"));
                ParameterAuthoringEntry supplies = catalog.ParameterEntries
                    .Single(entry => entry.Definition.Key == "Supplies");
                Assert.That(supplies.TableDisplayName, Is.EqualTo("Core Gameplay"));
                Assert.That(
                    supplies.TableGuid,
                    Is.EqualTo("9d02b7c1-1e01-4e00-8000-000000000001"));
                Assert.That(supplies.AssetPath, Is.EqualTo(selectedPath));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void ProjectCatalog_DuplicateParameterKeyAcrossTablesIsAnError()
        {
            TextAsset world = CreateParameterTable(
                "World.parameters.json",
                "World",
                ParameterDefinition.Bool(
                    "World.Temp1.DoorOpened",
                    "地下室門已開啟",
                    false));
            TextAsset story = CreateParameterTable(
                "Story.parameters.json",
                "Story",
                ParameterDefinition.Bool(
                    "World.Temp1.DoorOpened",
                    "劇情中的門狀態",
                    true));
            GameEventCatalogAsset asset =
                ScriptableObject.CreateInstance<GameEventCatalogAsset>();

            try
            {
                asset.SetParameterTables(new[] { world, story });
                GameEventProjectAuthoringCatalog catalog =
                    GameEventProjectAuthoringCatalog.Load(asset);

                Assert.That(catalog.Errors, Has.Count.EqualTo(1));
                StringAssert.Contains("World.Temp1.DoorOpened", catalog.Errors[0]);
                StringAssert.Contains("World.parameters.json", catalog.Errors[0]);
                StringAssert.Contains("Story.parameters.json", catalog.Errors[0]);
                Assert.That(
                    catalog.ParameterEntries.Count(
                        entry => entry.Definition.Key == "World.Temp1.DoorOpened"),
                    Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void ParameterWorkspace_SwitchingTablesPreservesEachDirtySession()
        {
            TextAsset world = CreateParameterTable(
                "WorkspaceWorld.parameters.json",
                "World",
                ParameterDefinition.Bool("World.DoorOpen", "門已開啟", false));
            TextAsset story = CreateParameterTable(
                "WorkspaceStory.parameters.json",
                "Story",
                ParameterDefinition.Int("Story.Chapter", "章節", 1, 1, 10));
            ParameterTableWorkspace workspace = new ParameterTableWorkspace();
            workspace.Bind(new[] { world, story });

            workspace.Expand(AssetDatabase.GetAssetPath(world));
            workspace.AddParameter(
                AssetDatabase.GetAssetPath(world),
                ParameterDefinition.Bool("World.LampOn", "燈已開啟", false));
            workspace.Expand(AssetDatabase.GetAssetPath(story));
            workspace.AddParameter(
                AssetDatabase.GetAssetPath(story),
                ParameterDefinition.Int("Story.Score", "分數", 0, 0, 999));

            Assert.That(workspace.DirtyCount, Is.EqualTo(2));
            Assert.That(workspace.HasUnsavedChanges, Is.True);
            Assert.That(workspace.Sessions.All(item => item.Expanded), Is.True);
            IReadOnlyList<ParameterAuthoringEntry> entries =
                workspace.BuildEntries(out IReadOnlyList<string> errors);
            Assert.That(errors, Is.Empty);
            Assert.That(
                entries.Select(entry => entry.Definition.Key),
                Does.Contain("World.LampOn"));
            Assert.That(
                entries.Select(entry => entry.Definition.Key),
                Does.Contain("Story.Score"));

            workspace.SaveAll();

            Assert.That(workspace.DirtyCount, Is.Zero);
            ParameterTable savedWorld = new ParameterTableJsonCodec().Read(
                AssetDatabase.LoadAssetAtPath<TextAsset>(
                    AssetDatabase.GetAssetPath(world)).text);
            ParameterTable savedStory = new ParameterTableJsonCodec().Read(
                AssetDatabase.LoadAssetAtPath<TextAsset>(
                    AssetDatabase.GetAssetPath(story)).text);
            Assert.That(
                savedWorld.Definitions.Select(definition => definition.Key),
                Does.Contain("World.LampOn"));
            Assert.That(
                savedStory.Definitions.Select(definition => definition.Key),
                Does.Contain("Story.Score"));
        }

        [Test]
        public void ParameterWorkspace_AddParameterIsImmediatelyAvailableBeforeSave()
        {
            TextAsset table = CreateParameterTable(
                "WorkspaceDirectAdd.parameters.json",
                "World",
                ParameterDefinition.Bool("World.DoorOpen", "門已開啟", false));
            ParameterTableWorkspace workspace = new ParameterTableWorkspace();
            workspace.Bind(new[] { table });

            workspace.AddParameter(
                AssetDatabase.GetAssetPath(table),
                ParameterDefinition.Int("World.Alert", "警戒值", 0, 0, 100));
            IReadOnlyList<ParameterAuthoringEntry> entries =
                workspace.BuildEntries(out IReadOnlyList<string> errors);

            Assert.That(errors, Is.Empty);
            Assert.That(workspace.HasUnsavedChanges, Is.True);
            Assert.That(
                entries.Single(entry => entry.Definition.Key == "World.Alert")
                    .TableDisplayName,
                Is.EqualTo("World"));
        }

        [Test]
        public void ParameterWorkspace_AddParameterRejectsCrossTableDuplicateKey()
        {
            TextAsset world = CreateParameterTable(
                "WorkspaceDuplicateWorld.parameters.json",
                "World",
                ParameterDefinition.Bool("Shared.Flag", "共用旗標", false));
            TextAsset story = CreateParameterTable(
                "WorkspaceDuplicateStory.parameters.json",
                "Story",
                ParameterDefinition.Int("Story.Chapter", "章節", 1, 1, 10));
            ParameterTableWorkspace workspace = new ParameterTableWorkspace();
            workspace.Bind(new[] { world, story });

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => workspace.AddParameter(
                    AssetDatabase.GetAssetPath(story),
                    ParameterDefinition.Bool("Shared.Flag", "重複旗標", true)));

            StringAssert.Contains("Shared.Flag", exception.Message);
            Assert.That(workspace.DirtyCount, Is.Zero);
        }

        [Test]
        public void ParameterUsageIndex_FindsConditionAndTypedCommandReferences()
        {
            string eventPath = TestFolder + "/Usage.gameevent.json";
            GameEventEditorSession eventSession = new GameEventEditorSession();
            eventSession.NewDocument();
            eventSession.DisplayName = "Usage Event";
            eventSession.Condition = "$World.DoorOpen && $Story.Score > 0";
            eventSession.Commands =
                "SetParameter(World.DoorOpen,true);AddScore($Story.Score+1);";
            eventSession.SaveDocument(eventPath);
            TextAsset eventAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(eventPath);
            GameEventCatalogAsset eventCatalog =
                ScriptableObject.CreateInstance<GameEventCatalogAsset>();
            SerializedObject serializedCatalog = new SerializedObject(eventCatalog);
            SerializedProperty files = serializedCatalog.FindProperty("files");
            files.arraySize = 1;
            files.GetArrayElementAtIndex(0).objectReferenceValue = eventAsset;
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EffectCommandDescriptor[] commands =
            {
                new EffectCommandDescriptor(
                    "SetParameter",
                    "Set Parameter",
                    "Parameters",
                    new[]
                    {
                        new EffectCommandParameterDefinition(
                            "key",
                            EffectCommandParameterKind.ParameterKey),
                        new EffectCommandParameterDefinition(
                            "value",
                            EffectCommandParameterKind.Literal)
                    }),
                new EffectCommandDescriptor(
                    "AddScore",
                    "Add Score",
                    "Parameters",
                    new[]
                    {
                        new EffectCommandParameterDefinition(
                            "amount",
                            EffectCommandParameterKind.NumberExpression)
                    })
            };

            try
            {
                GameEventParameterUsageIndex index =
                    GameEventParameterUsageIndex.Build(eventCatalog, commands);

                GameEventParameterReference door =
                    index.Find("World.DoorOpen").Single();
                Assert.That(door.EventDisplayName, Is.EqualTo("Usage Event"));
                Assert.That(door.UsedInCondition, Is.True);
                Assert.That(door.CommandNames, Is.EqualTo(new[] { "SetParameter" }));
                GameEventParameterReference score =
                    index.Find("Story.Score").Single();
                Assert.That(score.UsedInCondition, Is.True);
                Assert.That(score.CommandNames, Is.EqualTo(new[] { "AddScore" }));
                Assert.That(index.Warnings, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(eventCatalog);
            }
        }

        [Test]
        public void ParameterUsageIndex_OpenDocumentOverridesSavedEventReferences()
        {
            string eventPath = TestFolder + "/UsageOverride.gameevent.json";
            GameEventEditorSession eventSession = new GameEventEditorSession();
            eventSession.NewDocument();
            eventSession.DisplayName = "Saved Event";
            eventSession.Condition = "$Saved.Flag";
            eventSession.SaveDocument(eventPath);
            TextAsset eventAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(eventPath);
            GameEventCatalogAsset eventCatalog =
                ScriptableObject.CreateInstance<GameEventCatalogAsset>();
            SerializedObject serializedCatalog = new SerializedObject(eventCatalog);
            SerializedProperty files = serializedCatalog.FindProperty("files");
            files.arraySize = 1;
            files.GetArrayElementAtIndex(0).objectReferenceValue = eventAsset;
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();

            try
            {
                OpenGameEventUsageDocument openDocument =
                    new OpenGameEventUsageDocument(
                        eventAsset,
                        eventPath,
                        "Unsaved Event",
                        "$Unsaved.Flag",
                        string.Empty);
                GameEventParameterUsageIndex index =
                    GameEventParameterUsageIndex.Build(
                        eventCatalog,
                        Array.Empty<EffectCommandDescriptor>(),
                        openDocument);

                Assert.That(index.Find("Saved.Flag"), Is.Empty);
                Assert.That(
                    index.Find("Unsaved.Flag").Single().EventDisplayName,
                    Is.EqualTo("Unsaved Event"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(eventCatalog);
            }
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

        private static TextAsset CreateParameterTable(
            string fileName,
            string displayName,
            ParameterDefinition definition)
        {
            string assetPath = TestFolder + "/" + fileName;
            ParameterTable table = new ParameterTable(
                Guid.NewGuid().ToString(),
                displayName,
                new[] { definition });
            string fullPath = Path.Combine(
                Application.dataPath,
                assetPath.Substring("Assets/".Length));
            File.WriteAllText(
                fullPath,
                new ParameterTableJsonCodec().Write(table));
            AssetDatabase.ImportAsset(
                assetPath,
                ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
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
