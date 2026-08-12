# KahaGameCore

KahaGameCore 是以 asmdef 分隔的 Unity 遊戲開發工具包，包含 Parameters、Expressions、Effects、Game Events、GameFlow、Dialogue、Persistence 與常用 UI／基礎工具。

本文件說明如何快速建立並執行預設的表驅動遊戲專案。各模組的 API 與獨立用法請查閱文末的模組文件。

## 系統需求

此專案目前使用 Unity `6000.3.8f1`，並需要：

- Input System
- Addressables
- TextMeshPro／UGUI
- UniTask（已包含在 `Assets/KahaGameCore/Plugins/UniTask`）

Dialogue 同時使用 Unity Input 與 Input System。請到：

`Edit → Project Settings → Player → Other Settings → Active Input Handling`

設為 `Both`，然後重啟 Unity。

## 安裝

1. 將完整的 `KahaGameCore` 資料夾放到：

   `Assets/KahaGameCore/`

2. 在 Package Manager 安裝 Input System、Addressables 與 UGUI。
3. 等待 Unity 完成匯入與編譯。
4. 確認 Console 沒有 compile error。

## Quick Start：建立可執行專案

### 1. 生成預設 UI 與 Scene

執行 Unity 選單：

`KahaGameCore → GameFlowSystem → Build Default UI Prefabs And Scene`

Builder 會生成：

| 路徑 | 內容 |
|---|---|
| `Assets/Scenes/GameFlowGame.unity` | 已完成組裝的可執行 Scene |
| `Assets/Resources/GameFlowUIViews/` | 主標題、HUD、選單、提示與製作名單 prefabs |

生成的 Scene 已包含 Camera、EventSystem、Canvas、DialogueView、`UserInterfaceController` 與 `DefaultGameLauncher`。

### 2. 執行

1. 開啟 `Assets/Scenes/GameFlowGame.unity`。
2. 進入 Play Mode。
3. 從主標題開始遊戲。

正常情況下可以操作行動選單、地點選單、提示與對話；HUD 會隨 Parameters 與時間階段更新。

### 3. 指定專案資料

在 Scene 中選取 `DefaultGameLauncher`，設定下列欄位：

| 欄位 | 內容 |
|---|---|
| `Game Data Tables` | `TimePhaseData.txt`、`PlayerActionData.txt`、`LocationData.txt`、`GameTextData.txt`、`DialogueData.txt` |
| `Parameter Tables` | 一或多份 `.parameters.json` |
| `Game Event Files` | `.gameevent.json` documents |
| `Scene Game Event Triggers` | 場景內需要直接執行指定事件文件的 triggers |
| `Parameter State Binders` | 依 Parameter condition 更新顯示的場景 binders |
| `Game Title` | 主標題顯示名稱 |
| `Credits Text Id` | `GameTextData.ID`，供製作名單演出使用 |

內附 SampleData 位於：

`Assets/KahaGameCore/Modules/GameFlowSystem/DefaultViews/SampleData/`

可將 SampleData 複製到專案自己的資料夾後修改，再重新指定 Launcher 欄位。不要直接修改 KahaGameCore 內的 SampleData。

### 4. 驗證資料接線

資料之間使用下列 identity 互相引用：

| 寫法 | 對應資料 |
|---|---|
| `$Spirit` | Parameter `Key` |
| `AddParameter(Spirit,-10)` | 同一個 Parameter `Key` |
| `Action:Work` | `PlayerActionData.TriggerTiming` 與 Game Event `TriggerTiming` |
| `PhaseStart:Morning` | `TimePhaseData.Key` |
| `EnterLocation:1` | `LocationData.ID` |
| `ShowHint(901)` | `GameTextData.ID` |
| `StartDialogue(1)` | `DialogueData.ID` |

如果 Action 有顯示但沒有執行效果，先確認 `TriggerTiming` 完全相同，且對應的 Game Event 已加入 `Game Event Files`。

## DefaultGameLauncher 建立的服務

`DefaultGameLauncher` 是預設 composition root。它會：

