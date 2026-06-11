using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using KahaGameCore.Package.EffectProcessor;
using KahaGameCore.Package.EffectProcessor.Data;
// 本檔案位於 KahaGameCore.Package.* 之下，EffectProcessor 會優先解析為命名空間，需以別名指向類別。
using EffectProcessorEngine = KahaGameCore.Package.EffectProcessor.EffectProcessor;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    public class EffectCommandExecutor : ICommandExecutor
    {
        private const string DEFAULT_TIMING = "Execute";

        private readonly EffectCommandDeserializer deserializer;
        private readonly IGameState gameState;

        public EffectCommandExecutor(EffectCommandFactoryContainer factoryContainer, IGameState gameState)
        {
            if (factoryContainer == null) throw new ArgumentNullException(nameof(factoryContainer));
            this.gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
            deserializer = new EffectCommandDeserializer(factoryContainer);
        }

        public void Execute(string rawCommands, Action onCompleted)
        {
            if (string.IsNullOrWhiteSpace(rawCommands))
            {
                onCompleted?.Invoke();
                return;
            }

            Dictionary<string, List<EffectProcessorEngine.EffectData>> timingToData =
                deserializer.Deserialize(WrapWithDefaultTiming(rawCommands));

            EffectProcessorEngine processor = new EffectProcessorEngine();
            processor.SetUp(timingToData);

            ProcessData processData = new ProcessData
            {
                timing = DEFAULT_TIMING,
                caster = gameState.Container
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
