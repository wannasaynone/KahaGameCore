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

ExpressionResult<bool> result = parameters.EvaluateCondition(
    "$Supplies >= 10 && !$DoorOpen");
```

自己的 asmdef 至少需要：

```json
{
  "references": [
    "KahaGameCore.Modules.Parameters",
    "KahaGameCore.Modules.Expressions"
  ]
}
```

## 模組索引

| 模組 | 目的 | 範圍 |
|---|---|---|
| [Expressions](Modules/Expressions/README.md) | 計算式與條件式求值 | 純 runtime 求值引擎 |
| [Parameters](Modules/Parameters/README.md) | 全域、typed、可保存的內容值 | Runtime store、Expressions 求值與表格 Editor |
| [Effects](Modules/Effects/README.md) | 解析並依序執行文字效果指令 | Command codec、registry 與 runtime |
| [Game Events](Modules/GameEvents/README.md) | 依 timing、condition、priority 排隊執行 Effects | Catalog、條件篩選與 FIFO queue |
| [GameFlowSystem](Modules/GameFlowSystem/README.md) | 表驅動遊戲主循環與預設組裝 | Runtime contracts、default implementation 與 views |
| [Persistence](Modules/Persistence/README.md) | Parameters 與明確註冊 participant 的存讀檔 | Save／Load core；Scene host 由專案組裝 |
| [Presentation](Modules/Presentation/README.md) | 以 Parameter 條件控制子物件顯示 | `ParameterStateBinder` |
| [Dialogue](Modules/Dialogue/README.md) | 表驅動對話播放器 | 對話 queue、內建指令與演出 providers |
| [StaticData](Modules/StaticData/README.md) | 依資料型別保存與查詢靜態表格 | Runtime table store |
| [Serialization](Modules/Serialization/README.md) | JsonFx 的薄型讀寫 adapter | `IJsonReader`／`IJsonWriter` implementations |
| [UserInterfaceSystem](Modules/UserInterfaceSystem/README.md) | UGUI View stack 與淡入淡出 | View stack、附著 View 與轉場 |
| [ValueContainer](Modules/ValueContainer/README.md) | 可疊加角色數值與字串 key/value 契約 | `IValueContainer` 與 Caster／Target 求值 |
| [GradientTextureComponent](Modules/GradientTextureComponent/README.md) | 產生漸層 Sprite | Runtime component |
| [Audio](Audio/README.md) | BGM、SFX、白噪音與音量控制 | `AudioManager` |
| [Foundation](Foundation/README.md) | MessageBus 與共用 Unity utilities | Messaging 與 common utilities |
| [UITool](UITool/README.md) | Canvas 尺寸與世界物件 UI 跟隨 | `MainCanvas` 與 UI helpers |

`Plugins/` 是隨 KahaGameCore 放置的第三方程式，不是 KahaGameCore 自有 API；使用前仍應遵守各套件的授權與原始文件。

## 建議的核心組裝順序

```text
Parameter definitions → ParameterStore（包含參數計算與條件求值）
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

根層 `Editor/` 包含 Animator、UI template、資源搜尋與 Google Sheet JSON 工具。Google Sheet JSON converter 只處理其指定格式，不提供 Localization 或標準 CSV workflow。

## 能力邊界

KahaGameCore 的核心流程由 Expressions、Parameters、Effects、Game Events、GameFlow 與 Persistence 組成。以下能力不在公開介面內，或有明確限制：

- Persistence 的 `GameLoadCoordinator`／`IGameLoadHost` 有自動測試；ProjectTentacle 的 production Scene host adapter 由專案端提供，手動流程位於 GameSaveTest sample。
- Dialogue 以 `DialogueCommandFactoryContainer` 組裝指令；支援範圍以 Dialogue README 的內建指令清單為準，其他 command 類別可能丟出 `NotImplementedException`。
- KahaGameCore 不定義 Localization、TextGuid 或 Master CSV workflow。
- Presentation 的公開介面是 `ParameterStateBinder`，不包含 SceneObjectRegistry、Timeline、Animator 或 Camera adapter。

## 測試

在 Unity Test Runner 執行 EditMode tests。各模組測試 assembly 位於自己的 `Tests/Editor`；Persistence 的手動流程另有 `Modules/Persistence/GameEventsIntegration/Samples/GameSaveTest` PlayMode sample。
