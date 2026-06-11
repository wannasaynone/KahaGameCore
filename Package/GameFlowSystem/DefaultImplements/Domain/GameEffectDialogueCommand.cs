using ProjectBSR.DialogueSystem;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    /// <summary>
    /// 對話指令「GameEffect」：在對話表中執行遊戲效果指令串。
    /// 用法：Command=GameEffect，Arg1=效果指令串（如 ShowHint(901) 或 AddValue(Spirit,10)）。
    /// 對話分支（選項跳線）即可改變遊戲狀態，不必硬寫程式。
    /// </summary>
    public class GameEffectDialogueCommand : DialogueCommandBase
    {
        public const string COMMAND_NAME = "GameEffect";

        private readonly ICommandExecutor commandExecutor;

        public GameEffectDialogueCommand(ICommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        public override void Process(string[] args, DialogueContext context)
        {
            string rawCommands = args.Length > 0 ? args[0] : string.Empty;
            commandExecutor.Execute(rawCommands, () => context.onComplete?.Invoke());
        }
    }

    public class GameEffectDialogueCommandFactory : DialogueCommandFactoryBase
    {
        private readonly ICommandExecutor commandExecutor;

        public GameEffectDialogueCommandFactory(ICommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        public override DialogueCommandBase Create()
        {
            return new GameEffectDialogueCommand(commandExecutor);
        }
    }
}
