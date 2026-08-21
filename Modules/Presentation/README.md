# Presentation

## 用途

Presentation 提供 `ParameterStateBinder`：以 Expressions condition 監聽 `ParameterStore`，自動切換同一根物件之下的目標子物件。它適合門、機關、階段外觀等由語意狀態推導的顯示。

## 第一次使用：依 MachineStage 切換外觀

呼叫端 asmdef 引用 `KahaGameCore.Modules.Presentation`、`KahaGameCore.Modules.Parameters` 與 `KahaGameCore.Modules.Expressions`。開始前，`ParameterStore` 必須包含 Key 為 `MachineStage` 的 Int definition。

1. 在 Hierarchy 對共同父物件按右鍵，選擇
   `Kaha Game Core > Add Parameter State Binder`，建立掛有
   `ParameterStateBinder` 的 `Parameter State Binder` 子物件。
2. 在 Inspector 的 bindings 為每個子物件指定 condition，例如 `$MachineStage == 0`、`$MachineStage >= 1`。
3. 建立 `ParameterStore` 後，由場景 composition root 初始化：

```csharp
foreach (ParameterStateBinder binder in parameterStateBinders)
{
    binder.Initialize(parameters);
}
```

Inspector 的條件編輯器與 Game Event Editor 共用目前事件目錄的參數索引與
AND／OR 結構化填寫介面。若事件目錄或可用條件參數尚未設定，Inspector 會提供
按鈕開啟 Game Event Editor，完成初始化後才能新增綁定。
條件下拉中的「新增參數」會直接開啟小面板；新增後立即儲存參數表、刷新索引並
選取新參數。

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

預期結果：初始化時立即依目前的 `MachineStage` 切換物件；之後呼叫 `parameters.Set("MachineStage", value)` 會再次求值。Binder 使用的是 Parameter Key，不是 DisplayName。

## 限制

- Target 必須是 binder 根物件的 descendant，且同一 target 只能綁一次。
- 初始化後不可重新 `Configure`。
- 語法錯誤、unknown Parameter 或非 Bool 結果會明確失敗。
- 不要把 target active flag 存檔；載入 Parameters 並初始化 binder 後重新推導。
- 本模組沒有 SceneObjectRegistry、Timeline、Animator 或 Camera adapter。
