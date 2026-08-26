using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;
using KahaGameCore.Expressions;
using KahaGameCore.Parameters;
using UnityEngine;

namespace KahaGameCore.GameEvents
{
    public sealed class GameEventRunner
    {
        public static event Action<GameEventRunner, bool> QueueActivityChanged;

        private sealed class QueuedJob
        {
            public QueuedJob(Func<UniTask> operation, CancellationToken cancellationToken)
            {
                Operation = operation;
                CancellationToken = cancellationToken;
                Completion = new UniTaskCompletionSource();
            }

            public Func<UniTask> Operation { get; }
            public CancellationToken CancellationToken { get; }
            public UniTaskCompletionSource Completion { get; }
        }

        private readonly GameEventCatalog catalog;
        private readonly EffectRuntime effects;
        private readonly ParameterStore parameters;
        private readonly GameEventDocumentJsonCodec codec;
        private readonly Queue<QueuedJob> queue = new Queue<QueuedJob>();
        private readonly HashSet<Guid> activeDocuments = new HashSet<Guid>();
        private bool isProcessingQueue;
        private bool isExecutingCommands;
        private bool hasActiveQueueWork;
        private UniTaskCompletionSource idleCompletion;

        public bool IsProcessingQueue => isProcessingQueue;

        public GameEventRunner(
            GameEventCatalog catalog,
            EffectRuntime effects,
            ParameterStore parameters,
            GameEventDocumentJsonCodec codec = null)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.effects = effects ?? throw new ArgumentNullException(nameof(effects));
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            this.codec = codec ?? new GameEventDocumentJsonCodec();
        }

        public UniTask RunAsync(TextAsset gameEventFile, EventContext context)
        {
            if (gameEventFile == null) throw new ArgumentNullException(nameof(gameEventFile));
            if (context == null) throw new ArgumentNullException(nameof(context));

            string json = gameEventFile.text;
            return Enqueue(
                () => RunDirectAsync(json, context),
                context.CancellationToken);
        }

        public UniTask TriggerAsync(string triggerTiming, EventContext context)
        {
            if (triggerTiming == null) throw new ArgumentNullException(nameof(triggerTiming));
            if (context == null) throw new ArgumentNullException(nameof(context));
            return Enqueue(
                () => TriggerCoreAsync(triggerTiming, context),
                context.CancellationToken);
        }

