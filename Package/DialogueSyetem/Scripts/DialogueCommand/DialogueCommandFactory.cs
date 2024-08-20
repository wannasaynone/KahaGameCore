using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.DialogueSystem.DialogueCommand
{
    public class DialogueCommandFactory : IDialogueFactory
    {
        private readonly Dictionary<string, System.Type> commandTypeMap = new Dictionary<string, System.Type>();

        public DialogueCommandFactory(bool withDefaultCommandTypes)
        {
            if (withDefaultCommandTypes)
            {
                RegisterDefaultCommandTypes();
            }
        }

        private void RegisterDefaultCommandTypes()
        {
            RegisterCommandType("Log", typeof(DialogueCommand_Log));
            RegisterCommandType("Say", typeof(DialogueCommand_Say));
            RegisterCommandType("AddOption", typeof(DialogueCommand_AddOption));
            RegisterCommandType("GoTo", typeof(DialogueCommand_GoTo));
            RegisterCommandType("SetCharacter", typeof(DialogueCommand_SetCharacter));
            RegisterCommandType("ShowCG", typeof(DialogueCommand_ShowCG));
            RegisterCommandType("HideCG", typeof(DialogueCommand_HideCG));
        }

        public void RegisterCommandType(string command, System.Type type)
        {
            if (commandTypeMap.ContainsKey(command))
            {
                Debug.LogError("Command " + command + " is already registered.");
                return;
            }

            commandTypeMap.Add(command, type);
        }

        public DialogueCommandBase CreateDialogueCommand(DialogueData dialogueData, IDialogueView dialogueView)
        {
            if (commandTypeMap.TryGetValue(dialogueData.Command, out System.Type type))
            {
                return System.Activator.CreateInstance(type, new object[] { dialogueData, dialogueView }) as DialogueCommandBase;
            }

            return new DialogueCommand_Log(dialogueData, dialogueView);
        }
    }
}