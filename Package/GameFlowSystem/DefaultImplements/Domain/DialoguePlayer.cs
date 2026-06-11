using System;
using Cysharp.Threading.Tasks;
using KahaGameCore.GameData.Implemented;
using ProjectBSR.DialogueSystem;
using ProjectBSR.DialogueSystem.DefaultImplements.Command;
using ProjectBSR.DialogueSystem.View;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    /// <summary>
    /// KahaGameCore DialogueManager 的包裝：
    /// 1. 補上 UniTask 等待介面；2. 在預設指令之外註冊本專案的 GameEffect 橋接指令，
    ///    讓對話分支可以直接執行效果指令串（例如選項失敗後顯示提示）。
    /// </summary>
    public class DialoguePlayer : IDialoguePlayer
    {
        private readonly DialogueManager dialogueManager;
        private readonly DialogueView dialogueView;

        public DialoguePlayer(DialogueView dialogueView, GameStaticDataManager staticDataManager, ICommandExecutor commandExecutor)
        {
            this.dialogueView = dialogueView ? dialogueView : throw new ArgumentNullException(nameof(dialogueView));

            DialogueCommandFactoryContainer factoryContainer = CreateFactoryContainerWithDefaults();
            factoryContainer.RegisterFactory(GameEffectDialogueCommand.COMMAND_NAME, new GameEffectDialogueCommandFactory(commandExecutor));

            dialogueManager = new DialogueManager(dialogueView, staticDataManager, factoryContainer);
        }

        public UniTask PlayAsync(int dialogueId)
        {
            UniTaskCompletionSource completionSource = new UniTaskCompletionSource();
            dialogueView.gameObject.SetActive(true);
            dialogueManager.StartDialogue(dialogueId, () => completionSource.TrySetResult());
            return completionSource.Task;
        }

        /// <summary>
        /// DialogueManager 在外部提供容器時不會註冊內建指令，因此這裡照原始清單補齊。
        /// </summary>
        private static DialogueCommandFactoryContainer CreateFactoryContainerWithDefaults()
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
    }
}
