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
        private bool isProcessingQueue;
        private UniTaskCompletionSource idleCompletion;

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
            Debug.Log("Run " + json);
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
                await RunCommandsAsync(snapshot[index].Document, context);
            }
        }

        private UniTask RunDirectAsync(string json, EventContext context)
        {
            GameEventDocument document = codec.Read(json);
            return RunDocumentAsync(document, context);
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
            completion.TrySetResult();
        }

        private async UniTask RunDocumentAsync(GameEventDocument document, EventContext context)
        {
            if (!EvaluateCondition(document))
            {
                return;
            }

            await RunCommandsAsync(document, context);
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
            EffectExecutionResult result = await effects.ExecuteAsync(
                document.Commands,
                context.EffectContext,
                context.CancellationToken);
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
