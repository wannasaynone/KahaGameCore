using System;
using System.Threading;
using KahaGameCore.Effects;
using KahaGameCore.Parameters;

namespace KahaGameCore.GameEvents
{
    public sealed class GameEventRuntime : IDisposable
    {
        private readonly CancellationTokenSource lifetime;
        private bool disposed;

        internal GameEventRuntime(
            ParameterStore parameters,
            EffectRuntime effects,
            GameEventRunner events,
            CancellationTokenSource lifetime)
        {
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
            Events = events ?? throw new ArgumentNullException(nameof(events));
            this.lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            Context = new EventContext(lifetime.Token);
        }

        public ParameterStore Parameters { get; }
        public EffectRuntime Effects { get; }
        public GameEventRunner Events { get; }
        public EventContext Context { get; }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            lifetime.Cancel();
            lifetime.Dispose();
        }
    }

    public static class GameEventRuntimeBootstrapper
    {
        public static GameEventRuntime Create(GameEventCatalogAsset catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));

            ParameterStore parameters =
                ParameterRuntimeLoader.Load(catalog.ParameterTables);
            GameEventDocumentJsonCodec eventCodec =
                new GameEventDocumentJsonCodec();
            GameEventCatalog runtimeCatalog =
                new GameEventCatalog(catalog, eventCodec);
            var eventCommandRouter = new GameEventCommandRouter();
            EffectCommandServiceRegistry commandServices =
                new EffectCommandServiceRegistry()
                    .Add(parameters)
                    .Add(eventCommandRouter);
            var commandRegistry = new EffectCommandRegistry();
            var effects = new EffectRuntime(commandRegistry);
            EffectCommandBootstrapper.Populate(
                commandRegistry,
                catalog.CommandConfiguration,
                commandServices);
            GameEventRunner events = new GameEventRunner(
                runtimeCatalog,
                effects,
                parameters,
                eventCodec);
            eventCommandRouter.Initialize(events);

            return new GameEventRuntime(
                parameters,
                effects,
                events,
                new CancellationTokenSource());
        }
    }
}
