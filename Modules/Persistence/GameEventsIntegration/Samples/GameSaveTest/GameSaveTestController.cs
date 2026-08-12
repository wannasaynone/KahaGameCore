using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;
using KahaGameCore.GameEvents;
using KahaGameCore.GameFlowSystem.DefaultImplements;
using KahaGameCore.GameFlowSystem.DefaultImplements.Data;
using KahaGameCore.GameFlowSystem.DefaultImplements.DataAccess;
using KahaGameCore.Parameters;
using KahaGameCore.Persistence;
using KahaGameCore.Persistence.GameEventsIntegration;
using KahaGameCore.Presentation;
using KahaGameCore.StaticData;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KahaGameCore.Samples.GameSaveTest
{
    [DisallowMultipleComponent]
    public sealed class GameSaveTestController : MonoBehaviour
    {
        private const int TestSlot = 0;
        private const string MachineStageKey = "MachineStage";
        private const string MorningPhaseKey = "Morning";
        private const string NightPhaseKey = "Night";

        private ParameterStore parameters;
        private TimeService time;
        private SaveParticipantRegistry participants;
        private GameSaveDocumentJsonCodec codec;
        private GameSaveSlotStore slots;
        private GameSaveCoordinator saves;
        private Transform player;
        private GameObject stateA;
        private GameObject stateB;
        private TextAsset phaseTable;
        private string status;
        private bool isBusy;

        public int MachineStage => parameters.GetInt(MachineStageKey);
        public string CurrentPhaseKey => time.CurrentPhase.Key;
        public Vector3 PlayerPosition => player.position;
        public bool StateAActive => stateA.activeSelf;
        public bool StateBActive => stateB.activeSelf;
        public bool SaveExists => slots.Exists(TestSlot);

        private void Awake()
        {
            CreateModel();
            CreateVisuals();
            CreatePersistence();
            status = SaveExists
                ? "Ready. An existing sample save was found."
                : "Ready. No sample save exists.";
        }

        private void OnDestroy()
        {
            if (phaseTable != null)
            {
                Destroy(phaseTable);
            }
        }

        public void MutateState()
        {
            if (isBusy)
            {
                return;
            }

            int nextStage = (MachineStage + 1) % 3;
            parameters.Set(MachineStageKey, nextStage);
            time.SetPhase(
                string.Equals(CurrentPhaseKey, MorningPhaseKey, StringComparison.Ordinal)
                    ? NightPhaseKey
                    : MorningPhaseKey);
            player.position = new Vector3(StageToPlayerX(nextStage), -2f, 0f);
            status = $"Changed to stage {nextStage}. This is not saved yet.";
        }

        public async UniTask SaveAsync()
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;
            status = "Waiting for the Game Event queue, then saving...";
            try
            {
                await saves.SaveAsync(
                    TestSlot,
                    SceneManager.GetActiveScene().name,
                    CancellationToken.None);
                status = $"Saved stage {MachineStage}, phase {CurrentPhaseKey}, player X {PlayerPosition.x:0}.";
            }
            catch (Exception exception)
            {
                status = $"SAVE FAILED: {exception.Message}";
                throw;
            }
            finally
            {
                isBusy = false;
            }
        }

        public void Load()
        {
            if (isBusy)
            {
                return;
            }

            if (!SaveExists)
            {
                status = "LOAD FAILED: Save first.";
                return;
            }

            try
            {
                GameSaveSnapshot snapshot = codec.Read(
                    slots.Load(TestSlot),
                    participants);
                string activeScene = SceneManager.GetActiveScene().name;
                if (!string.Equals(
                        snapshot.SceneKey,
                        activeScene,
                        StringComparison.Ordinal))
                {
                    status = $"LOAD FAILED: Save belongs to scene '{snapshot.SceneKey}'.";
                    return;
                }

                parameters.Restore(snapshot.Parameters);
                participants.Restore(snapshot.Participants);
                status = $"Loaded stage {MachineStage}, phase {CurrentPhaseKey}, player X {PlayerPosition.x:0}.";
            }
            catch (Exception exception)
            {
                status = $"LOAD FAILED: {exception.Message}";
                throw;
            }
        }

        public void DeleteSave()
        {
            if (isBusy)
            {
                return;
            }

            status = slots.Delete(TestSlot)
                ? "Deleted the sample save."
                : "No sample save to delete.";
        }

        public void ReloadScene()
        {
            if (isBusy)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex < 0)
            {
                status = "RELOAD FAILED: Add this scene to Build Settings.";
                return;
            }

            SceneManager.LoadScene(activeScene.buildIndex);
        }

        private void CreateModel()
        {
            parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int(
                    TimeService.DayParameterKey,
                    "Day",
                    1,
                    1,
                    999),
                ParameterDefinition.Int(
                    MachineStageKey,
                    "Machine Stage",
                    0,
                    0,
                    2)
            });

            phaseTable = new TextAsset(
                "[{\"ID\":1,\"Key\":\"Morning\",\"DisplayName\":\"Morning\",\"NextID\":2,\"IsNewDay\":1}," +
                "{\"ID\":2,\"Key\":\"Night\",\"DisplayName\":\"Night\",\"NextID\":1,\"IsNewDay\":0}]")
            {
                name = nameof(TimePhaseData)
            };
            GameStaticDataManager staticData = new GameStaticDataManager();
            staticData.Add<TimePhaseData>(
                new TextAssetJsonStaticDataHandler(phaseTable));
            time = new TimeService(staticData, parameters);
            time.ResetToFirstPhase();
        }

        private void CreateVisuals()
        {
            GameObject machine = new GameObject("MachineState");
            machine.transform.SetParent(transform, false);
            machine.transform.localPosition = new Vector3(0f, 1f, 0f);

            stateA = CreateCube(
                "StateA_Green",
                machine.transform,
                Vector3.zero,
                new Color(0.18f, 0.8f, 0.35f));
            stateB = CreateCube(
                "StateB_Orange",
                machine.transform,
                Vector3.zero,
                new Color(1f, 0.45f, 0.1f));

            ParameterStateBinder binder =
                machine.AddComponent<ParameterStateBinder>();
            binder.Configure(new[]
            {
                new ParameterChildConditionBinding(
                    stateA,
                    $"${MachineStageKey} == 0 || ${MachineStageKey} == 2"),
                new ParameterChildConditionBinding(
                    stateB,
                    $"${MachineStageKey} == 1")
            });
            binder.Initialize(parameters);

            GameObject playerObject = CreateCube(
                "SavedPlayer_Blue",
                transform,
                new Vector3(0f, -2f, 0f),
                new Color(0.15f, 0.45f, 1f));
            playerObject.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            player = playerObject.transform;
        }

        private void CreatePersistence()
        {
            participants = new SaveParticipantRegistry();
            participants.Register(time);
            participants.Register(
                new TransformSaveParticipant("Sample.PlayerTransform", player));

            GameEventDocumentJsonCodec eventCodec =
                new GameEventDocumentJsonCodec();
            GameEventCatalog catalog = new GameEventCatalog(
                Array.Empty<TextAsset>(),
                eventCodec);
            GameEventRunner runner = new GameEventRunner(
                catalog,
                new EffectRuntime(new EffectCommandRegistry()),
                parameters,
                eventCodec);

            codec = new GameSaveDocumentJsonCodec();
            slots = new GameSaveSlotStore(Path.Combine(
                Application.persistentDataPath,
                "KahaGameCore",
                "GameSaveTest"));
            saves = new GameSaveCoordinator(
                runner,
                parameters,
                participants,
                codec,
                slots);
        }

        private static GameObject CreateCube(
            string objectName,
            Transform parent,
            Vector3 localPosition,
            Color color)
        {
            GameObject result = GameObject.CreatePrimitive(PrimitiveType.Cube);
            result.name = objectName;
            result.transform.SetParent(parent, false);
            result.transform.localPosition = localPosition;
            Renderer renderer = result.GetComponent<Renderer>();
            Material material = renderer.material;
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader != null)
            {
                material.shader = unlitShader;
            }
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            return result;
        }

        private static float StageToPlayerX(int stage)
        {
            switch (stage)
            {
                case 1:
                    return 3f;
                case 2:
                    return -3f;
                default:
                    return 0f;
            }
        }

        private void OnGUI()
        {
            float uiScale = Mathf.Clamp(Screen.height / 720f, 1f, 2.5f);
            Matrix4x4 previousMatrix = GUI.matrix;
            bool previousEnabled = GUI.enabled;
            GUI.matrix = Matrix4x4.Scale(
                new Vector3(uiScale, uiScale, 1f));

            float logicalWidth = Screen.width / uiScale;
            float logicalHeight = Screen.height / uiScale;
            float panelWidth = Mathf.Min(620f, logicalWidth - 40f);
            float panelHeight = Mathf.Min(550f, logicalHeight - 40f);

            GUIStyle panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(18, 18, 14, 14)
            };
            GUIStyle headingStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold
            };
            GUIStyle instructionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                wordWrap = true
            };
            GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18
            };
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            GUIStyle statusStyle = new GUIStyle(GUI.skin.textArea)
            {
                fontSize = 16,
                wordWrap = true
            };

            try
            {
                GUILayout.BeginArea(
                    new Rect(20f, 20f, panelWidth, panelHeight),
                    panelStyle);
                GUILayout.Label("GAME SAVE TEST", headingStyle);
                GUILayout.Space(4f);
                GUILayout.Label(
                    "1. Change State   2. Save   3. Change or Reload   4. Load",
                    instructionStyle);
                GUILayout.Space(6f);
                GUILayout.Label(
                    $"MachineStage parameter: {MachineStage}",
                    bodyStyle);
                GUILayout.Label(
                    $"Visible child: {(StateAActive ? "A / GREEN" : "B / ORANGE")}",
                    bodyStyle);
                GUILayout.Label(
                    $"TimeService phase: {CurrentPhaseKey}",
                    bodyStyle);
                GUILayout.Label(
                    $"Player participant X: {PlayerPosition.x:0}",
                    bodyStyle);
                GUILayout.Label(
                    $"Sample save exists: {SaveExists}",
                    bodyStyle);
                GUILayout.Space(8f);

                GUI.enabled = !isBusy;
                if (GUILayout.Button(
                        "1  CHANGE STATE",
                        buttonStyle,
                        GUILayout.Height(40f)))
                {
                    MutateState();
                }
                if (GUILayout.Button(
                        "2  SAVE",
                        buttonStyle,
                        GUILayout.Height(40f)))
                {
                    SaveAsync().Forget();
                }
                if (GUILayout.Button(
                        "3  RELOAD SCENE",
                        buttonStyle,
                        GUILayout.Height(40f)))
                {
                    ReloadScene();
                }
                if (GUILayout.Button(
                        "4  LOAD",
                        buttonStyle,
                        GUILayout.Height(40f)))
                {
                    Load();
                }
                if (GUILayout.Button(
                        "DELETE SAMPLE SAVE",
                        buttonStyle,
                        GUILayout.Height(32f)))
                {
                    DeleteSave();
                }
                GUI.enabled = true;

                GUILayout.Space(8f);
                GUILayout.Label(
                    status,
                    statusStyle,
                    GUILayout.MinHeight(52f));
                GUILayout.EndArea();
            }
            finally
            {
                GUI.enabled = previousEnabled;
                GUI.matrix = previousMatrix;
            }
        }
    }

    internal sealed class TransformSnapshot
    {
        public float X;
        public float Y;
        public float Z;
    }

    internal sealed class TransformSaveParticipant :
        ISaveParticipant<TransformSnapshot>
    {
        private readonly Transform target;

        public TransformSaveParticipant(string saveKey, Transform target)
        {
            if (string.IsNullOrWhiteSpace(saveKey))
            {
                throw new ArgumentException(
                    "Transform participant requires a SaveKey.",
                    nameof(saveKey));
            }

            SaveKey = saveKey;
            this.target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public string SaveKey { get; }

        public TransformSnapshot Capture()
        {
            Vector3 position = target.position;
            return new TransformSnapshot
            {
                X = position.x,
                Y = position.y,
                Z = position.z
            };
        }

        public void Restore(TransformSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            target.position = new Vector3(snapshot.X, snapshot.Y, snapshot.Z);
        }
    }
}
