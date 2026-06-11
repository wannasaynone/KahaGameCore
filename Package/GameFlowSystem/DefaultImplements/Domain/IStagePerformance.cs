using Cysharp.Threading.Tasks;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    /// <summary>
    /// 「演出」接口：所有 UGUI 動畫演出（換日、丈夫外出、物資增加、製作名單……）皆實作本介面，
    /// 並以演出 ID 註冊到 IPerformancePlayer。表格中以 ID 字串引用，不寫死。
    /// </summary>
    public interface IStagePerformance
    {
        UniTask PlayAsync();
    }
}
