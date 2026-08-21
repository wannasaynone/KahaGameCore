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

    public sealed class GameFlowEffectCommandModule : IEffectCommandModule
    {
        private readonly GameFlowExpressions expressions;
        private readonly ITimeService timeService;
        private readonly ILocationService locationService;
        private readonly IDialoguePlayer dialoguePlayer;
        private readonly IPerformancePlayer performancePlayer;
        private readonly IGameTextProvider textProvider;
        private readonly IHintPresenter hintPresenter;
        private readonly ILocationMenuPresenter locationMenuPresenter;

        public GameFlowEffectCommandModule(GameFlowEffectCommandServices services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            expressions = services.Expressions;
            timeService = services.TimeService;
            locationService = services.LocationService;
            dialoguePlayer = services.DialoguePlayer;
            performancePlayer = services.PerformancePlayer;
            textProvider = services.TextProvider;
            hintPresenter = services.HintPresenter;
            locationMenuPresenter = services.LocationMenuPresenter;
        }

        public EffectCommandDefinition CreateDefinition(string commandName)
        {
            switch (commandName)
            {
                case "AdvancePhase":
                    return Bind(
                        GameFlowEffectCommandManifest.AdvancePhase,
                        new AdvancePhaseCommand(timeService));
                case "SetPhase":
                    return Bind(
                        GameFlowEffectCommandManifest.SetPhase,
                        new SetPhaseCommand(timeService));
                case "MoveToLocation":
                    return Bind(
                        GameFlowEffectCommandManifest.MoveToLocation,
                        new MoveToLocationCommand(expressions, locationService));
                case "StartDialogue":
                    return Bind(
                        GameFlowEffectCommandManifest.StartDialogue,
                        new StartDialogueCommand(expressions, dialoguePlayer));
                case "ShowHint":
                    return Bind(
                        GameFlowEffectCommandManifest.ShowHint,
                        new ShowHintCommand(expressions, textProvider, hintPresenter));
                case "Monologue":
                    return Bind(
                        GameFlowEffectCommandManifest.Monologue,
                        new MonologueCommand(textProvider));
                case "PlayPerformance":
                    return Bind(
                        GameFlowEffectCommandManifest.PlayPerformance,
                        new PlayPerformanceCommand(performancePlayer));
                case "OpenLocationMenu":
                    return Bind(
                        GameFlowEffectCommandManifest.OpenLocationMenu,
                        new OpenLocationMenuCommand(
                            locationService,
                            locationMenuPresenter));
                case "ReturnToTitle":
                    return Bind(
                        GameFlowEffectCommandManifest.ReturnToTitle,
                        new ReturnToTitleCommand());
                default:
                    throw new ArgumentException(
                        $"GameFlow does not own command '{commandName}'.",
                        nameof(commandName));
            }
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
