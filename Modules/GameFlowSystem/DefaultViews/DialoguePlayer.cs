using System;
using Cysharp.Threading.Tasks;
using KahaGameCore.StaticData;
using KahaGameCore.GameFlowSystem.DefaultImplements;
using KahaGameCore.Dialogue;
using KahaGameCore.Dialogue.View;

namespace KahaGameCore.GameFlowSystem.DefaultViews
{
    /// <summary>
    /// 【範例橋接】本檔屬於 DefaultViews 範例層，把 DefaultImplements 與具體 Dialogue Module 接起來。
    /// 各專案請複製一份到自己的 Module，並依需求修改。
    ///
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

            DialogueCommandFactoryContainer factoryContainer = DialogueCommandFactoryContainer.CreateDefault();
            factoryContainer.RegisterFactory(GameEffectDialogueCommand.COMMAND_NAME, new GameEffectDialogueCommandFactory(commandExecutor));

            dialogueManager = new DialogueManager(dialogueView, staticDataManager, factoryContainer);
        }

        public UniTask PlayAsync(int dialogueId)
        {
            UniTaskCompletionSource completionSource = new UniTaskCompletionSource();
            dialogueView.gameObject.SetActive(true);
            // 範例橋接無 cinematic 裝飾層收尾，故自行在對話完成時收掉視圖（與上方 SetActive(true) 對稱）。
            dialogueManager.StartDialogue(dialogueId, () =>
            {
                dialogueView.gameObject.SetActive(false);
                completionSource.TrySetResult();
            });
            return completionSource.Task;
        }
    }
}
