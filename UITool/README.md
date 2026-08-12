# UITool

## 用途

UITool 提供兩類小型 UGUI 元件：依畫面比例調整 `CanvasScaler`，以及讓 UI 跟隨世界空間 GameObject。

## 第一次使用：依畫面比例調整 Canvas

在含 `CanvasScaler` 的 Canvas 加入 `ScreenSizeRateSetter`。進入 Play Mode 後，`Awake` 會比較實際寬高縮放比，自動把 `matchWidthOrHeight` 設為 0 或 1。

預期結果：寬度較吃緊時以寬度匹配，高度較吃緊時以高度匹配。只在 Inspector 掛 component、不進入 Play Mode，不會執行 `Awake`。

## 第一次使用：讓 UI 跟隨世界物件

1. 建立主 Canvas，將 tag 設為 `MainCanvas`。
2. 確認場景的主攝影機有 `MainCamera` tag。
3. 在要跟隨的 UI 物件加入 `TrackCharacterUIBase`，指定 `target` 與 offset。

```csharp
using KahaGameCore.UITool;

TrackCharacterUIBase tracker =
    GetComponent<TrackCharacterUIBase>();
tracker.target = character.gameObject;
tracker.ForceUpdate();
```

`character` 是專案場景中的角色 component。Tracker 會在 `Awake` 把自己移到主 Canvas 下，之後每幀以 `Camera.main.WorldToViewportPoint` 更新 anchored position。

預期結果：UI 物件每幀跟隨 `character` 的螢幕位置。呼叫端 asmdef 引用 `KahaGameCore.UITool`。

## 注意事項

- `MainCanvas` 是 lazy static lookup；第一次存取時場景必須已有帶 `MainCanvas` tag 的 RectTransform。
- 切換 Canvas 的場景後，已建立的 static instance 不會自動重新搜尋。
- 此模組只處理位置與 scaler，不管理 View stack；View 導航請使用 UserInterfaceSystem。
