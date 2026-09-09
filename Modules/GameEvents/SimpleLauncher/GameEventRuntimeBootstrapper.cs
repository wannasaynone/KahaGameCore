using System;
using KahaGameCore.Effects;
using KahaGameCore.Parameters;
using UnityEngine;

namespace KahaGameCore.GameEvents
{
    /// <summary>
    /// Parameters, Effects and Game Events for one play session. A Scene load
    /// does not rebuild it, which is what lets parameters survive travelling
    /// between Scenes. Scene-scoped things — the cancellation lifetime, state
    /// binders, triggers — belong to the launcher, not here.
    /// </summary>
    public sealed class GameEventRuntime
    {
        internal GameEventRuntime(
            ParameterStore parameters,
            EffectRuntime effects,
            GameEventRunner events)
        {
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
            Events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public ParameterStore Parameters { get; }
        public EffectRuntime Effects { get; }
        public GameEventRunner Events { get; }
    }

    /// <summary>
    /// Owns the single GameEventRuntime a play session has. Every Scene's
    /// launcher asks for the same instance, so parameters outlive a Scene load
    /// without anyone copying them across.
    /// </summary>
    public static class GameEventSession
    {
        private static GameEventCatalogAsset source;

        public static GameEventRuntime Runtime { get; private set; }

        public static GameEventRuntime GetOrCreate(GameEventCatalogAsset catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));

            if (Runtime == null)
            {
                source = catalog;
                Runtime = GameEventRuntimeBootstrapper.Create(catalog);
            }
            else if (source != catalog)
            {
                throw new InvalidOperationException(
                    $"[GameEventSession] The session was built from Game Event Catalog " +
                    $"'{source.name}'; '{catalog.name}' cannot replace it. Call Reset first.");
            }

            return Runtime;
        }

        /// <summary>
        /// Drops the session, so the next launcher builds a runtime with initial
        /// parameter values. This is what starting a new game means.
        /// </summary>
        public static void Reset()
        {
            Runtime = null;
            source = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnEnterPlayMode()
        {
            // Statics survive entering Play Mode when domain reload is disabled.
            Reset();
        }
    }

    public static class GameEventRuntimeBootstrapper
    {
        public static GameEventRuntime Create(GameEventCatalogAsset catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));

            ParameterStore parameters = ParameterRuntimeLoader.Load(catalog.ParameterTables);
            GameEventDocumentJsonCodec eventCodec = new GameEventDocumentJsonCodec();
            GameEventCatalog runtimeCatalog = new GameEventCatalog(catalog, eventCodec);

            var eventCommandRouter = new GameEventCommandRouter();

            EffectCommandDependencies commandDependencies =
                new EffectCommandDependencies()
                    .Add(parameters)
                    .Add(eventCommandRouter);

            var commandRegistry = new EffectCommandRegistry();
            var effects = new EffectRuntime(commandRegistry);

            commandRegistry.PopulateByEffectCommandBootstrapper(
                catalog.CommandConfiguration,
                commandDependencies);
            GameEventRunner events = new GameEventRunner(
                runtimeCatalog,
                effects,
                parameters,
                eventCodec);
            eventCommandRouter.Initialize(events);

            return new GameEventRuntime(parameters, effects, events);
        }
    }
}
