namespace KahaGameCore.Package.GameFlowSystem
{
    /// <summary>流程系統對遊戲可變狀態的唯一要求：開新遊戲時能重設回初始值。</summary>
    public interface IGameFlowState
    {
        void ResetToInitial();
    }
}
