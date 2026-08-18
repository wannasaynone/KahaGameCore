using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.StaticData;
using KahaGameCore.Foundation.Messaging;
using KahaGameCore.GameFlowSystem.DefaultImplements;
using KahaGameCore.GameFlowSystem.DefaultImplements.Data;
using KahaGameCore.GameFlowSystem.DefaultImplements.DataAccess;
using KahaGameCore.GameFlowSystem.DefaultImplements.Events;
using KahaGameCore.UserInterfaceSystem;
using KahaGameCore.Dialogue;
using KahaGameCore.Dialogue.View;
using KahaGameCore.Parameters;
using KahaGameCore.GameEvents;
using KahaGameCore.GameFlowSystem.GameEventsIntegration;
using KahaGameCore.Presentation;
using UnityEngine;

namespace KahaGameCore.GameFlowSystem.DefaultViews
{
    /// <summary>
    /// 預設組裝根（Composition Root）：載入表格、組裝所有服務與預設 UI，控制「主標題 ↔ 遊戲流程」切換。
    /// 場景與 View prefabs 由選單「KahaGameCore → GameFlowSystem → Build Default UI Prefabs And Scene」一鍵生成。
    /// 要客製組裝邏輯（Override 服務、註冊演出與自訂指令）時，建議把本檔複製到專案改名後修改，
    /// 不要直接改包內版本。
    /// </summary>
    public class DefaultGameLauncher : ParameterRuntimeSource
    {
        private const string MAIN_MENU_VIEW_PATH = "GameFlowUIViews/MainMenuView";
        private const string GAMEPLAY_HUD_VIEW_PATH = "GameFlowUIViews/GameplayHudView";
        private const string ACTION_MENU_VIEW_PATH = "GameFlowUIViews/ActionMenuView";
        private const string LOCATION_MENU_VIEW_PATH = "GameFlowUIViews/LocationMenuView";
        private const string HINT_POPUP_VIEW_PATH = "GameFlowUIViews/HintPopupView";
        private const string CREDITS_VIEW_PATH = "GameFlowUIViews/CreditsView";
        private static readonly string[] HUD_PARAMETER_KEYS = { "Supplies", "Satiety", "Spirit" };

        [SerializeField] private UserInterfaceController uiController;
        [SerializeField] private DialogueView dialogueView;
        [Tooltip("行動選單、提示視窗等覆蓋層 View 的父節點。")]
        [SerializeField] private RectTransform overlayRoot;
        [Header("GameFlow Tables")]
        [SerializeField] private TextAsset timePhaseData;
        [SerializeField] private TextAsset playerActionData;
        [SerializeField] private TextAsset locationData;
        [SerializeField] private TextAsset gameTextData;
        [SerializeField] private TextAsset dialogueData;
        [Header("Game Events")]
        [SerializeField] private GameEventCatalogAsset gameEventCatalog;
        [SerializeField] private SceneGameEventTrigger[] sceneGameEventTriggers;
        [SerializeField] private SceneGameEventTrigger2D[] sceneGameEventTriggers2D;
        [SerializeField] private ParameterStateBinder[] parameterStateBinders;
        [SerializeField] private string gameTitle = "My Game";
        [Tooltip("製作人員名單文字（GameTextData 表的 ID）。")]
        [SerializeField] private int creditsTextId = 950;

        private GameStaticDataManager staticDataManager;
        private ParameterStore parameters;
        private IReadOnlyList<ParameterDefinition> parameterDefinitions;
        private GameFlowServices services;
        private ActionMenuPresenter actionMenuPresenter;
        private LocationMenuPresenter locationMenuPresenter;
        private HintPresenter hintPresenter;
        private GameplayHudPresenter hudPresenter;
        private CancellationTokenSource flowCts;
        private bool isGameRunning;
        private GameEventRunner gameEventRunner;

        private void Awake()
        {
            // 視窗失焦時仍持續運作（轉場與演出皆以時間驅動，暫停會卡住流程）。
            Application.runInBackground = true;

            LoadStaticData();
            MessageBus.Subscribe<ReturnToTitleRequestedEvent>(OnReturnToTitleRequested);
        }

        private void Start()
        {
            ShowMainMenuAsync().Forget();
        }

        private void OnDestroy()
        {
            MessageBus.Unsubscribe<ReturnToTitleRequestedEvent>(OnReturnToTitleRequested);
            CancelFlow();
            hudPresenter?.Dispose();
        }

