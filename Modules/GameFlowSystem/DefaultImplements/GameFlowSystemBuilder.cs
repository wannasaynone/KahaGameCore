using System;
using KahaGameCore.StaticData;
using KahaGameCore.Effects;
using KahaGameCore.Parameters;
using KahaGameCore.GameFlowSystem.DefaultImplements.DataAccess;
using UnityEngine;

namespace KahaGameCore.GameFlowSystem.DefaultImplements
{
    /// <summary>Build 完成後的服務群組。跨次遊玩共用，開新遊戲時呼叫 ResetForNewGame。</summary>
    public class GameFlowServices
    {
        public ParameterStore Parameters { get; internal set; }
        public IConditionEvaluator ConditionEvaluator { get; internal set; }
        public ITimeService TimeService { get; internal set; }
        public ILocationService LocationService { get; internal set; }
        public IPlayerActionProvider ActionProvider { get; internal set; }
        public IGameTextProvider TextProvider { get; internal set; }
        public IPerformancePlayer PerformancePlayer { get; internal set; }
        public ICommandExecutor CommandExecutor { get; internal set; }
        public IDialoguePlayer DialoguePlayer { get; internal set; }
        public IGameFlowEventTriggerService TriggerService { get; internal set; }
        public GameFlowController FlowController { get; internal set; }
        /// <summary>效果指令定義。所有 GameFlow 與 Game Event 執行共用此 registry。</summary>
        public EffectCommandRegistry CommandRegistry { get; internal set; }
        public EffectRuntime EffectRuntime { get; internal set; }

        public void ResetForNewGame()
        {
            Parameters.ResetToInitial();
            TimeService.ResetToFirstPhase();
            LocationService.ResetToInitial();
        }
    }

    /// <summary>
    /// 預設實作的組裝器：照表驅動慣例把所有服務接好並回傳 GameFlowServices。
    /// 每個服務都有預設實作；新專案有新需求時以 Override 系列方法傳入自己的實作即可。
    /// UI 層（行動選單、提示、移動選單）屬於各專案的演出資產，必須由外部提供；
    /// 對話播放器（IDialoguePlayer）亦由外部以 WithDialoguePlayerFactory 注入，本套件不相依具體對話系統。
    /// </summary>
    public class GameFlowSystemBuilder
    {
        private readonly GameStaticDataManager staticDataManager;
        private readonly ParameterStore parameters;

        // 必要外部件（UI 層）
        private IActionMenuPresenter actionMenuPresenter;
        private IHintPresenter hintPresenter;
        private ILocationMenuPresenter locationMenuPresenter;

        // 可覆寫的預設實作
        private IConditionEvaluator conditionEvaluator;
        private ITimeService timeService;
        private ILocationService locationService;
        private IPlayerActionProvider actionProvider;
        private IGameTextProvider textProvider;
        private IPerformancePlayer performancePlayer;
        private ICommandExecutor commandExecutor;
        private IDialoguePlayer dialoguePlayer;
        private Func<EffectRuntime, IGameFlowEventTriggerService> eventTriggerFactory;

        private EffectCommandConfiguration commandConfiguration;
        private Func<ICommandExecutor, IDialoguePlayer> dialoguePlayerFactory;
        private Action<EffectCommandServiceRegistry> configureEffectCommandServices;

