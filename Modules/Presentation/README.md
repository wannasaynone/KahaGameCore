# Presentation

## 目的

Presentation 目前只提供 `ParameterStateBinder`：以 Expressions condition 監聽 `ParameterStore`，自動切換同一根物件之下的目標子物件。它適合門、機關、階段外觀等由語意狀態推導的顯示。

## 快速開始

1. 在共同父物件加入 `ParameterStateBinder`。
2. 在 Inspector 的 bindings 為每個子物件指定 condition，例如 `$MachineStage == 0`、`$MachineStage >= 1`。
3. 建立 `ParameterStore` 後，由場景 composition root 初始化：

```csharp
foreach (ParameterStateBinder binder in parameterStateBinders)
{
    binder.Initialize(parameters);
}
```

也可以在初始化前以程式設定：

```csharp
binder.Configure(new[]
{
    new ParameterChildConditionBinding(idleObject, "$MachineStage == 0"),
    new ParameterChildConditionBinding(activeObject, "$MachineStage >= 1")
});
binder.Initialize(parameters);
```

初始化會立即評估；之後任何 Parameter change 都會重新評估所有 bindings。每個 condition 獨立，所以可以同時啟用多個目標。

## 限制

- Target 必須是 binder 根物件的 descendant，且同一 target 只能綁一次。
- 初始化後不可重新 `Configure`。
- 語法錯誤、unknown Parameter 或非 Bool 結果會明確失敗。
- 不要把 target active flag 存檔；載入 Parameters 並初始化 binder 後重新推導。
- 本模組沒有 SceneObjectRegistry、Timeline、Animator 或 Camera adapter。

