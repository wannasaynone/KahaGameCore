using System.Collections.Generic;

namespace KahaGameCore.Combat.Processor.EffectProcessor
{
    public class EffectCommandFactoryContainer
    {
        private Dictionary<string, EffectCommandFactoryBase> m_commandNameToFactory = new Dictionary<string, EffectCommandFactoryBase>();

        public void RegisterFactory(string command, EffectCommandFactoryBase factoryBase)
        {
            if (m_commandNameToFactory.ContainsKey(command))
                return;

            m_commandNameToFactory.Add(command, factoryBase);
        }

        public EffectCommandBase GetEffectCommand(string commandName)
        {
            if (!m_commandNameToFactory.ContainsKey(commandName))
            {
                UnityEngine.Debug.LogError("[EffectProcesser][GetEffectCommand] Invaild command=" + commandName);
                return null;
            }

            return m_commandNameToFactory[commandName].Create();
        }
    }
}
