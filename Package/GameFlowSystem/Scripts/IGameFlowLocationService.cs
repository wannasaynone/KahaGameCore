namespace KahaGameCore.Package.GameFlowSystem
{
    /// <summary>地點服務。流程系統只需要知道目前地點，用於行動清單與 EnterLocation 事件。</summary>
    public interface IGameFlowLocationService
    {
        int CurrentLocationID { get; }
    }
}
