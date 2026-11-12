using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;
using KahaGameCore.Parameters;
using KahaGameCore.Presentation;
using UnityEngine;
using UnityEngine.Serialization;

namespace KahaGameCore.GameEvents
{
    /// <summary>
    /// Scene entry point for Parameters, Effects and Game Events. The runtime
    /// itself belongs to GameEventSession and outlives this component, so this
    /// class owns only what is genuinely Scene-scoped: the cancellation
    /// lifetime, and initializing the state binders and triggers that are
    /// loaded when it awakes, wherever they sit in the hierarchy.
    /// It intentionally owns no dialogue, UI or flow controller.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public class DefaultSimpleGameLauncher : ParameterRuntimeSource
    {
        [SerializeField] private GameEventCatalogAsset catalog;
        [Tooltip("Automatically initialize every loaded Game Event trigger, including inactive ones.")]
        [FormerlySerializedAs("initializeChildTriggers")]
        [SerializeField] private bool initializeTriggers = true;

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
            if (initializeTriggers)
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
            foreach (SceneGameEventTrigger trigger in FindIncludingInactive<SceneGameEventTrigger>())
                trigger.Initialize(Events, Context);

            foreach (SceneGameEventTrigger2D trigger in FindIncludingInactive<SceneGameEventTrigger2D>())
                trigger.Initialize(Events, Context);

            startEventTriggers = FindIncludingInactive<StartGameEventTrigger>();
            for (int index = 0; index < startEventTriggers.Length; index++)
            {
                startEventTriggers[index].Initialize(Events, Context);
            }
        }

        // Searches every loaded object, wherever it sits in the hierarchy, and
        // includes inactive ones so they are ready when something activates them.
        // Results are in Hierarchy order (top to bottom), so start events run in
        // the order the Scene shows them.
        private static T[] FindIncludingInactive<T>() where T : Component
        {
            T[] found = FindObjectsByType<T>(
                findObjectsInactive: FindObjectsInactive.Include,
                sortMode: FindObjectsSortMode.None);
            if (found.Length < 2)
                return found;

            var keys = new HierarchyKey[found.Length];
            for (int index = 0; index < found.Length; index++)
                keys[index] = new HierarchyKey(found[index].transform);
            Array.Sort(keys, found);
            return found;
        }

        // Scene first, then the sibling index of each ancestor from the root down.
        private readonly struct HierarchyKey : IComparable<HierarchyKey>
        {
            private readonly int sceneHandle;
            private readonly int[] siblingPath;

            public HierarchyKey(Transform transform)
            {
                sceneHandle = transform.gameObject.scene.handle;
                int depth = 0;
                for (Transform current = transform; current != null; current = current.parent)
                    depth++;
                siblingPath = new int[depth];
                for (Transform current = transform; current != null; current = current.parent)
                    siblingPath[--depth] = current.GetSiblingIndex();
            }

            public int CompareTo(HierarchyKey other)
            {
                if (sceneHandle != other.sceneHandle)
                    return sceneHandle.CompareTo(other.sceneHandle);
                int shared = Math.Min(siblingPath.Length, other.siblingPath.Length);
                for (int index = 0; index < shared; index++)
                {
                    if (siblingPath[index] != other.siblingPath[index])
                        return siblingPath[index].CompareTo(other.siblingPath[index]);
                }
                return siblingPath.Length.CompareTo(other.siblingPath.Length);
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
            foreach (ParameterStateBinder binder in FindIncludingInactive<ParameterStateBinder>())
            {
                binder.Initialize(Parameters);
            }
        }
    }
}
