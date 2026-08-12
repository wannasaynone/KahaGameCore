# UserInterfaceSystem

## 目的

UserInterfaceSystem 提供 `AView` 淡入淡出生命週期，以及 `UserInterfaceController` 的主 View stack、附加 View、返回鍵與黑幕控制。

## 快速開始

1. 建立繼承 `AView` 的 View prefab，指定同物件的 `CanvasGroup`。
2. 把 prefab 放在 `Resources`，例如 `Resources/UI/InventoryView.prefab`。
3. 場景建立 `UserInterfaceController`，指定 `uiRoot` 與 `blackoutOverlay`。
4. 推入與移除 View：

```csharp
InventoryView view = await uiController.PushView<InventoryView>(
    "UI/InventoryView",
    created => created.Bind(inventory));

await uiController.PopView();
```

已有實例時可用 `PushView(view)`。`AttachView` 會把 HUD／overlay 附著在目前 stack entry；主 View 被隱藏或移除時，附著 View 會一起處理。

覆寫返回行為：

```csharp
public override BackButtonResult OnBackButtonPressed()
{
    return hasUnsavedChanges
        ? BackButtonResult.DoNothing
        : BackButtonResult.Close;
}
```

## 注意事項

- `AView` 必須有有效的 `CanvasGroup` reference。
- `PushView<T>(path)` 使用 `Resources.Load<T>`；路徑不含 `Resources/` 與副檔名。
- `HandleBackButton` 只在 stack 超過一層時自動 Pop；根 View 的關閉由專案決定。
- API 回傳 `System.Threading.Tasks.Task`，內部 transition 使用 UniTask。

