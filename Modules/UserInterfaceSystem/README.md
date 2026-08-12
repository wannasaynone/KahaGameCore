# UserInterfaceSystem

## 用途

UserInterfaceSystem 提供 `AView` 淡入淡出生命週期，以及 `UserInterfaceController` 的主 View stack、附加 View、返回鍵與黑幕控制。

## 第一次使用：Push 一個 View

呼叫端 asmdef 引用 `KahaGameCore.Modules.UserInterfaceSystem`。先建立一個 View script：

```csharp
using KahaGameCore.UserInterfaceSystem;

public sealed class InventoryView : AView
{
    public void Bind()
    {
        // 將專案資料顯示到 UI。
    }
}
```

1. 建立含 `InventoryView` 與 `CanvasGroup` 的 prefab，並把 `AView.Canvas Group` 指向同物件的 CanvasGroup。
2. 將 prefab 放到 `Assets/Resources/UI/InventoryView.prefab`。
3. 場景建立 `UserInterfaceController`，指定 `Ui Root` 與 `Blackout Overlay`。
4. 由 presenter 或 composition root Push／Pop：

```csharp
InventoryView view = await uiController.PushView<InventoryView>(
    "UI/InventoryView",
    created => created.Bind());

await uiController.PopView();
```

預期結果：Push 時目前 View 淡出，InventoryView 在 `uiRoot` 下生成並淡入；Pop 時它淡出並銷毀，上一層 View 再顯示。

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

## Stack 行為與限制

- `AView` 必須有有效的 `CanvasGroup` reference。
- `PushView<T>(path)` 使用 `Resources.Load<T>`；路徑不含 `Resources/` 與副檔名。
- `HandleBackButton` 只在 stack 超過一層時自動 Pop；根 View 的關閉由專案決定。
- API 回傳 `System.Threading.Tasks.Task`，內部 transition 使用 UniTask。
