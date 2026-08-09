using KahaGameCore.Effects;
using KahaGameCore.GameFlowSystem.DefaultImplements;
using KahaGameCore.GameFlowSystem.DefaultImplements.Commands;

namespace KahaGameCore.GameFlowSystem.DefaultImplements
{
    /// <summary>
    /// 將本專案所有效果指令註冊進 EffectCommandFactoryContainer。
    /// 新增指令時：實作 EffectCommandBase → 在此註冊 → 即可在表格中使用。
    /// </summary>
    public static class EffectCommandRegistrar
    {
        public static void RegisterAll(
            EffectCommandFactoryContainer container,
            IGameState gameState,
            GameFlowExpressions expressions,
            ITimeService timeService,
            ILocationService locationService,
            IDialoguePlayer dialoguePlayer,
            IPerformancePlayer performancePlayer,
            IGameTextProvider textProvider,
            IHintPresenter hintPresenter,
            ILocationMenuPresenter locationMenuPresenter)
        {
            container.RegisterFactory("AddValue", new DelegateEffectCommandFactory(() => new AddValueCommand(gameState, expressions)));
            container.RegisterFactory("SetValue", new DelegateEffectCommandFactory(() => new SetValueCommand(gameState, expressions)));
            container.RegisterFactory("AdvanceTime", new DelegateEffectCommandFactory(() => new AdvanceTimeCommand(timeService)));
            container.RegisterFactory("SetPhase", new DelegateEffectCommandFactory(() => new SetPhaseCommand(timeService)));
            container.RegisterFactory("MoveToLocation", new DelegateEffectCommandFactory(() => new MoveToLocationCommand(expressions, locationService)));
            container.RegisterFactory("StartDialogue", new DelegateEffectCommandFactory(() => new StartDialogueCommand(expressions, dialoguePlayer)));
            container.RegisterFactory("ShowHint", new DelegateEffectCommandFactory(() => new ShowHintCommand(expressions, textProvider, hintPresenter)));
            container.RegisterFactory("Monologue", new DelegateEffectCommandFactory(() => new MonologueCommand(textProvider)));
            container.RegisterFactory("PlayPerformance", new DelegateEffectCommandFactory(() => new PlayPerformanceCommand(performancePlayer)));
            container.RegisterFactory("OpenLocationMenu", new DelegateEffectCommandFactory(() => new OpenLocationMenuCommand(locationService, locationMenuPresenter)));
            container.RegisterFactory("ReturnToTitle", new DelegateEffectCommandFactory(() => new ReturnToTitleCommand()));
            container.RegisterFactory("Wait", new DelegateEffectCommandFactory(() => new WaitCommand()));
        }
    }
}
