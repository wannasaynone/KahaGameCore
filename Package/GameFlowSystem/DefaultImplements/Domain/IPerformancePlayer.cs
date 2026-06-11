using Cysharp.Threading.Tasks;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    /// <summary>依演出 ID 播放演出；未註冊的 ID 以佔位行為（Log + 短暫停頓）代替。</summary>
    public interface IPerformancePlayer
    {
        void Register(string performanceId, IStagePerformance performance);
        UniTask PlayAsync(string performanceId);
    }
}
