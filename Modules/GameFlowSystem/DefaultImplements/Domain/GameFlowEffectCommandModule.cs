using System;
using System.Collections.Generic;
using KahaGameCore.Effects;
using KahaGameCore.GameFlowSystem.DefaultImplements.Commands;

namespace KahaGameCore.GameFlowSystem.DefaultImplements
{
    internal static class GameFlowEffectCommandManifest
    {
        public static readonly EffectCommandDescriptor AdvancePhase = Describe(
            "AdvancePhase", "Game Flow");
        public static readonly EffectCommandDescriptor SetPhase = Describe(
            "SetPhase", "Game Flow",
            Parameter("phase", EffectCommandParameterKind.Literal));
        public static readonly EffectCommandDescriptor MoveToLocation = Describe(
            "MoveToLocation", "Game Flow",
            Parameter("locationId", EffectCommandParameterKind.NumberExpression));
        public static readonly EffectCommandDescriptor StartDialogue = Describe(
            "StartDialogue", "Presentation",
            Parameter("dialogueId", EffectCommandParameterKind.NumberExpression));
        public static readonly EffectCommandDescriptor ShowHint = Describe(
            "ShowHint", "Presentation",
            Parameter("textId", EffectCommandParameterKind.NumberExpression));
        public static readonly EffectCommandDescriptor Monologue = Describe(
            "Monologue", "Presentation",
            Parameter("group", EffectCommandParameterKind.Literal));
        public static readonly EffectCommandDescriptor PlayPerformance = Describe(
            "PlayPerformance", "Presentation",
            Parameter("performanceId", EffectCommandParameterKind.AssetKey));
        public static readonly EffectCommandDescriptor OpenLocationMenu = Describe(
            "OpenLocationMenu", "Presentation");
        public static readonly EffectCommandDescriptor ReturnToTitle = Describe(
            "ReturnToTitle", "Game Flow");

        public static readonly IReadOnlyList<EffectCommandDescriptor> Descriptors =
            Array.AsReadOnly(new[]
            {
                AdvancePhase,
                SetPhase,
                MoveToLocation,
                StartDialogue,
                ShowHint,
                Monologue,
                PlayPerformance,
                OpenLocationMenu,
                ReturnToTitle
            });

        private static EffectCommandDescriptor Describe(
            string name,
            string category,
            params EffectCommandParameterDefinition[] parameters)
        {
            return new EffectCommandDescriptor(
                name,
                name,
                category,
                parameters ?? Array.Empty<EffectCommandParameterDefinition>());
        }

        private static EffectCommandParameterDefinition Parameter(
            string name,
            EffectCommandParameterKind kind)
        {
            return new EffectCommandParameterDefinition(name, kind);
        }
    }

    public static class GameFlowEffectCommandModule
    {
        public static IReadOnlyList<EffectCommandDefinition> CreateDefinitions(
            GameFlowEffectCommandServices services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            return new[]
            {
                Bind(
                    GameFlowEffectCommandManifest.AdvancePhase,
                    new AdvancePhaseCommand(services.TimeService)),
                Bind(
                    GameFlowEffectCommandManifest.SetPhase,
                    new SetPhaseCommand(services.TimeService)),
                Bind(
                    GameFlowEffectCommandManifest.MoveToLocation,
                    new MoveToLocationCommand(
                        services.Expressions,
                        services.LocationService)),
                Bind(
                    GameFlowEffectCommandManifest.StartDialogue,
                    new StartDialogueCommand(
                        services.Expressions,
                        services.DialoguePlayer)),
                Bind(
                    GameFlowEffectCommandManifest.ShowHint,
                    new ShowHintCommand(
                        services.Expressions,
                        services.TextProvider,
                        services.HintPresenter)),
                Bind(
                    GameFlowEffectCommandManifest.Monologue,
                    new MonologueCommand(services.TextProvider)),
                Bind(
                    GameFlowEffectCommandManifest.PlayPerformance,
                    new PlayPerformanceCommand(services.PerformancePlayer)),
                Bind(
                    GameFlowEffectCommandManifest.OpenLocationMenu,
                    new OpenLocationMenuCommand(
                        services.LocationService,
                        services.LocationMenuPresenter)),
                Bind(
                    GameFlowEffectCommandManifest.ReturnToTitle,
                    new ReturnToTitleCommand())
            };
        }

        private static EffectCommandDefinition Bind(
            EffectCommandDescriptor descriptor,
            IEffectCommand command)
        {
            return new EffectCommandDefinition(descriptor, command);
        }
    }

    public sealed class GameFlowEffectCommandServices
    {
        public GameFlowEffectCommandServices(
            GameFlowExpressions expressions,
            ITimeService timeService,
            ILocationService locationService,
            IDialoguePlayer dialoguePlayer,
            IPerformancePlayer performancePlayer,
            IGameTextProvider textProvider,
            IHintPresenter hintPresenter,
            ILocationMenuPresenter locationMenuPresenter)
        {
            Expressions = expressions ?? throw new ArgumentNullException(nameof(expressions));
            TimeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
            LocationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
            DialoguePlayer = dialoguePlayer ?? throw new ArgumentNullException(nameof(dialoguePlayer));
            PerformancePlayer = performancePlayer ?? throw new ArgumentNullException(nameof(performancePlayer));
            TextProvider = textProvider ?? throw new ArgumentNullException(nameof(textProvider));
            HintPresenter = hintPresenter;
            LocationMenuPresenter = locationMenuPresenter;
        }

        internal GameFlowExpressions Expressions { get; }
        internal ITimeService TimeService { get; }
        internal ILocationService LocationService { get; }
        internal IDialoguePlayer DialoguePlayer { get; }
        internal IPerformancePlayer PerformancePlayer { get; }
        internal IGameTextProvider TextProvider { get; }
        internal IHintPresenter HintPresenter { get; }
        internal ILocationMenuPresenter LocationMenuPresenter { get; }
    }
}
