using KahaGameCore.GameFlowSystem;

namespace KahaGameCore.GameFlowSystem.DefaultImplements
{
    /// <summary>
    /// 遊戲進行中的所有可變數值的唯一入口。
    /// 寫入時依 GameValueData 表自動鉗制上下限，並發佈 GameValueChangedEvent。
    /// ResetToInitial（依 GameValueData 表的 InitialValue 重設）繼承自 IGameFlowState。
    /// </summary>
    public interface IGameState : IGameFlowState
    {
        int Get(string tag);
        bool TryGet(string tag, out int value);
        void Add(string tag, int amount);
        void Set(string tag, int value);
    }
}
