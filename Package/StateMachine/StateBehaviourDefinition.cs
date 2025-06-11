using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using KahaGameCore.Package.EffectProcessor.ValueContainer;
using KahaGameCore.Package.EffectProcessor;

namespace Assets.Scripts.StateMachine
{
    [CreateAssetMenu(fileName = "NewBehaviour", menuName = "StateMachine/Behaviour")]
    public class StateBehaviourDefinition : ScriptableObject
    {
        public string behaviourID;
        public string behaviourName;

        [TextArea(5, 20)]
        public string behaviourContent;

        private bool isRunning = false;

        public async UniTask Execute(EffectCommandFactoryContainer effectCommandFactoryContainer, IValueContainer caster, List<IValueContainer> targets)
        {
            if (isRunning)
            {
                Debug.LogWarning("Behaviour is already running, returning.");
                return;
            }

            string fullCommand = "Execute{" + behaviourContent.ReplaceWhitespace("").Replace("\n", "") + "}";

            //Debug.Log(name + " " + fullCommand);

            EffectCommandDeserializer deserializer = new EffectCommandDeserializer(effectCommandFactoryContainer);
            Dictionary<string, List<EffectProcessor.EffectData>> effectCommands = deserializer.Deserialize(fullCommand);

            EffectProcessor effectProcessor = new EffectProcessor();
            effectProcessor.SetUp(effectCommands);

            effectProcessor.OnProcessEnded += EndRunning;

            effectProcessor.Start(new KahaGameCore.Package.EffectProcessor.Data.ProcessData
            {
                caster = caster,
                targets = targets,
                skipIfCount = 0,
                timing = "Execute"
            });

            while (isRunning)
            {
                await UniTask.Yield();
            }

            effectProcessor.OnProcessEnded -= EndRunning;
        }

        private void EndRunning()
        {
            isRunning = false;
        }
    }
}
