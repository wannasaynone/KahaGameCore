using System;
using KahaGameCore.Effects;
using KahaGameCore.GameFlowSystem.DefaultImplements;
using KahaGameCore.GameFlowSystem.DefaultImplements.Commands;
using KahaGameCore.Parameters;

namespace KahaGameCore.GameFlowSystem.DefaultImplements
{
    /// <summary>
    /// 將本專案所有效果指令與編輯 metadata 註冊進 EffectCommandRegistry。
    /// 新增指令時：實作 IEffectCommand → 在此註冊 → 即可在表格中使用。
    /// </summary>
    public static class EffectCommandRegistrar
    {
        public static void RegisterAll(
            EffectCommandRegistry registry,
            ParameterStore parameters,
            GameFlowExpressions expressions,
            ITimeService timeService,
            ILocationService locationService,
            IDialoguePlayer dialoguePlayer,
            IPerformancePlayer performancePlayer,
            IGameTextProvider textProvider,
            IHintPresenter hintPresenter,
            ILocationMenuPresenter locationMenuPresenter)
        {
            registry.Register(Define(
                "AddParameter", "Parameters",
                new AddParameterCommand(parameters, expressions),
                Parameter("key", EffectCommandParameterKind.ParameterKey),
                Parameter("value", EffectCommandParameterKind.NumberExpression)));
            registry.Register(Define(
                "SetParameter", "Parameters",
                new SetParameterCommand(parameters, expressions),
                Parameter("key", EffectCommandParameterKind.ParameterKey),
                Parameter("value", EffectCommandParameterKind.Literal)));
            registry.Register(Define(
                "AdvancePhase", "Game Flow",
                new AdvancePhaseCommand(timeService)));
            registry.Register(Define(
                "SetPhase", "Game Flow",
                new SetPhaseCommand(timeService),
                Parameter("phase", EffectCommandParameterKind.Literal)));
            registry.Register(Define(
                "MoveToLocation", "Game Flow",
                new MoveToLocationCommand(expressions, locationService),
                Parameter("locationId", EffectCommandParameterKind.NumberExpression)));
            registry.Register(Define(
                "StartDialogue", "Presentation",
                new StartDialogueCommand(expressions, dialoguePlayer),
                Parameter("dialogueId", EffectCommandParameterKind.NumberExpression)));
            registry.Register(Define(
                "ShowHint", "Presentation",
                new ShowHintCommand(expressions, textProvider, hintPresenter),
                Parameter("textId", EffectCommandParameterKind.NumberExpression)));
            registry.Register(Define(
                "Monologue", "Presentation",
                new MonologueCommand(textProvider),
                Parameter("group", EffectCommandParameterKind.Literal)));
            registry.Register(Define(
                "PlayPerformance", "Presentation",
                new PlayPerformanceCommand(performancePlayer),
                Parameter("performanceId", EffectCommandParameterKind.AssetKey)));
            registry.Register(Define(
                "OpenLocationMenu", "Presentation",
                new OpenLocationMenuCommand(locationService, locationMenuPresenter)));
            registry.Register(Define(
                "ReturnToTitle", "Game Flow",
                new ReturnToTitleCommand()));
            registry.Register(Define(
                "Wait", "Presentation",
                new WaitCommand(),
                Parameter("seconds", EffectCommandParameterKind.Literal)));
        }

        private static EffectCommandDefinition Define(
            string name,
            string category,
            IEffectCommand command,
            params EffectCommandParameterDefinition[] parameters)
        {
            return new EffectCommandDefinition(
                name,
                name,
                category,
                parameters ?? Array.Empty<EffectCommandParameterDefinition>(),
                command);
        }

        private static EffectCommandParameterDefinition Parameter(
            string name,
            EffectCommandParameterKind kind)
        {
            return new EffectCommandParameterDefinition(name, kind);
        }
    }
}
