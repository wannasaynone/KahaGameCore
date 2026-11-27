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

        /// <summary>
        /// Runs the document without evaluating its Condition, for debugging
        /// tools such as the Game Event Editor's force trigger. It still waits
        /// in the shared queue behind earlier events.
        /// </summary>
        public UniTask ForceRunAsync(GameEventDocument document, EventContext context)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (context == null) throw new ArgumentNullException(nameof(context));

            return Enqueue(
                () => ExecuteDocumentAsync(document, context),
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
            IReadOnlyList<EffectCommandCall> calls = ParseCommands(document);
            GameEventCommandStructure structure = AnalyzeStructure(document, calls);

            bool previousIsExecutingCommands = isExecutingCommands;
            isExecutingCommands = true;
            try
            {
                int index = 0;
                while (index < calls.Count)
                {
                    EffectCommandCall call = calls[index];
                    switch (call.Name)
                    {
                        case GameEventControlFlow.IfCommandName:
                            index = EvaluateIfCondition(document, call)
                                ? index + 1
                                : structure.GetJumpTarget(index) + 1;
                            break;
                        case GameEventControlFlow.ElseCommandName:
                            // Reaching Else means the If branch ran; skip to EndIf.
                            index = structure.GetJumpTarget(index) + 1;
                            break;
                        case GameEventControlFlow.EndIfCommandName:
                            index++;
                            break;
                        case GameEventControlFlow.QuitCommandName:
                            return;
                        default:
                            EffectExecutionResult result = await effects.ExecuteCommandAsync(
                                call,
                                context.EffectContext,
                                context.CancellationToken);
                            ThrowIfUnsuccessful(document, context, result);
                            index++;
                            break;
                    }
                }
            }
            finally
            {
                isExecutingCommands = previousIsExecutingCommands;
            }
        }

        private IReadOnlyList<EffectCommandCall> ParseCommands(GameEventDocument document)
        {
            EffectParseResult parsed = effects.Parse(document.Commands);
            if (!parsed.IsSuccess)
            {
                throw new GameEventException(
                    "EffectFailed",
                    $"Game Event '{document.DisplayName}' failed: {parsed.Diagnostics[0]}");
            }

            for (int index = 0; index < parsed.Program.Blocks.Count; index++)
            {
                EffectTimingBlock block = parsed.Program.Blocks[index];
                if (string.Equals(block.Name, EffectRuntime.DefaultTiming, StringComparison.Ordinal))
                {
                    return block.Commands;
                }
            }

            throw new GameEventException(
                "EffectFailed",
                $"Game Event '{document.DisplayName}' failed: Effect timing " +
                $"'{EffectRuntime.DefaultTiming}' does not exist.");
        }

        private static GameEventCommandStructure AnalyzeStructure(
            GameEventDocument document,
            IReadOnlyList<EffectCommandCall> calls)
        {
            string[] names = new string[calls.Count];
            for (int index = 0; index < calls.Count; index++)
            {
                EffectCommandCall call = calls[index];
                names[index] = call.Name;
                if (GameEventControlFlow.TryGetDescriptor(
                        call.Name,
                        out EffectCommandDescriptor descriptor) &&
                    descriptor.Parameters.Count != call.Arguments.Count)
                {
                    throw new GameEventException(
                        "InvalidControlFlow",
                        $"Game Event '{document.DisplayName}' command {index + 1} " +
                        $"'{call.Name}' expects {descriptor.Parameters.Count} arguments " +
                        $"but received {call.Arguments.Count}.");
                }
            }

            GameEventCommandStructure structure = GameEventControlFlow.Analyze(names);
            if (!structure.IsValid)
            {
                throw new GameEventException(
                    "InvalidControlFlow",
                    $"Game Event '{document.DisplayName}' command " +
                    $"{structure.FirstErrorIndex + 1}: " +
                    structure.GetError(structure.FirstErrorIndex));
            }

            return structure;
        }

        private bool EvaluateIfCondition(GameEventDocument document, EffectCommandCall call)
        {
            ExpressionResult<bool> condition = parameters.EvaluateCondition(call.Arguments[0]);
            if (!condition.IsSuccess)
            {
                throw new GameEventException(
                    "ConditionFailed",
                    $"Game Event '{document.DisplayName}' If condition " +
                    $"'{call.Arguments[0]}' failed: {condition.Error}");
            }

            return condition.Value;
        }

        private static void ThrowIfUnsuccessful(
            GameEventDocument document,
            EventContext context,
            EffectExecutionResult result)
        {
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
