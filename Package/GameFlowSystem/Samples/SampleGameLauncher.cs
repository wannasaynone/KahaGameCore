using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.GameData.Implemented;
using KahaGameCore.GameEvent;
using KahaGameCore.Package.GameFlowSystem;
using KahaGameCore.UserInterfaceSystem;
using ProjectBSR.DialogueSystem;
using ProjectBSR.DialogueSystem.View;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Data;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.DataAccess;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Events;
using KahaGameCore.Package.GameFlowSystem.Samples.Presenters;
using KahaGameCore.Package.GameFlowSystem.Samples.Views;
using UnityEngine;

namespace KahaGameCore.Package.GameFlowSystem.Samples
{
    /// <summary>
    /// 【範例】組裝根（Composition Root）：載入表格資料、組裝所有服務與 Presenter、控制
    /// 「主標題 ↔ 遊戲流程」的切換。所有相依關係只在這裡建立。
    ///
    /// Samples 資料夾是「複製進專案再改」的範本，不要直接讓遊戲程式碼依賴本 assembly：
    ///   1. 把整個 Samples 資料夾內容複製到你的專案，改成自己的命名空間。
    ///   2. View prefab 需自行建立（本範例以 Resources.Load 依路徑載入，路徑見上方常數）；
    ///      TMP 預設字型無 CJK，中文需自建 TMP Font Asset。
    ///   3. DialogueView 來自 KahaGameCore DialogueSystem 包（類別本體不在本包），場景中放入其 prefab
    ///      後以 Inspector 拖入。注意：DialogueView 以 1080p 設計，高解析度專案需用一層
    ///      固定 1920x1080、scale 放大的節點包覆；其 Update() 使用舊版 Input，
    ///      Active Input Handling 需設為 Both（改完要重啟編輯器）。
    ///   4. 表格放在 Resources/GameData/{類別名}.txt（JSON 陣列）。
    /// </summary>
    public class SampleGameLauncher : MonoBehaviour
    {
        // Sample prefab 放在包內 Samples/Resources/SampleUIViews（路徑刻意與專案的 UIViews 區隔，避免 Resources 路徑衝突）。
        private const string MAIN_MENU_VIEW_PATH = "SampleUIViews/MainMenuView";
        private const string GAMEPLAY_HUD_VIEW_PATH = "SampleUIViews/GameplayHudView";
        private const string ACTION_MENU_VIEW_PATH = "SampleUIViews/ActionMenuView";
        private const string LOCATION_MENU_VIEW_PATH = "SampleUIViews/LocationMenuView";
        private const string HINT_POPUP_VIEW_PATH = "SampleUIViews/HintPopupView";
        private const string CREDITS_VIEW_PATH = "SampleUIViews/CreditsView";

        [SerializeField] private UserInterfaceController uiController;
        [SerializeField] private DialogueView dialogueView;
        [Tooltip("行動選單、提示視窗等覆蓋層 View 的父節點。")]
        [SerializeField] private RectTransform overlayRoot;
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
            UnityEngine.Application.runInBackground = true;

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
            GameFlowSystemBuilder.LoadDefaultTables(staticDataManager);
            staticDataManager.Add<DialogueData>(new ResourcesJsonStaticDataHandler());
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

            // 全部採用 GameFlowSystem 的預設實作；有專案特殊需求時改用 Override 系列方法傳入。
            services = new GameFlowSystemBuilder(staticDataManager)
                .WithDialogueView(dialogueView)
                .WithActionMenuPresenter(actionMenuPresenter)
                .WithHintPresenter(hintPresenter)
                .WithLocationMenuPresenter(locationMenuPresenter)
                .Build();

            RegisterPerformances();
        }

        /// <summary>
        /// 演出註冊處：之後實作好的 UGUI 演出（換日、丈夫外出、物資增加……）都在這裡
        /// 以表格使用的演出 ID 註冊。未註冊的 ID 會以佔位 Log 代替，不會卡住流程。
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
                Debug.LogError($"[SampleGameLauncher] 找不到 View prefab：Resources/{resourcePath}");
                return null;
            }

            T view = Instantiate(prefab, overlayRoot);
            view.gameObject.SetActive(false);
            return view;
        }

        private void OnReturnToTitleRequested(ReturnToTitleRequestedEvent requestedEvent)
        {
            CancelFlow();

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
