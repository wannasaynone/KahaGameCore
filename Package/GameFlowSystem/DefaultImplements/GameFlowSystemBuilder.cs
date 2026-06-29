using System;
using KahaGameCore.GameData.Implemented;
using KahaGameCore.Package.EffectProcessor;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.DataAccess;
using ProjectBSR.DialogueSystem.View;
using UnityEngine;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    /// <summary>Build 完成後的服務群組。跨次遊玩共用（開新遊戲時由 GameState.ResetToInitial 重置狀態）。</summary>
    public class GameFlowServices
    {
        public IGameState GameState { get; internal set; }
        public IConditionEvaluator ConditionEvaluator { get; internal set; }
        public ITimeService TimeService { get; internal set; }
        public ILocationService LocationService { get; internal set; }
        public IPlayerActionProvider ActionProvider { get; internal set; }
        public IGameTextProvider TextProvider { get; internal set; }
        public IPerformancePlayer PerformancePlayer { get; internal set; }
        public ICommandExecutor CommandExecutor { get; internal set; }
        public IDialoguePlayer DialoguePlayer { get; internal set; }
        public IGameEventTriggerService TriggerService { get; internal set; }
        public GameFlowController FlowController { get; internal set; }
        /// <summary>效果指令工廠。Build 之後仍可追加註冊專案自訂指令。</summary>
        public EffectCommandFactoryContainer FactoryContainer { get; internal set; }
    }

    /// <summary>
    /// 預設實作的組裝器：照表驅動慣例把所有服務接好並回傳 GameFlowServices。
    /// 每個服務都有預設實作；新專案有新需求時以 Override 系列方法傳入自己的實作即可。
    /// UI 層（行動選單、提示、移動選單）與 DialogueView 屬於各專案的演出資產，必須由外部提供。
    /// </summary>
    public class GameFlowSystemBuilder
    {
        private readonly GameStaticDataManager staticDataManager;

        // 必要外部件（UI 層）
        private DialogueView dialogueView;
        private IActionMenuPresenter actionMenuPresenter;
        private IHintPresenter hintPresenter;
        private ILocationMenuPresenter locationMenuPresenter;

        // 可覆寫的預設實作
        private IGameState gameState;
        private IConditionEvaluator conditionEvaluator;
        private ITimeService timeService;
        private ILocationService locationService;
        private IPlayerActionProvider actionProvider;
        private IGameTextProvider textProvider;
        private IPerformancePlayer performancePlayer;
        private ICommandExecutor commandExecutor;
        private IDialoguePlayer dialoguePlayer;
        private IGameEventTriggerService triggerService;

        private Action<EffectCommandFactoryContainer> extraCommandRegistration;

        /// <param name="staticDataManager">已載入所有表格的資料管理器（可用 LoadDefaultTables 載入預設表）。</param>
        public GameFlowSystemBuilder(GameStaticDataManager staticDataManager)
        {
            this.staticDataManager = staticDataManager ?? throw new ArgumentNullException(nameof(staticDataManager));
        }

        /// <summary>
        /// 把預設六張表（TimePhaseData、PlayerActionData、LocationData、GameEventTriggerData、
        /// GameValueData、GameTextData）從 Resources/GameData/{類別名}.txt 載入。
        /// DialogueData 等其他表請專案自行 Add。
        /// </summary>
        public static void LoadDefaultTables(GameStaticDataManager staticDataManager)
        {
            LoadDefaultTables(staticDataManager, new ResourcesJsonStaticDataHandler());
        }

        /// <summary>
        /// 以指定的 handler 載入預設六張表（例如 TextAssetJsonStaticDataHandler 手動指定、
        /// 或自行實作的線上下載 / Addressables handler）。
        /// </summary>
        public static void LoadDefaultTables(GameStaticDataManager staticDataManager, KahaGameCore.GameData.IGameStaticDataHandler handler)
        {
            staticDataManager.Add<Data.TimePhaseData>(handler);
            staticDataManager.Add<Data.PlayerActionData>(handler);
            staticDataManager.Add<Data.LocationData>(handler);
            staticDataManager.Add<Data.GameEventTriggerData>(handler);
            staticDataManager.Add<Data.GameValueData>(handler);
            staticDataManager.Add<Data.GameTextData>(handler);
        }

        // ───────── 必要外部件（UI 層）─────────

        /// <summary>劇情對話 View（KahaGameCore DialogueSystem）。若以 OverrideDialoguePlayer 自管對話則可不提供。</summary>
        public GameFlowSystemBuilder WithDialogueView(DialogueView view) { dialogueView = view; return this; }
        /// <summary>行動選單（必要）。</summary>
        public GameFlowSystemBuilder WithActionMenuPresenter(IActionMenuPresenter presenter) { actionMenuPresenter = presenter; return this; }
        /// <summary>提示視窗。未提供時表格使用 ShowHint 指令會在執行期出錯。</summary>
        public GameFlowSystemBuilder WithHintPresenter(IHintPresenter presenter) { hintPresenter = presenter; return this; }
        /// <summary>移動選單。未提供時表格使用 OpenLocationMenu 指令會在執行期出錯。</summary>
        public GameFlowSystemBuilder WithLocationMenuPresenter(ILocationMenuPresenter presenter) { locationMenuPresenter = presenter; return this; }

        // ───────── 覆寫預設實作（有新需求再傳入）─────────

        public GameFlowSystemBuilder OverrideGameState(IGameState custom) { gameState = custom; return this; }
        public GameFlowSystemBuilder OverrideConditionEvaluator(IConditionEvaluator custom) { conditionEvaluator = custom; return this; }
        public GameFlowSystemBuilder OverrideTimeService(ITimeService custom) { timeService = custom; return this; }
        public GameFlowSystemBuilder OverrideLocationService(ILocationService custom) { locationService = custom; return this; }
        public GameFlowSystemBuilder OverrideActionProvider(IPlayerActionProvider custom) { actionProvider = custom; return this; }
        public GameFlowSystemBuilder OverrideTextProvider(IGameTextProvider custom) { textProvider = custom; return this; }
        public GameFlowSystemBuilder OverridePerformancePlayer(IPerformancePlayer custom) { performancePlayer = custom; return this; }
        public GameFlowSystemBuilder OverrideCommandExecutor(ICommandExecutor custom) { commandExecutor = custom; return this; }
        public GameFlowSystemBuilder OverrideDialoguePlayer(IDialoguePlayer custom) { dialoguePlayer = custom; return this; }
        public GameFlowSystemBuilder OverrideTriggerService(IGameEventTriggerService custom) { triggerService = custom; return this; }

        /// <summary>在內建指令之外追加專案自訂的效果指令。</summary>
        public GameFlowSystemBuilder AddCommandRegistration(Action<EffectCommandFactoryContainer> register)
        {
            extraCommandRegistration += register;
            return this;
        }

        public GameFlowServices Build()
        {
            if (actionMenuPresenter == null)
            {
                throw new InvalidOperationException("[GameFlowSystemBuilder] 必須以 WithActionMenuPresenter 提供行動選單實作。");
            }
            if (dialogueView == null && dialoguePlayer == null)
            {
                throw new InvalidOperationException("[GameFlowSystemBuilder] 必須以 WithDialogueView 提供對話 View，或以 OverrideDialoguePlayer 提供自訂對話播放器。");
            }
            if (hintPresenter == null)
            {
                Debug.LogWarning("[GameFlowSystemBuilder] 未提供 IHintPresenter，表格使用 ShowHint 指令時會在執行期出錯。");
            }
            if (locationMenuPresenter == null)
            {
                Debug.LogWarning("[GameFlowSystemBuilder] 未提供 ILocationMenuPresenter，表格使用 OpenLocationMenu 指令時會在執行期出錯。");
            }

            GameFlowServices services = new GameFlowServices();
            services.GameState = gameState ?? new GameState(staticDataManager);
            services.ConditionEvaluator = conditionEvaluator ?? new FormulaConditionEvaluator(services.GameState);
            services.TimeService = timeService ?? new TimeService(staticDataManager, services.GameState);
            services.LocationService = locationService ?? new LocationService(staticDataManager, services.GameState, services.ConditionEvaluator);
            services.ActionProvider = actionProvider ?? new PlayerActionProvider(staticDataManager, services.ConditionEvaluator);
            services.TextProvider = textProvider ?? new GameTextProvider(staticDataManager, services.ConditionEvaluator);
            services.PerformancePlayer = performancePlayer ?? new PerformanceRegistry();

            services.FactoryContainer = new EffectCommandFactoryContainer();
            services.CommandExecutor = commandExecutor ?? new EffectCommandExecutor(services.FactoryContainer, services.GameState);
            services.DialoguePlayer = dialoguePlayer ?? new DialoguePlayer(dialogueView, staticDataManager, services.CommandExecutor);

            EffectCommandRegistrar.RegisterAll(
                services.FactoryContainer,
                services.GameState,
                services.TimeService,
                services.LocationService,
                services.DialoguePlayer,
                services.PerformancePlayer,
                services.TextProvider,
                hintPresenter,
                locationMenuPresenter);
            extraCommandRegistration?.Invoke(services.FactoryContainer);

            services.TriggerService = triggerService ?? new GameEventTriggerService(
                staticDataManager,
                services.GameState,
                services.ConditionEvaluator,
                services.DialoguePlayer,
                services.PerformancePlayer,
                services.CommandExecutor);

            services.FlowController = new GameFlowController(
                services.TimeService,
                services.LocationService,
                services.ActionProvider,
                services.TriggerService,
                services.CommandExecutor,
                actionMenuPresenter);

            return services;
        }
    }
}
