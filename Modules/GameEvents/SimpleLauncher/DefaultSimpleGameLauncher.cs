using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;
using KahaGameCore.Parameters;
using KahaGameCore.Presentation;
using UnityEngine;

namespace KahaGameCore.GameEvents
{
    /// <summary>
    /// Scene entry point for Parameters, Effects and Game Events. The runtime
    /// itself belongs to GameEventSession and outlives this component, so this
    /// class owns only what is genuinely Scene-scoped: the cancellation
    /// lifetime, the state binders and the triggers under this hierarchy.
    /// It intentionally owns no dialogue, UI or flow controller.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public class DefaultSimpleGameLauncher : ParameterRuntimeSource
    {
        [SerializeField] private GameEventCatalogAsset catalog;
        [Tooltip("Automatically initialize all child Game Event triggers.")]
        [SerializeField] private bool initializeChildTriggers = true;

        private GameEventRuntime runtime;
        private CancellationTokenSource sceneLifetime;
        private StartGameEventTrigger[] startEventTriggers = Array.Empty<StartGameEventTrigger>();

        public ParameterStore Parameters => runtime?.Parameters;
        public EffectRuntime Effects => runtime?.Effects;
        public GameEventRunner Events => runtime?.Events;
        public EventContext Context { get; private set; }
        public bool IsReady => runtime != null;

        protected virtual void Awake()
        {
            if (catalog == null)
                throw new InvalidOperationException(
                    "[DefaultSimpleGameLauncher] Game Event Catalog is required.");
            runtime = GameEventSession.GetOrCreate(catalog);
            sceneLifetime = new CancellationTokenSource();
            Context = new EventContext(sceneLifetime.Token);
            Initialize(Parameters);
            InitializeParameterStateBinders();
            if (initializeChildTriggers)
                InitializeTriggers();
        }

        protected virtual void Start()
        {
            TriggerActiveStartEventsAsync().Forget();
        }

        protected virtual void OnDestroy()
        {
            // Cancels the events this Scene started. Parameters and the event
            // catalog belong to the session, so they are deliberately untouched.
            sceneLifetime?.Cancel();
            sceneLifetime?.Dispose();
            sceneLifetime = null;
            Context = null;
            runtime = null;
        }

        private void InitializeTriggers()
        {
            var sceneTriggerArray = GetComponentsInChildren<SceneGameEventTrigger>(true);
            foreach (SceneGameEventTrigger trigger in sceneTriggerArray)
                trigger.Initialize(Events, Context);

            var sceneTrigger2DArray = GetComponentsInChildren<SceneGameEventTrigger2D>(true);
            foreach (SceneGameEventTrigger2D trigger in sceneTrigger2DArray)
                trigger.Initialize(Events, Context);

            startEventTriggers = GetComponentsInChildren<StartGameEventTrigger>(true);
            for (int index = 0; index < startEventTriggers.Length; index++)
            {
                startEventTriggers[index].Initialize(Events, Context);
            }
        }

        protected async UniTask TriggerActiveStartEventsAsync()
        {
            for (int index = 0; index < startEventTriggers.Length; index++)
            {
                StartGameEventTrigger trigger = startEventTriggers[index];
                if (trigger == null || !trigger.isActiveAndEnabled)
                {
                    continue;
                }

                await trigger.TriggerAsync();
            }
        }

        private void InitializeParameterStateBinders()
        {
            var binderArray = GetComponentsInChildren<ParameterStateBinder>(true);
            foreach (ParameterStateBinder binder in binderArray)
            {
                binder.Initialize(Parameters);
            }
        }
    }
}
