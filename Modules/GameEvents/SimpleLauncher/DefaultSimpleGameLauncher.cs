using System;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;
using KahaGameCore.Parameters;
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
        [Tooltip("Automatically initialize all child Game Event triggers.")]
        [SerializeField] private bool initializeChildTriggers = true;

        private GameEventRuntime runtime;
        private StartGameEventTrigger[] startEventTriggers =
            Array.Empty<StartGameEventTrigger>();

        public ParameterStore Parameters => runtime?.Parameters;
        public EffectRuntime Effects => runtime?.Effects;
        public GameEventRunner Events => runtime?.Events;
        public EventContext Context => runtime?.Context;
        public bool IsReady => runtime != null;

        protected virtual void Awake()
        {
            if (catalog == null)
                throw new InvalidOperationException(
                    "[DefaultSimpleGameLauncher] Game Event Catalog is required.");
            runtime = GameEventRuntimeBootstrapper.Create(catalog);
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
            runtime?.Dispose();
            runtime = null;
        }

        private void OnDisable()
        {
            Debug.Log("diabled")
;
        }

        private void InitializeTriggers()
        {
            foreach (SceneGameEventTrigger trigger in
                     GetComponentsInChildren<SceneGameEventTrigger>(true))
                trigger.Initialize(Events, Context);
            foreach (SceneGameEventTrigger2D trigger in
                     GetComponentsInChildren<SceneGameEventTrigger2D>(true))
                trigger.Initialize(Events, Context);

            startEventTriggers =
                GetComponentsInChildren<StartGameEventTrigger>(true);
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
            foreach (ParameterStateBinder binder in
                     GetComponentsInChildren<ParameterStateBinder>(true))
            {
                binder.Initialize(Parameters);
            }
        }
    }
}
