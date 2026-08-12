# Gradient Texture Component

## 用途

`GradientTextureComponent` 在 runtime 產生線性或放射狀漸層 Texture，建立 Sprite 並套用到同一個 GameObject 的 `SpriteRenderer`。產物只存在記憶體，不會寫成 asset。

## 第一次使用：產生漸層 Sprite

1. 在 GameObject 加入 `GradientTextureComponent`；`SpriteRenderer` 會由 `RequireComponent` 一起加入。
2. 在 Inspector 設定 `Gradient`、`Start Point`、`End Point`、`Texture Width` 與 `Texture Height`。
3. `Render Mode` 選擇 `Linear` 或 `Radius`。
4. 按下 Inspector 的 `Regenerate Preview` 確認結果。
5. 進入 Play Mode。

預期結果：Component 在 `Start` 產生 Texture 與 Sprite，並指定給同物件的 `SpriteRenderer`。

呼叫端程式若直接存取 component，asmdef 引用 `KahaGameCore.Modules.GradientTextureComponent`，namespace 為 `ProjectTentacle.Tools`。此模組的 asmdef 同時引用專案既有的 `SpriteBlendingMode` assembly：

```csharp
using ProjectTentacle.Tools;

GradientTextureComponent gradient =
    GetComponent<GradientTextureComponent>();

gradient.SetRenderMode(GradientRenderMode.Linear);
gradient.SetTextureWidth(512);
gradient.SetTextureHeight(256);
Texture2D texture = gradient.GenerateTexture();
```

`GenerateTexture()` 只產生或更新 Texture；Component 自己在 `Start` 建立並套用 Sprite。

## Inspector 欄位

| 欄位 | 功能 |
|---|---|
| `Gradient` | 顏色與 alpha keys。 |
| `Start Point`／`End Point` | 0–1 normalized 座標，用來決定方向與距離。 |
| `Texture Width`／`Texture Height` | 產生 Texture 的像素尺寸。 |
| `Render Mode` | `Linear` 線性漸層或 `Radius` 放射狀漸層。 |
| `Show Preview In Game Scene` | Play Mode 時用 `OnGUI` 顯示額外預覽。 |
| `Preview Size` | `OnGUI` 預覽尺寸，不改變產生的 Texture。 |

## 限制

- Texture 與 Sprite 在記憶體建立，Component 銷毀時一併清理。
- Game Scene preview 使用 `Camera.main` 與 `OnGUI`，只適合檢查，不是正式 UI。
- 啟用 `USING_URP` define 時，`GradientTextureComponent` 會要求同物件具有 `SpriteBlendingMode` component。