1. 載入五張 static data tables。
2. 載入 Parameter tables 並建立共用的 `ParameterStore`。
3. 建立 Effects command registry 與 runtime。
4. 建立 `GameEventCatalog` 與 `GameEventRunner`。
5. 透過 `GameFlowGameEventAdapter` 將 Game Events 接到 GameFlow。
6. 建立 `GameFlowServices`、Dialogue bridge、Presenters 與 HUD。
7. 初始化 Scene Game Event Triggers 與 Parameter State Binders。
8. 在玩家開始遊戲時重置狀態並執行 `FlowController.RunNewGameAsync(...)`。

需要替換服務、註冊自訂 Effects command、調整 HUD Parameter Keys 或修改組裝流程時，將 `DefaultGameLauncher.cs` 複製到專案 assembly，改名後修改，並替換 Scene 中的 component。不要直接修改 KahaGameCore 內的 Launcher。

## 生成內容注意事項

- 再次執行 Builder 會覆寫生成的 Scene 與 UI prefabs。
- 開始修改 UI 後，不要再次執行 Builder；或先備份生成內容。
- `DefaultGameLauncher` 的 Sample HUD 預設顯示 `Supplies`、`Satiety`、`Spirit`。改用其他 Keys 時需客製 Launcher 的 `HUD_PARAMETER_KEYS`。
- 五張 static data tables 的檔名必須與資料型別名稱相同。
- Parameter Keys 在所有載入的 Parameter tables 中必須唯一。
- 每份 Game Event 的 `DocumentGuid` 必須唯一。

## 加入存讀檔

Persistence 不會由 Builder 自動加入遊戲 UI。需要存讀檔時：

1. 使用 gameplay 已建立的同一份 `ParameterStore`。
2. 將非 Parameter 狀態註冊到 `SaveParticipantRegistry`。
3. 使用 `GameSaveSlotStore` 與 `GameSaveDocumentJsonCodec` 讀寫 slot。
4. 使用 Game Events 時，在專案自己的 Launcher 中，以同一個 `GameEventRunner` 建立 `GameSaveCoordinator`。
5. 跨 Scene Load 時實作 `IGameLoadHost`。

完整步驟見 [Persistence](Modules/Persistence/README.md)。

## 專案程式碼與 asmdef

生成並執行預設 Scene 不需要先建立專案 scripts assembly。

開始撰寫自訂 Launcher、Views、Presenters、Effects commands 或 services 時，請為專案程式建立自己的 asmdef，只加入實際使用的 KahaGameCore assembly references。完整的 GameFlow 專案 references 與組裝範例見 [GameFlowSystem 專案實作指南](Modules/GameFlowSystem/專案實作指南.md#1-遊戲程式碼-asmdef)。

## 模組文件

| 模組 | 文件 |
|---|---|
| GameFlow | [GameFlowSystem](Modules/GameFlowSystem/README.md)／[專案實作指南](Modules/GameFlowSystem/專案實作指南.md) |
| Parameters | [Parameters](Modules/Parameters/README.md) |
| Expressions | [Expressions](Modules/Expressions/README.md) |
| Effects | [Effects](Modules/Effects/README.md) |
| Game Events | [Game Events](Modules/GameEvents/README.md) |
| Persistence | [Persistence](Modules/Persistence/README.md) |
| Dialogue | [Dialogue](Modules/Dialogue/README.md) |
| Presentation | [Presentation](Modules/Presentation/README.md) |
| StaticData | [StaticData](Modules/StaticData/README.md) |
| User Interface | [UserInterfaceSystem](Modules/UserInterfaceSystem/README.md) |
| ValueContainer | [ValueContainer](Modules/ValueContainer/README.md) |
| Serialization | [Serialization](Modules/Serialization/README.md) |
| Audio | [Audio](Audio/README.md) |
| Foundation | [Foundation](Foundation/README.md) |
| UI utilities | [UITool](UITool/README.md) |
| Gradient Sprite | [GradientTextureComponent](Modules/GradientTextureComponent/README.md) |

## 測試

在 Unity Test Runner 執行 EditMode tests。各模組測試 assembly 位於自己的 `Tests/Editor`；Persistence 另提供 `Modules/Persistence/GameEventsIntegration/Samples/GameSaveTest` PlayMode sample。