        private void LoadStaticData()
        {
            ValidateStaticData();
            staticDataManager = new GameStaticDataManager();
            IGameStaticDataHandler handler = new TextAssetJsonStaticDataHandler(
                new[]
                {
                    timePhaseData,
                    playerActionData,
                    locationData,
                    gameTextData,
                    dialogueData
                });
            GameFlowSystemBuilder.LoadDefaultTables(staticDataManager, handler);
            staticDataManager.Add<DialogueData>(handler);

            ParameterTableJsonCodec codec = new ParameterTableJsonCodec();
            parameterDefinitions = gameEventCatalog.ParameterTables
                .Select(tableAsset => codec.Read(tableAsset.text))
                .SelectMany(table => table.Definitions)
                .ToList();
            parameters = new ParameterStore(parameterDefinitions);
            Initialize(parameters);
        }

        private async UniTaskVoid ShowMainMenuAsync()
        {
            await uiController.ClearViewStack();
            MainMenuView mainMenu = await uiController.PushView<MainMenuView>(
                MAIN_MENU_VIEW_PATH,
                view => view.SetTitle(gameTitle));
            mainMenu.OnStartRequested += () => StartGameAsync().Forget();
        }

        private async UniTaskVoid StartGameAsync()
        {
            if (isGameRunning)
            {
                return;
            }
            isGameRunning = true;

            EnsureServicesBuilt();

            await uiController.ClearViewStack();
            GameplayHudView hudView = await uiController.PushView<GameplayHudView>(GAMEPLAY_HUD_VIEW_PATH);

            hudPresenter?.Dispose();
            IReadOnlyList<ParameterDefinition> hudDefinitions = HUD_PARAMETER_KEYS
                .Select(key => parameterDefinitions.Single(definition => definition.Key == key))
                .ToList();
            hudPresenter = new GameplayHudPresenter(hudView, services.Parameters, hudDefinitions, services.TimeService);

            // 開新局：Parameters、Phase、Location 各由自己的 owner 重置。
            services.ResetForNewGame();
            hudPresenter.Refresh();

            flowCts = new CancellationTokenSource();
            InitializeSceneGameEventTriggers(flowCts.Token);
            services.FlowController.RunNewGameAsync(flowCts.Token).Forget();
        }

        private void EnsureServicesBuilt()
        {
            if (services != null)
            {
                return;
            }

            actionMenuPresenter = new ActionMenuPresenter(InstantiateOverlayView<ActionMenuView>(ACTION_MENU_VIEW_PATH));
            locationMenuPresenter = new LocationMenuPresenter(InstantiateOverlayView<LocationMenuView>(LOCATION_MENU_VIEW_PATH));
            hintPresenter = new HintPresenter(InstantiateOverlayView<HintPopupView>(HINT_POPUP_VIEW_PATH));

            GameEventDocumentJsonCodec gameEventCodec = new GameEventDocumentJsonCodec();
            GameEventCatalog runtimeGameEventCatalog = new GameEventCatalog(
                gameEventCatalog,
                gameEventCodec);

            // 全部採用預設實作；Game Events 由可選 integration assembly 接入。
            services = new GameFlowSystemBuilder(staticDataManager, parameters)
                .WithDialoguePlayerFactory(cmdExec => new DialoguePlayer(dialogueView, staticDataManager, cmdExec))
                .WithActionMenuPresenter(actionMenuPresenter)
                .WithHintPresenter(hintPresenter)
                .WithLocationMenuPresenter(locationMenuPresenter)
                .WithEventTriggerFactory(effectRuntime =>
                {
                    gameEventRunner = new GameEventRunner(
                        runtimeGameEventCatalog,
                        effectRuntime,
                        parameters,
                        gameEventCodec);
                    return new GameFlowGameEventAdapter(gameEventRunner);
                })
                .Build();
            ParameterStateBinder[] binders = parameterStateBinders ?? Array.Empty<ParameterStateBinder>();
            for (int index = 0; index < binders.Length; index++)
            {
                ParameterStateBinder binder = binders[index];
                if (binder == null)
                {
                    throw new InvalidOperationException(
                        $"[DefaultGameLauncher] parameterStateBinders[{index}] is missing.");
                }

                binder.Initialize(services.Parameters);
            }

            RegisterPerformances();
        }

