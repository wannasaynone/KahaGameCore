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
            EffectCommandServiceRegistry commandServices =
                new EffectCommandServiceRegistry()
                    .Add(parameters);
            EffectRuntime effects = EffectCommandBootstrapper.CreateRuntime(
                catalog.CommandConfiguration,
                commandServices);
            GameEventDocumentJsonCodec eventCodec =
                new GameEventDocumentJsonCodec();
            GameEventRunner events = new GameEventRunner(
                new GameEventCatalog(catalog, eventCodec),
                effects,
                parameters,
                eventCodec);

            return new GameEventRuntime(
                parameters,
                effects,
                events,
                new CancellationTokenSource());
        }
    }
}
