# KahaGameCore 使用手冊

## 目的

KahaGameCore 是放在 Unity `Assets/` 下、以 asmdef 分隔的遊戲功能模組集合。各模組可個別引用；不要讓專案程式直接依賴未使用的整包功能。

## 快速開始

1. 確認 `Assets/KahaGameCore` 已存在於專案，等待 Unity 完成編譯。
2. 在自己的 asmdef 加入要使用的 KahaGameCore assembly reference。
3. 從下表進入對應模組 README，依該模組的「快速開始」組裝。
4. Parameters、Effects、Game Events、Persistence 等純 runtime 物件由專案 composition root 建立並注入；不要用場景掃描或自行新增 static locator。

最小的參數與條件式範例：

```csharp
using KahaGameCore.Expressions;
using KahaGameCore.Parameters;

var parameters = new ParameterStore(new[]
{
    ParameterDefinition.Int("Supplies", "物資", 10, 0, 999),
    ParameterDefinition.Bool("DoorOpen", "門已開啟", false)
});

parameters.Add("Supplies", 5);

var expressions = new Expressions();
var context = new ParameterExpressionContext(parameters);
ExpressionResult<bool> result = expressions.EvaluateCondition(
    "$Supplies >= 10 && !$DoorOpen",
    context);
```

自己的 asmdef 至少需要：

```json
{
  "references": [
    "KahaGameCore.Modules.Parameters",
    "KahaGameCore.Modules.Expressions",
    "KahaGameCore.Modules.Expressions.Parameters"
  ]
}
```

## 模組索引

| 模組 | 目的 | 狀態 |
|---|---|---|
| [Expressions](Modules/Expressions/README.md) | 計算式與條件式求值 | 已完成 |
| [Parameters](Modules/Parameters/README.md) | 全域、typed、可保存的內容值 | 已完成 |
| [Effects](Modules/Effects/README.md) | 解析並依序執行文字效果指令 | 已完成 |
| [Game Events](Modules/GameEvents/README.md) | 依 timing、condition、priority 排隊執行 Effects | 已完成 |
| [GameFlowSystem](Modules/GameFlowSystem/README.md) | 表驅動遊戲主循環與預設組裝 | 已完成 |
| [Persistence](Modules/Persistence/README.md) | Parameters 與明確註冊 participant 的存讀檔 | Save 已驗收；Load core 未人工驗收 |
| [Presentation](Modules/Presentation/README.md) | 以 Parameter 條件控制子物件顯示 | 只包含 Parameter binder |
| [Dialogue](Modules/Dialogue/README.md) | 舊表驅動對話播放器 | Legacy；未完成重構 |
| [StaticData](Modules/StaticData/README.md) | 依資料型別保存與查詢靜態表格 | 現有工具 |
| [Serialization](Modules/Serialization/README.md) | JsonFx 的薄型讀寫 adapter | 現有工具 |
| [UserInterfaceSystem](Modules/UserInterfaceSystem/README.md) | UGUI View stack 與淡入淡出 | 現有工具 |
| [ValueContainer](Modules/ValueContainer/README.md) | 舊數值容器契約 | Legacy interface |
| [GradientTextureComponent](Modules/GradientTextureComponent/README.md) | 產生漸層 Sprite | 現有元件 |
| [Audio](Audio/README.md) | BGM、SFX、白噪音與音量控制 | 現有元件 |
| [Foundation](Foundation/README.md) | MessageBus 與共用 Unity utilities | 現有工具 |
| [UITool](UITool/README.md) | Canvas 尺寸與世界物件 UI 跟隨 | 現有工具 |

`Plugins/` 是隨 KahaGameCore 放置的第三方程式，不是 KahaGameCore 自有 API；使用前仍應遵守各套件的授權與原始文件。

## 建議的核心組裝順序

```text
Parameter definitions → ParameterStore
                              ↓
Expressions + ParameterExpressionContext
                              ↓
EffectCommandRegistry → EffectRuntime
                              ↓
GameEventCatalog → GameEventRunner
                              ↓
GameFlow adapter / Scene trigger / Save coordinator
```

- Parameters 保存權威語意狀態。
- Expressions 只求值，不修改狀態。
- Effects 執行已註冊 command，不擁有跨事件 queue。
- Game Events 擁有 timing 過濾、condition snapshot、priority 與 FIFO queue。
- GameFlow、場景物件與存讀檔只透過上述公開 seam 組裝。

## Editor 工具

- `KahaGameCore/Parameters/Parameter Table Editor`：建立與驗證 `.parameters.json`。
- `KahaGameCore/GameFlowSystem/Build Default UI Prefabs And Scene`：生成 GameFlow 範例 UI 與場景；重新執行會覆寫生成內容。

根層 `Editor/` 還包含 Animator、UI template、資源搜尋與舊 Google Sheet 工具。這些工具不是新版 Localization workflow；不要把 Google Sheet JSON converter 當成標準 CSV parser。

## 能力邊界

本輪已交付的核心是 Expressions、Parameters、Effects、Game Events、GameFlow，以及 Save。以下不是已完成能力：

- Persistence 的 `GameLoadCoordinator`／`IGameLoadHost` 已實作並有自動測試，但尚未人工驗收，也沒有 ProjectTentacle production Scene host adapter。
- Dialogue 仍使用 `DialogueCommandFactoryContainer` 舊架構；部分舊 command 會丟出 `NotImplementedException`。
- Localization 沒有新版 TextGuid 或 Master CSV workflow。
- Presentation 沒有 SceneObjectRegistry、Timeline、Animator 或 Camera adapter；目前只有 `ParameterStateBinder`。

因此應說「已交付模組可使用」，不要把整份舊重構藍圖描述成全部完成。

## 測試

在 Unity Test Runner 執行 EditMode tests。各模組測試 assembly 位於自己的 `Tests/Editor`；Persistence 的手動流程另有 `Modules/Persistence/GameEventsIntegration/Samples/GameSaveTest` PlayMode sample。