        private void ValidateStaticData()
        {
            TextAsset[] tables =
            {
                timePhaseData,
                playerActionData,
                locationData,
                gameTextData,
                dialogueData
            };
            string[] names =
            {
                "TimePhaseData",
                "PlayerActionData",
                "LocationData",
                "GameTextData",
                "DialogueData"
            };
            for (int index = 0; index < tables.Length; index++)
            {
                if (tables[index] == null)
                    throw new InvalidOperationException(
                        $"[DefaultGameLauncher] {names[index]} is required.");
                if (!string.Equals(tables[index].name, names[index], StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"[DefaultGameLauncher] {names[index]} must reference a TextAsset named '{names[index]}'.");
            }
            if (gameEventCatalog == null)
                throw new InvalidOperationException(
                    "[DefaultGameLauncher] Game Event Catalog is required.");
            if (gameEventCatalog.ParameterTables.Count == 0)
                throw new InvalidOperationException(
                    "[DefaultGameLauncher] Game Event Catalog needs a Parameter Table.");
            for (int index = 0; index < gameEventCatalog.ParameterTables.Count; index++)
            {
                if (gameEventCatalog.ParameterTables[index] == null)
                    throw new InvalidOperationException(
                        $"[DefaultGameLauncher] Parameter Table row {index + 1} is missing.");
            }
        }

        private void InitializeSceneGameEventTriggers(CancellationToken cancellationToken)
        {
            EventContext eventContext = new EventContext(cancellationToken);
            SceneGameEventTrigger[] triggers =
                sceneGameEventTriggers ?? Array.Empty<SceneGameEventTrigger>();
            for (int index = 0; index < triggers.Length; index++)
            {
                SceneGameEventTrigger trigger = triggers[index];
                if (trigger == null)
                {
                    throw new InvalidOperationException(
                        $"[DefaultGameLauncher] sceneGameEventTriggers[{index}] is missing.");
                }

                trigger.Initialize(gameEventRunner, eventContext);
            }

            SceneGameEventTrigger2D[] triggers2D =
                sceneGameEventTriggers2D ?? Array.Empty<SceneGameEventTrigger2D>();
            for (int index = 0; index < triggers2D.Length; index++)
            {
                SceneGameEventTrigger2D trigger = triggers2D[index];
                if (trigger == null)
                {
                    throw new InvalidOperationException(
                        $"[DefaultGameLauncher] sceneGameEventTriggers2D[{index}] is missing.");
                }

                trigger.Initialize(gameEventRunner, eventContext);
            }
        }

        /// <summary>
        /// 演出註冊處：表格引用的演出 ID 在這裡註冊；未註冊的 ID 以佔位 Log 代替，不卡流程。
        /// </summary>
        private void RegisterPerformances()
        {
            CreditsView creditsView = InstantiateOverlayView<CreditsView>(CREDITS_VIEW_PATH);
            services.PerformancePlayer.Register("Credits", new CreditsPerformance(creditsView, services.TextProvider, creditsTextId));
        }

        private T InstantiateOverlayView<T>(string resourcePath) where T : AView
        {
            T prefab = Resources.Load<T>(resourcePath);
            if (prefab == null)
            {
                Debug.LogError($"[DefaultGameLauncher] 找不到 View prefab：Resources/{resourcePath}，請先執行選單 KahaGameCore → GameFlowSystem → Build Default UI Prefabs And Scene。");
                return null;
            }

            T view = Instantiate(prefab, overlayRoot);
            view.gameObject.SetActive(false);
            return view;
        }

        private void OnReturnToTitleRequested(ReturnToTitleRequestedEvent requestedEvent)
        {
            CancelFlow();

            // 兩件事缺一不可：取消 token 停掉流程迴圈與事件佇列；
            // CancelPending 讓停在選單上的 await 以 null 收場。
            actionMenuPresenter.CancelPending();
            locationMenuPresenter.CancelPending();
            hintPresenter.CancelPending();

            hudPresenter?.Dispose();
            hudPresenter = null;

            dialogueView.gameObject.SetActive(false);
            isGameRunning = false;

            ShowMainMenuAsync().Forget();
        }

        private void CancelFlow()
        {
            if (flowCts != null)
            {
                flowCts.Cancel();
                flowCts.Dispose();
                flowCts = null;
            }
        }
    }
}
