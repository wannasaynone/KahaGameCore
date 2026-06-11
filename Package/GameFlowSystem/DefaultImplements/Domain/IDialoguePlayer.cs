using Cysharp.Threading.Tasks;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    /// <summary>以可等待的方式播放一段劇情對話（DialogueData 表的 ID）。</summary>
    public interface IDialoguePlayer
    {
        UniTask PlayAsync(int dialogueId);
    }
}
