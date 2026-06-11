using Cysharp.Threading.Tasks;

namespace KahaGameCore.Package.GameFlowSystem
{
    /// <summary>執行表格中的效果指令串（KahaGameCore EffectProcessor 語法）。</summary>
    public interface IGameFlowCommandExecutor
    {
        UniTask ExecuteAsync(string rawCommands);
    }
}
