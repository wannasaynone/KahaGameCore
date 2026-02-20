using System.Collections.Generic;
using UnityEngine;

namespace ProjectBSR.DialogueSystem
{
    public class DialogueCommandFactoryContainer
    {
        private Dictionary<string, DialogueCommandFactoryBase> commandNameToFactory = new Dictionary<string, DialogueCommandFactoryBase>();

        public void RegisterFactory(string command, DialogueCommandFactoryBase factoryBase)
        {
            if (commandNameToFactory.ContainsKey(command))
                return;

            commandNameToFactory.Add(command, factoryBase);
        }

        public DialogueCommandBase GetDialogueCommand(string commandName)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                Debug.LogError("[EffectProcesser][GetEffectCommand] commandName is null or empty");
                return null;
            }

            if (!commandNameToFactory.ContainsKey(commandName))
            {
                Debug.LogError("[EffectProcesser][GetEffectCommand] Invaild command=" + commandName);
                return null;
            }

            return commandNameToFactory[commandName].Create();
        }
    }
}