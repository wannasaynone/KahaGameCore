using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;
using KahaGameCore.Effects.Data;

namespace KahaGameCore.GameFlowSystem.DefaultImplements
{
    public class EffectCommandExecutor : ICommandExecutor
    {
        private const string DEFAULT_TIMING = "Execute";

        private readonly EffectCommandDeserializer deserializer;
        public EffectCommandExecutor(EffectCommandFactoryContainer factoryContainer)
        {
            if (factoryContainer == null) throw new ArgumentNullException(nameof(factoryContainer));
            deserializer = new EffectCommandDeserializer(factoryContainer);
        }

        public void Execute(string rawCommands, Action onCompleted)
        {
            if (string.IsNullOrWhiteSpace(rawCommands))
            {
                onCompleted?.Invoke();
                return;
            }

            Dictionary<string, List<EffectProcessor.EffectData>> timingToData =
                deserializer.Deserialize(WrapWithDefaultTiming(rawCommands));

            EffectProcessor processor = new EffectProcessor();
            processor.SetUp(timingToData);

            ProcessData processData = new ProcessData
            {
                timing = DEFAULT_TIMING
            };

            processor.Start(
                processData,
                onEnded: () =>
                {
                    processor.Dispose();
                    onCompleted?.Invoke();
                },
                onQuitted: () =>
                {
                    processor.Dispose();
                    onCompleted?.Invoke();
                });
        }

        public UniTask ExecuteAsync(string rawCommands)
        {
            UniTaskCompletionSource completionSource = new UniTaskCompletionSource();
            Execute(rawCommands, () => completionSource.TrySetResult());
            return completionSource.Task;
        }

        private static string WrapWithDefaultTiming(string rawCommands)
        {
            return rawCommands.Contains("{")
                ? rawCommands
                : DEFAULT_TIMING + "{" + rawCommands + "}";
        }
    }
}
