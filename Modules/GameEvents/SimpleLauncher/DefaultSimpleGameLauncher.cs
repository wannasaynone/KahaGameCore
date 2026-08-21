using System;
using System.Linq;
using System.Threading;
using KahaGameCore.Effects;
using KahaGameCore.Parameters;
using KahaGameCore.Parameters.EffectsIntegration;
using KahaGameCore.Presentation;
using UnityEngine;

namespace KahaGameCore.GameEvents
{
    /// <summary>
    /// Minimal composition root for Parameters, Effects and Game Events.
    /// It intentionally owns no dialogue, UI or flow controller. Project flow may start
    /// after this component's Awake and use the exposed runtime services.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public class DefaultSimpleGameLauncher : ParameterRuntimeSource
    {
        [SerializeField] private GameEventCatalogAsset catalog;
        [Tooltip("Automatically initialize all child 3D and 2D Game Event triggers.")]
        [SerializeField] private bool initializeChildTriggers = true;

        private CancellationTokenSource lifetime;

        public ParameterStore Parameters { get; private set; }
        public EffectRuntime Effects { get; private set; }
        public GameEventRunner Events { get; private set; }
        public EventContext Context { get; private set; }
        public bool IsReady => Events != null;

        protected virtual void Awake()
        {
            if (catalog == null)
                throw new InvalidOperationException(
                    "[DefaultSimpleGameLauncher] Game Event Catalog is required.");
            if (catalog.ParameterTables.Count == 0)
                throw new InvalidOperationException(
                    "[DefaultSimpleGameLauncher] The Catalog needs at least one Parameter Table.");

            ParameterTableJsonCodec parameterCodec = new ParameterTableJsonCodec();
            Parameters = new ParameterStore(catalog.ParameterTables
                .Select(asset => asset != null
                    ? parameterCodec.Read(asset.text)
                    : throw new InvalidOperationException(
                        "[DefaultSimpleGameLauncher] The Catalog contains a missing Parameter Table."))
                .SelectMany(table => table.Definitions));
            Initialize(Parameters);
            InitializeParameterStateBinders();

            EffectCommandRegistry commands = new EffectCommandRegistry();
            ParameterEffectCommandRegistrar.RegisterAll(commands, Parameters);
            RegisterProjectCommands(commands, Parameters);
            Effects = new EffectRuntime(commands);

            GameEventDocumentJsonCodec eventCodec = new GameEventDocumentJsonCodec();
            Events = new GameEventRunner(
                new GameEventCatalog(catalog, eventCodec),
                Effects,
                Parameters,
                eventCodec);

            lifetime = new CancellationTokenSource();
            Context = new EventContext(lifetime.Token);
            if (initializeChildTriggers)
                InitializeTriggers();
        }

        /// <summary>Project extension seam for Commands not supplied by runtime modules.</summary>
        protected virtual void RegisterProjectCommands(
            EffectCommandRegistry registry,
            ParameterStore parameters)
        {
        }

        protected virtual void OnDestroy()
        {
            lifetime?.Cancel();
            lifetime?.Dispose();
            lifetime = null;
        }

        private void InitializeTriggers()
        {
            foreach (SceneGameEventTrigger trigger in
                     GetComponentsInChildren<SceneGameEventTrigger>(true))
                trigger.Initialize(Events, Context);
            foreach (SceneGameEventTrigger2D trigger in
                     GetComponentsInChildren<SceneGameEventTrigger2D>(true))
                trigger.Initialize(Events, Context);
        }

        private void InitializeParameterStateBinders()
        {
            foreach (ParameterStateBinder binder in
                     GetComponentsInChildren<ParameterStateBinder>(true))
            {
                binder.Initialize(Parameters);
            }
        }
    }
}
