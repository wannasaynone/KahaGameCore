using Cysharp.Threading.Tasks;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    /// <summary>顯示提示視窗並等待玩家確認。</summary>
    public interface IHintPresenter
    {
        UniTask ShowAsync(string text);
    }
}
