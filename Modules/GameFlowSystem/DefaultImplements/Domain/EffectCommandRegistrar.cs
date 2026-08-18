using System;
using System.Collections.Generic;
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
        private static readonly EffectCommandDescriptor AddParameterDescriptor = Describe(
            "AddParameter", "Parameters",
            Parameter("key", EffectCommandParameterKind.ParameterKey),
            Parameter("value", EffectCommandParameterKind.NumberExpression));
        private static readonly EffectCommandDescriptor SetParameterDescriptor = Describe(
            "SetParameter", "Parameters",
            Parameter("key", EffectCommandParameterKind.ParameterKey),
            Parameter("value", EffectCommandParameterKind.Literal));
        private static readonly EffectCommandDescriptor AdvancePhaseDescriptor = Describe(
            "AdvancePhase", "Game Flow");
        private static readonly EffectCommandDescriptor SetPhaseDescriptor = Describe(
            "SetPhase", "Game Flow",
            Parameter("phase", EffectCommandParameterKind.Literal));
        private static readonly EffectCommandDescriptor MoveToLocationDescriptor = Describe(
            "MoveToLocation", "Game Flow",
            Parameter("locationId", EffectCommandParameterKind.NumberExpression));
        private static readonly EffectCommandDescriptor StartDialogueDescriptor = Describe(
            "StartDialogue", "Presentation",
            Parameter("dialogueId", EffectCommandParameterKind.NumberExpression));
        private static readonly EffectCommandDescriptor ShowHintDescriptor = Describe(
            "ShowHint", "Presentation",
            Parameter("textId", EffectCommandParameterKind.NumberExpression));
        private static readonly EffectCommandDescriptor MonologueDescriptor = Describe(
            "Monologue", "Presentation",
            Parameter("group", EffectCommandParameterKind.Literal));
        private static readonly EffectCommandDescriptor PlayPerformanceDescriptor = Describe(
            "PlayPerformance", "Presentation",
            Parameter("performanceId", EffectCommandParameterKind.AssetKey));
        private static readonly EffectCommandDescriptor OpenLocationMenuDescriptor = Describe(
            "OpenLocationMenu", "Presentation");
        private static readonly EffectCommandDescriptor ReturnToTitleDescriptor = Describe(
            "ReturnToTitle", "Game Flow");
        private static readonly EffectCommandDescriptor WaitDescriptor = Describe(
            "Wait", "Presentation",
            Parameter("seconds", EffectCommandParameterKind.Literal));

        private static readonly IReadOnlyList<EffectCommandDescriptor> descriptors =
            Array.AsReadOnly(new[]
            {
                AddParameterDescriptor,
                SetParameterDescriptor,
                AdvancePhaseDescriptor,
                SetPhaseDescriptor,
                MoveToLocationDescriptor,
                StartDialogueDescriptor,
                ShowHintDescriptor,
                MonologueDescriptor,
                PlayPerformanceDescriptor,
                OpenLocationMenuDescriptor,
                ReturnToTitleDescriptor,
                WaitDescriptor
            });

        public static IReadOnlyList<EffectCommandDescriptor> Descriptors => descriptors;

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
            registry.Register(Bind(
                AddParameterDescriptor,
                new AddParameterCommand(parameters, expressions)));
            registry.Register(Bind(
                SetParameterDescriptor,
                new SetParameterCommand(parameters, expressions)));
            registry.Register(Bind(AdvancePhaseDescriptor, new AdvancePhaseCommand(timeService)));
            registry.Register(Bind(SetPhaseDescriptor, new SetPhaseCommand(timeService)));
            registry.Register(Bind(
                MoveToLocationDescriptor,
                new MoveToLocationCommand(expressions, locationService)));
            registry.Register(Bind(
                StartDialogueDescriptor,
                new StartDialogueCommand(expressions, dialoguePlayer)));
            registry.Register(Bind(
                ShowHintDescriptor,
                new ShowHintCommand(expressions, textProvider, hintPresenter)));
            registry.Register(Bind(MonologueDescriptor, new MonologueCommand(textProvider)));
            registry.Register(Bind(
                PlayPerformanceDescriptor,
                new PlayPerformanceCommand(performancePlayer)));
            registry.Register(Bind(
                OpenLocationMenuDescriptor,
                new OpenLocationMenuCommand(locationService, locationMenuPresenter)));
            registry.Register(Bind(ReturnToTitleDescriptor, new ReturnToTitleCommand()));
            registry.Register(Bind(WaitDescriptor, new WaitCommand()));
        }

        private static EffectCommandDescriptor Describe(
            string name,
            string category,
            params EffectCommandParameterDefinition[] parameters)
        {
            return new EffectCommandDescriptor(
                name: name,
                displayName: name,
                category: category,
                parameters: parameters ?? Array.Empty<EffectCommandParameterDefinition>());
        }

        private static EffectCommandDefinition Bind(
            EffectCommandDescriptor descriptor,
            IEffectCommand command)
        {
            return new EffectCommandDefinition(descriptor, command);
        }

        private static EffectCommandParameterDefinition Parameter(
            string name,
            EffectCommandParameterKind kind)
        {
            return new EffectCommandParameterDefinition(name, kind);
        }
    }
}