        public UniTask WaitUntilIdleAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!isProcessingQueue)
            {
                return UniTask.CompletedTask;
            }

            UniTask idle = idleCompletion.Task;
            return cancellationToken.CanBeCanceled
                ? idle.AttachExternalCancellation(cancellationToken)
                : idle;
        }

        private async UniTask TriggerCoreAsync(string triggerTiming, EventContext context)
        {
            List<GameEventCatalog.Entry> snapshot = new List<GameEventCatalog.Entry>();
            for (int index = 0; index < catalog.Entries.Count; index++)
            {
                GameEventCatalog.Entry entry = catalog.Entries[index];
                if (!string.Equals(
                        entry.Document.TriggerTiming,
                        triggerTiming,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (EvaluateCondition(entry.Document))
                {
                    snapshot.Add(entry);
                }
            }

            for (int index = 0; index < snapshot.Count; index++)
            {
                await ExecuteDocumentAsync(snapshot[index].Document, context);
            }
        }

        private UniTask RunDirectAsync(string json, EventContext context)
        {
            GameEventDocument document = codec.Read(json);
            return RunDocumentAsync(document, context);
        }

        internal UniTask TriggerDocumentAsync(
            Guid documentGuid,
            EffectExecutionContext effectContext,
            CancellationToken cancellationToken)
        {
            if (!catalog.TryGetDocument(documentGuid, out GameEventDocument document))
            {
                throw new GameEventException(
                    "MissingDocument",
                    $"Game Event '{documentGuid:D}' is not in the current catalog.");
            }

            var context = new EventContext(cancellationToken, effectContext);
            return isExecutingCommands
                ? RunDocumentAsync(document, context)
                : Enqueue(
                    () => RunDocumentAsync(document, context),
                    cancellationToken);
        }

        private UniTask Enqueue(Func<UniTask> operation, CancellationToken cancellationToken)
        {
            QueuedJob job = new QueuedJob(operation, cancellationToken);
            queue.Enqueue(job);
            if (!isProcessingQueue)
            {
                isProcessingQueue = true;
                idleCompletion = new UniTaskCompletionSource();
                ProcessQueueAsync().Forget();
            }

            return job.Completion.Task;
        }

        private async UniTask ProcessQueueAsync()
        {
            while (queue.Count > 0)
            {
                QueuedJob job = queue.Dequeue();
                try
                {
                    job.CancellationToken.ThrowIfCancellationRequested();
                    await job.Operation();
                    job.Completion.TrySetResult();
                }
                catch (OperationCanceledException exception)
                {
                    job.Completion.TrySetCanceled(exception.CancellationToken);
                }
                catch (Exception exception)
                {
                    job.Completion.TrySetException(exception);
                }
            }

            isProcessingQueue = false;
            UniTaskCompletionSource completion = idleCompletion;
            idleCompletion = null;
            if (hasActiveQueueWork)
            {
                hasActiveQueueWork = false;
                NotifyQueueActivityChanged(false);
            }
            completion.TrySetResult();
        }

        private void EnsureQueueActivityStarted()
        {
            if (hasActiveQueueWork)
            {
                return;
            }

            hasActiveQueueWork = true;
            NotifyQueueActivityChanged(true);
        }

        private void NotifyQueueActivityChanged(bool isActive)
        {
            Delegate[] subscribers = QueueActivityChanged?.GetInvocationList();
            if (subscribers == null)
            {
                return;
            }

            for (int index = 0; index < subscribers.Length; index++)
            {
                try
                {
                    ((Action<GameEventRunner, bool>)subscribers[index])(
                        this,
                        isActive);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private async UniTask RunDocumentAsync(GameEventDocument document, EventContext context)
        {
            if (!EvaluateCondition(document))
            {
                return;
            }

            await ExecuteDocumentAsync(document, context);
        }

        private async UniTask ExecuteDocumentAsync(
            GameEventDocument document,
            EventContext context)
        {
            if (!activeDocuments.Add(document.DocumentGuid))
            {
                throw new GameEventException(
                    "RecursiveEvent",
                    $"Game Event '{document.DisplayName}' recursively triggers itself.");
            }

            try
            {
                await RunCommandsAsync(document, context);
            }
            finally
            {
                activeDocuments.Remove(document.DocumentGuid);
            }
        }

        private bool EvaluateCondition(GameEventDocument document)
        {
            ExpressionResult<bool> condition = parameters.EvaluateCondition(document.Condition);
            if (!condition.IsSuccess)
            {
                throw new GameEventException(
                    "ConditionFailed",
                    $"Game Event '{document.DisplayName}' condition failed: {condition.Error}");
            }

            return condition.Value;
        }

        private async UniTask RunCommandsAsync(GameEventDocument document, EventContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            EnsureQueueActivityStarted();
            bool previousIsExecutingCommands = isExecutingCommands;
            isExecutingCommands = true;
            EffectExecutionResult result;
            try
            {
                result = await effects.ExecuteAsync(
                    document.Commands,
                    context.EffectContext,
                    context.CancellationToken);
            }
            finally
            {
                isExecutingCommands = previousIsExecutingCommands;
            }
            if (result.Status == EffectExecutionStatus.Cancelled)
            {
                throw new OperationCanceledException(
                    $"Game Event '{document.DisplayName}' was cancelled: {result.FormatDiagnostic()}",
                    context.CancellationToken);
            }

            if (result.Status == EffectExecutionStatus.Failed)
            {
                throw new GameEventException(
                    "EffectFailed",
                    $"Game Event '{document.DisplayName}' failed: {result.FormatDiagnostic()}");
            }
        }
    }
}