        /// <param name="staticDataManager">已載入所有表格的資料管理器（可用 LoadDefaultTables 載入預設表）。</param>
        public GameFlowSystemBuilder(GameStaticDataManager staticDataManager, ParameterStore parameters)
        {
            this.staticDataManager = staticDataManager ?? throw new ArgumentNullException(nameof(staticDataManager));
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        /// <summary>
        /// 把預設四張表（TimePhaseData、PlayerActionData、LocationData、GameTextData）
        /// 從 Resources/GameData/{類別名}.txt 載入。
        /// DialogueData 等其他表請專案自行 Add。
        /// </summary>
        public static void LoadDefaultTables(GameStaticDataManager staticDataManager)
        {
            LoadDefaultTables(staticDataManager, new ResourcesJsonStaticDataHandler());
        }

        /// <summary>
        /// 以指定的 handler 載入預設四張表（例如 TextAssetJsonStaticDataHandler 手動指定、
        /// 或自行實作的線上下載 / Addressables handler）。
        /// </summary>
        public static void LoadDefaultTables(GameStaticDataManager staticDataManager, KahaGameCore.StaticData.IGameStaticDataHandler handler)
        {
            staticDataManager.Add<Data.TimePhaseData>(handler);
            staticDataManager.Add<Data.PlayerActionData>(handler);
            staticDataManager.Add<Data.LocationData>(handler);
            staticDataManager.Add<Data.GameTextData>(handler);
        }

        // ───────── 必要外部件（UI 層）─────────

        /// <summary>
        /// 對話播放器工廠（必要，除非以 OverrideDialoguePlayer 直接提供）。
        /// 本套件不認得具體對話系統，對話播放器由外部（各專案／DefaultViews 範例的對話橋接）建構。
        /// 工廠會收到組裝完成的 ICommandExecutor，方便對話橋接指令（如 GameEffect）重用同一個執行器。
        /// </summary>
        public GameFlowSystemBuilder WithDialoguePlayerFactory(Func<ICommandExecutor, IDialoguePlayer> factory) { dialoguePlayerFactory = factory; return this; }
        /// <summary>行動選單（必要）。</summary>
        public GameFlowSystemBuilder WithActionMenuPresenter(IActionMenuPresenter presenter) { actionMenuPresenter = presenter; return this; }
        /// <summary>提示視窗。未提供時表格使用 ShowHint 指令會在執行期出錯。</summary>
        public GameFlowSystemBuilder WithHintPresenter(IHintPresenter presenter) { hintPresenter = presenter; return this; }
        /// <summary>移動選單。未提供時表格使用 OpenLocationMenu 指令會在執行期出錯。</summary>
        public GameFlowSystemBuilder WithLocationMenuPresenter(ILocationMenuPresenter presenter) { locationMenuPresenter = presenter; return this; }
        /// <summary>
        /// 建立 Timing trigger adapter。工廠收到本次組裝唯一的 EffectRuntime，
        /// 讓 Game Events 與 GameFlow 共用同一個 command registry／runtime。
        /// </summary>
        public GameFlowSystemBuilder WithEventTriggerFactory(
            Func<EffectRuntime, IGameFlowEventTriggerService> factory)
        {
            eventTriggerFactory = factory;
            return this;
        }

        /// <summary>指定由編輯器 Catalog 儲存的效果指令選擇。</summary>
        public GameFlowSystemBuilder WithEffectCommandConfiguration(
            EffectCommandConfiguration configuration)
        {
            commandConfiguration = configuration ??
                throw new ArgumentNullException(nameof(configuration));
            return this;
        }

        /// <summary>
        /// Adds command services owned by an outer composition root before enabled
        /// command modules are created.
        /// </summary>
        public GameFlowSystemBuilder WithEffectCommandServices(
            Action<EffectCommandServiceRegistry> configure)
        {
            configureEffectCommandServices += configure ??
                throw new ArgumentNullException(nameof(configure));
            return this;
        }

        // ───────── 覆寫預設實作（有新需求再傳入）─────────

        public GameFlowSystemBuilder OverrideConditionEvaluator(IConditionEvaluator custom) { conditionEvaluator = custom; return this; }
        public GameFlowSystemBuilder OverrideTimeService(ITimeService custom) { timeService = custom; return this; }
        public GameFlowSystemBuilder OverrideLocationService(ILocationService custom) { locationService = custom; return this; }
        public GameFlowSystemBuilder OverrideActionProvider(IPlayerActionProvider custom) { actionProvider = custom; return this; }
        public GameFlowSystemBuilder OverrideTextProvider(IGameTextProvider custom) { textProvider = custom; return this; }
        public GameFlowSystemBuilder OverridePerformancePlayer(IPerformancePlayer custom) { performancePlayer = custom; return this; }
        public GameFlowSystemBuilder OverrideCommandExecutor(ICommandExecutor custom) { commandExecutor = custom; return this; }
        public GameFlowSystemBuilder OverrideDialoguePlayer(IDialoguePlayer custom) { dialoguePlayer = custom; return this; }

        public GameFlowServices Build()
        {
            if (actionMenuPresenter == null)
            {
                throw new InvalidOperationException("[GameFlowSystemBuilder] 必須以 WithActionMenuPresenter 提供行動選單實作。");
            }
            if (dialoguePlayer == null && dialoguePlayerFactory == null)
            {
                throw new InvalidOperationException("[GameFlowSystemBuilder] 必須以 WithDialoguePlayerFactory 提供對話播放器工廠，或以 OverrideDialoguePlayer 提供自訂對話播放器。");
            }
            if (eventTriggerFactory == null)
            {
                throw new InvalidOperationException(
                    "[GameFlowSystemBuilder] 必須以 WithEventTriggerFactory 提供 Game Event timing adapter。");
            }
            if (commandConfiguration == null)
            {
                throw new InvalidOperationException(
                    "[GameFlowSystemBuilder] 必須以 WithEffectCommandConfiguration 提供效果指令設定。");
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
            services.Parameters = parameters;
            GameFlowExpressions gameFlowExpressions = new GameFlowExpressions(services.Parameters);
            services.ConditionEvaluator = conditionEvaluator ?? gameFlowExpressions;
            services.TimeService = timeService ?? new TimeService(staticDataManager, services.Parameters);
            services.LocationService = locationService ?? new LocationService(staticDataManager, services.ConditionEvaluator);
            services.ActionProvider = actionProvider ?? new PlayerActionProvider(staticDataManager, services.ConditionEvaluator);
            services.TextProvider = textProvider ?? new GameTextProvider(staticDataManager, services.ConditionEvaluator);
            services.PerformancePlayer = performancePlayer ?? new PerformanceRegistry();

            services.CommandRegistry = new EffectCommandRegistry();
            services.EffectRuntime = new EffectRuntime(services.CommandRegistry);
            services.CommandExecutor = commandExecutor ?? new EffectCommandExecutor(services.EffectRuntime);
            services.DialoguePlayer = dialoguePlayer ?? dialoguePlayerFactory(services.CommandExecutor);

            EffectCommandServiceRegistry commandServices =
                new EffectCommandServiceRegistry()
                    .Add(services.Parameters)
                    .Add(new GameFlowEffectCommandServices(
                        gameFlowExpressions,
                        services.TimeService,
                        services.LocationService,
                        services.DialoguePlayer,
                        services.PerformancePlayer,
                        services.TextProvider,
                        hintPresenter,
                        locationMenuPresenter));
            configureEffectCommandServices?.Invoke(commandServices);
            EffectCommandBootstrapper.Populate(
                services.CommandRegistry,
                commandConfiguration,
                commandServices);

            services.TriggerService = eventTriggerFactory(services.EffectRuntime)
                ?? throw new InvalidOperationException(
                    "[GameFlowSystemBuilder] WithEventTriggerFactory 不可回傳 null。");

            services.FlowController = new GameFlowController(
                services.TimeService,
                services.LocationService,
                services.ActionProvider,
                services.TriggerService,
                actionMenuPresenter);

            return services;
        }
    }
}
