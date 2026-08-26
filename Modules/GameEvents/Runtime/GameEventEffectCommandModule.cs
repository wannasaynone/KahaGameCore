using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;
using UnityEngine.Scripting;

namespace KahaGameCore.GameEvents
{
    public sealed class GameEventCommandRouter
    {
        private GameEventRunner runner;

        public void Initialize(GameEventRunner gameEventRunner)
        {
            if (gameEventRunner == null)
            {
                throw new ArgumentNullException(nameof(gameEventRunner));
            }

            if (runner != null && !ReferenceEquals(runner, gameEventRunner))
            {
                throw new InvalidOperationException(
                    "GameEventCommandRouter is already initialized with another runner.");
            }

            runner = gameEventRunner;
        }

        internal UniTask TriggerAsync(
            Guid documentGuid,
            EffectExecutionContext context,
            CancellationToken cancellationToken)
        {
            if (runner == null)
            {
                throw new InvalidOperationException(
                    "GameEventCommandRouter must be initialized by the composition root.");
            }

            return runner.TriggerDocumentAsync(
                documentGuid,
                context,
                cancellationToken);
        }
    }

    internal static class GameEventEffectCommandManifest
    {
        public static readonly EffectCommandDescriptor TriggerEvent =
            new EffectCommandDescriptor(
                "TriggerEvent",
                "Trigger Event",
                "Game Events",
                new[]
                {
                    new EffectCommandParameterDefinition(
                        "event",
                        EffectCommandParameterKind.AssetKey,
                        GameEventEffectCommandModule.EventOptionSourceKey)
                });

        public static readonly IReadOnlyList<EffectCommandDescriptor> Descriptors =
            Array.AsReadOnly(new[] { TriggerEvent });
    }

    public sealed class GameEventEffectCommandModule : IEffectCommandModule
    {
        public const string EventOptionSourceKey = "GameEventDocument";

        private sealed class TriggerEventCommand : IEffectCommand
        {
            private readonly GameEventCommandRouter router;

            public TriggerEventCommand(GameEventCommandRouter router)
            {
                this.router = router ?? throw new ArgumentNullException(nameof(router));
            }

            public UniTask ExecuteAsync(
                EffectExecutionContext context,
                IReadOnlyList<string> arguments,
                CancellationToken cancellationToken)
            {
                if (!Guid.TryParseExact(arguments[0], "D", out Guid documentGuid))
                {
                    throw new GameEventException(
                        "InvalidDocumentGuid",
                        $"TriggerEvent requires a canonical DocumentGuid, but received '{arguments[0]}'.");
                }

                return router.TriggerAsync(
                    documentGuid,
                    context,
                    cancellationToken);
            }
        }

        private readonly GameEventCommandRouter router;

        public GameEventEffectCommandModule(GameEventCommandRouter router)
        {
            this.router = router ?? throw new ArgumentNullException(nameof(router));
        }

        public EffectCommandDefinition CreateDefinition(string commandName)
        {
            if (string.Equals(commandName, "TriggerEvent", StringComparison.Ordinal))
            {
                return new EffectCommandDefinition(
                    GameEventEffectCommandManifest.TriggerEvent,
                    new TriggerEventCommand(router));
            }

            throw new ArgumentException(
                $"Game Events does not own command '{commandName}'.",
                nameof(commandName));
        }
    }

    [Preserve]
    public sealed class GameEventEffectCommandModuleFactory :
        IEffectCommandModuleFactory
    {
        public IReadOnlyList<EffectCommandDescriptor> GetDescriptors()
        {
            return GameEventEffectCommandManifest.Descriptors;
        }

        public IEffectCommandModule Create(EffectCommandServiceRegistry services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            return new GameEventEffectCommandModule(
                services.GetRequired<GameEventCommandRouter>());
        }
    }
}
