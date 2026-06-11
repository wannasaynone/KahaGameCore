using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.GameData;
using KahaGameCore.GameData.Implemented;
using KahaGameCore.GameEvent;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Data;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.DataAccess;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Events;
using KahaGameCore.UserInterfaceSystem;
using ProjectBSR.DialogueSystem;
using ProjectBSR.DialogueSystem.View;
using UnityEngine;

namespace KahaGameCore.Package.GameFlowSystem.DefaultViews
{
    /// <summary>
    /// 預設組裝根（Composition Root）：載入表格、組裝所有服務與預設 UI，控制「主標題 ↔ 遊戲流程」切換。
    /// 場景與 View prefabs 由選單「KahaGameCore → GameFlowSystem → Build Default UI Prefabs And Scene」一鍵生成。
    /// 要客製組裝邏輯（Override 服務、註冊演出與自訂指令）時，建議把本檔複製到專案改名後修改，
    /// 不要直接改包內版本。
    /// </summary>
    public class DefaultGameLauncher : MonoBehaviour
    {
        private const string MAIN_MENU_VIEW_PATH = "GameFlowUIViews/MainMenuView";
        private const string GAMEPLAY_HUD_VIEW_PATH = "GameFlowUIViews/GameplayHudView";
        private const string ACTION_MENU_VIEW_PATH = "GameFlowUIViews/ActionMenuView";
        private const string LOCATION_MENU_VIEW_PATH = "GameFlowUIViews/LocationMenuView";
        private const string HINT_POPUP_VIEW_PATH = "GameFlowUIViews/HintPopupView";
        private const string CREDITS_VIEW_PATH = "GameFlowUIViews/CreditsView";

        [SerializeField] private UserInterfaceController uiController;
        [SerializeField] private DialogueView dialogueView;
        [Tooltip("行動選單、提示視窗等覆蓋層 View 的父節點。")]
        [SerializeField] private RectTransform overlayRoot;
        [Tooltip("七張表的 JSON TextAsset（檔名需與資料型別名稱一致，如 TimePhaseData.txt）。留空時改從 Resources/GameData/{型別名}.txt 載入。")]
        [SerializeField] private TextAsset[] gameDataTables;
        [SerializeField] private string gameTitle = "My Game";
        [Tooltip("製作人員名單文字（GameTextData 表的 ID）。")]
        [SerializeField] private int creditsTextId = 950;

        private GameStaticDataManager staticDataManager;
        private GameFlowServices services;
        private ActionMenuPresenter actionMenuPresenter;
        private LocationMenuPresenter locationMenuPresenter;
        private HintPresenter hintPresenter;
        private GameplayHudPresenter hudPresenter;
        private CancellationTokenSource flowCts;
        private bool isGameRunning;

        private void Awake()
        {
            // 視窗失焦時仍持續運作（轉場與演出皆以時間驅動，暫停會卡住流程）。
            Application.runInBackground = true;

            LoadStaticData();
            EventBus.Subscribe<ReturnToTitleRequestedEvent>(OnReturnToTitleRequested);
        }

        private void Start()
        {
            ShowMainMenuAsync().Forget();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ReturnToTitleRequestedEvent>(OnReturnToTitleRequested);
            CancelFlow();
            hudPresenter?.Dispose();
        }

        private void LoadStaticData()
        {
            staticDataManager = new GameStaticDataManager();
            IGameStaticDataHandler handler = gameDataTables != null && gameDataTables.Length > 0
                ? new TextAssetJsonStaticDataHandler(gameDataTables)
                : new ResourcesJsonStaticDataHandler();
            GameFlowSystemBuilder.LoadDefaultTables(staticDataManager, handler);
            staticDataManager.Add<DialogueData>(handler);
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
            hudPresenter = new GameplayHudPresenter(hudView, staticDataManager, services.GameState, services.TimeService);
            hudPresenter.Refresh();

            flowCts = new CancellationTokenSource();
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

            // 全部採用預設實作；有專案特殊需求時改用 Override 系列方法傳入。
            services = new GameFlowSystemBuilder(staticDataManager)
                .WithDialogueView(dialogueView)
                .WithActionMenuPresenter(actionMenuPresenter)
                .WithHintPresenter(hintPresenter)
                .WithLocationMenuPresenter(locationMenuPresenter)
                .Build();

            RegisterPerformances();
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
