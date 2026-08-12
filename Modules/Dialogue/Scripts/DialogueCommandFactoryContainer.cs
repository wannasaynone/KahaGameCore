using System.Collections.Generic;
using KahaGameCore.Dialogue.DefaultImplements.Command;
using UnityEngine;

namespace KahaGameCore.Dialogue
{
    public class DialogueCommandFactoryContainer
    {
        private readonly Dictionary<string, DialogueCommandFactoryBase> commandNameToFactory =
            new Dictionary<string, DialogueCommandFactoryBase>();

        public static DialogueCommandFactoryContainer CreateDefault()
        {
            DialogueCommandFactoryContainer container = new DialogueCommandFactoryContainer();
            container.RegisterFactory("Say", new SayFactory());
            container.RegisterFactory("BlackIn", new BlackInFactory());
            container.RegisterFactory("BlackOut", new BlackOutFactory());
            container.RegisterFactory("AddOption", new AddOptionFactory());
            container.RegisterFactory("ShowOptions", new ShowOptionsFactory());
            container.RegisterFactory("GoToLine", new GoToLineFactory());
            container.RegisterFactory("ShowFullScreenImage", new ShowFullScreenImageFactory());
            container.RegisterFactory("HideFullScreenImage", new HideFullScreenImageFactory());
            container.RegisterFactory("HideDialogueBox", new HideDialogueBoxFactory());
            container.RegisterFactory("PlaySoundEffect", new PlaySoundEffectFactory());
            container.RegisterFactory("PlayBackgroundMusic", new PlayBackgroundMusicFactory());
            container.RegisterFactory("ShowCharacter", new ShowCharacterFactory());
            container.RegisterFactory("HideCharacter", new HideCharacterFactory());
            container.RegisterFactory("ChangeCharacter", new ChangeCharacterFactory());
            container.RegisterFactory("MoveCharacterX", new MoveCharacterXFactory());
            container.RegisterFactory("MoveCharacterY", new MoveCharacterYFactory());
            container.RegisterFactory("CharacterJump", new CharacterJumpFactory());
            container.RegisterFactory("ScaleCharacter", new ScaleCharacterFactory());
            return container;
        }

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
