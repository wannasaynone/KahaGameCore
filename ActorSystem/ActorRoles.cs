namespace KahaGameCore.ActorSystem
{
    // 跨專案的 action 協調基礎：action 以「角色介面」對外宣告自己的能力/狀態，
    // 由 actor 層的泛型 helper（AGameActor.ActivateActionsWith / DeactivateActionsWith /
    // IsAnyActionWithRole / TryGetRole）統一消費，避免 action 互相引用具體型別。
    //
    // 新增一個跨 action 互動的標準步驟：
    //   1. 定一個介面，extends 下列三種 base 之一（依語意：查詢 / 啟動 / 停用）。
    //   2. 在相關 action 上實作該介面（action 只「宣告角色」，不引用其他具體 action 型別）。
    //   3. 在 actor / orchestrator 層呼叫對應的泛型 helper 消費。不需新增 actor 方法。
    //
    // base 階層的作用：把每個角色「綁定到它合法的操作」——泛型 helper 以 where 約束對應 base，
    // 讓「拿狀態去停用」「拿讓位角色去啟動」這類語意錯誤在編譯期就被擋下。

    /// <summary>所有 action 協調角色的根標記。</summary>
    public interface IActorRole { }

    /// <summary>查詢類：宣告「我（在 active 期間）處於某狀態」。由 IsAnyActionWithRole / TryGetRole 查詢。</summary>
    public interface IActorState : IActorRole { }

    /// <summary>啟動類：宣告「actor 因某事件可啟動我」。由 ActivateActionsWith 啟動。</summary>
    public interface IActorActivatable : IActorRole { }

    /// <summary>停用類：宣告「某情況下（如被接管）停用我」。由 DeactivateActionsWith 停用。</summary>
    public interface IActorYieldable : IActorRole { }
}
