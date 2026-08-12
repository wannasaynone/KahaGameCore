# UITool

## 目的

UITool 提供兩類小型 UGUI 元件：依畫面比例調整 `CanvasScaler`，以及讓 UI 跟隨世界空間 GameObject。

## 快速開始：畫面比例

在含 `CanvasScaler` 的 Canvas 加入 `ScreenSizeRateSetter`。`Awake` 會比較實際寬高縮放比，自動把 `matchWidthOrHeight` 設為 0 或 1。

## 快速開始：跟隨世界物件

1. 建立主 Canvas，將 tag 設為 `MainCanvas`。
2. 確認場景的主攝影機有 `MainCamera` tag。
3. 在要跟隨的 UI 物件加入 `TrackCharacterUIBase`，指定 `target` 與 offset。

```csharp
tracker.target = character.gameObject;
tracker.ForceUpdate();
```

元件會在 `Awake` 把自己移到主 Canvas 下，之後每幀以 `Camera.main.WorldToViewportPoint` 更新 anchored position。

## 注意事項

- `MainCanvas` 是 lazy static lookup；第一次存取時場景必須已有帶 `MainCanvas` tag 的 RectTransform。
- 切換 Canvas 的場景後，現有 static instance 不會自動重新搜尋。
- 此模組只處理位置與 scaler，不管理 View stack；View 導航請使用 UserInterfaceSystem。

