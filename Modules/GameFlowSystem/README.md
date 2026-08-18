# GameFlowSystem

## 用途

表驅動遊戲主流程包。提供固定的流程骨架，所有劇情、條件與數值變化都由各專案的表格定義，包內不含任何劇情內容。

## 第一次使用：生成並執行 Sample

1. 完成 [專案實作指南的前置設定](專案實作指南.md#0-前置包套件與專案設定)。
2. 執行 `KahaGameCore → GameFlowSystem → Build Default UI Prefabs And Scene`。
3. 開啟 `Assets/Scenes/GameFlowGame.unity` 並進入 Play Mode。

預期結果：從主標題開始後可操作行動與地點選單，HUD 會隨 Parameters、Phase 與 Location 更新。這條路徑會一次驗證資料載入、Game Events、Effects、Dialogue、Presentation 與 UI 組裝。

確認 Sample 能執行後再選擇：

- 沿用 DefaultImplements／DefaultViews：閱讀下方「在專案中組裝預設實作」。
- 完全自有 UI 或 domain services：跳到「接入核心介面」。
- 需要逐檔建立 View、Presenter、表格與 Scene：閱讀 [專案實作指南](專案實作指南.md)。

包分五層：

- **`Scripts/`（核心）**：`GameFlowController` 與 7 個最小介面，只依賴 UniTask。
- **`DefaultImplements/`（預設實作）**：Parameters adapter、TimeService、LocationService、效果指令、條件式與表格定義，加上 `GameFlowSystemBuilder`。不依賴 GameEvents 或 Dialogue。
- **`GameEventsIntegration/`（可選整合）**：`GameFlowGameEventAdapter`；這是 GameFlow core 與 GameEvents 唯一互相認識的位置。
- **組裝邊界**：GameFlow 不再提供跨模組 Data Catalog。`DefaultGameLauncher` 直接持有五張 Flow tables 與一個 `GameEventCatalogAsset`；Game Event authoring 設定由該 Catalog 自己擁有。
- **`DefaultViews/`（預設 UI＋範例橋接）**：整套 UGUI View / Presenter / `DefaultGameLauncher`（組裝根）腳本＋`DefaultUiBuilder`（Editor 選單一鍵生成全部 prefab 與可運行場景），**並含範例對話橋接 `DialoguePlayer` + `GameEffectDialogueCommand`**（連接具體 Dialogue Module）。純腳本、零美術資產——prefab 在各專案內按需生成。

> **對話橋接屬於範例／各專案的程式碼，不是 Module 的共用層。** 核心與 DefaultImplements 刻意與 Dialogue Module 無關（只認 `IDialoguePlayer`）。`DefaultViews` 內附一份範例橋接示範如何接上 Dialogue Module；**各專案請複製一份到自己的組件並依需求修改**。

> **完整組裝方式請直接看 [`專案實作指南.md`](專案實作指南.md)**——含一鍵生成路線、表格規格全文、手動實作 UI 的完整程式碼與 prefab 結構規格、疑難排解。

## 在專案中組裝預設實作

```csharp
// 1. GameFlow 明確持有自己的五張 Flow tables；Game Events 使用專用 Catalog。
var staticDataManager = new GameStaticDataManager();
var handler = new TextAssetJsonStaticDataHandler(new[]
{
    timePhaseData, playerActionData, locationData, gameTextData, dialogueData
});
GameFlowSystemBuilder.LoadDefaultTables(staticDataManager, handler);
staticDataManager.Add<DialogueData>(handler); // 對話表另外加

// 可載入多張 .parameters.json 大表；每張表含多列 Parameter。
var parameterCodec = new ParameterTableJsonCodec();
ParameterDefinition[] parameterDefinitions = gameEventCatalogAsset.ParameterTables
    .Select(tableAsset => parameterCodec.Read(tableAsset.text))
    .SelectMany(table => table.Definitions)
    .ToArray();
var parameters = new ParameterStore(parameterDefinitions);
var eventCodec = new GameEventDocumentJsonCodec();
var eventCatalog = new GameEventCatalog(gameEventCatalogAsset, eventCodec);
GameEventRunner eventRunner = null;

// 2. 組裝（UI 層與對話系統是各專案的演出資產，由外部提供；其餘全用預設）
//    對話播放器由工廠提供（本套件不相依具體對話系統）；DialoguePlayer 是各專案自備的橋接，
//    可從 DefaultViews 的範例橋接複製一份到自己的組件再修改。
GameFlowServices services = new GameFlowSystemBuilder(staticDataManager, parameters)
    .WithDialoguePlayerFactory(cmdExec =>               // 必要（或 OverrideDialoguePlayer）
        new DialoguePlayer(dialogueView, staticDataManager, cmdExec))
    .WithActionMenuPresenter(actionMenuPresenter)       // 必要
    .WithHintPresenter(hintPresenter)                   // 表格有用 ShowHint 才需要
    .WithLocationMenuPresenter(locationMenuPresenter)   // 表格有用 OpenLocationMenu 才需要
    .WithEventTriggerFactory(effectRuntime =>           // 必要；共用 Builder 建立的唯一 runtime
    {
        eventRunner = new GameEventRunner(eventCatalog, effectRuntime, parameters, eventCodec);
        return new GameFlowGameEventAdapter(eventRunner);
    })
    .Build();

// 3. 啟動流程
flowCts = new CancellationTokenSource();
services.FlowController.RunNewGameAsync(flowCts.Token).Forget();
```

有新需求時，實作對應介面後以 `Override` 系列方法傳入，其餘維持預設：

```csharp
var services = new GameFlowSystemBuilder(staticDataManager, parameters)
    .WithDialoguePlayerFactory(cmdExec => new DialoguePlayer(dialogueView, staticDataManager, cmdExec))
    .WithActionMenuPresenter(actionMenuPresenter)
    .WithEventTriggerFactory(CreateEventAdapter)         // 同上：用 Builder 的 EffectRuntime 建 runner
    .OverrideTimeService(new MyRealTimeService())        // 例：改用真實時間制
    .OverrideConditionEvaluator(new MyLuaEvaluator())    // 例：改用 Lua 條件式
    .AddCommandRegistration(registry => registry.Register( // 例：追加專案自訂效果指令
        new EffectCommandDefinition(
            name: "MyCommand",
            displayName: "My Command",
            category: "Project",
            parameters: System.Array.Empty<EffectCommandParameterDefinition>(),
            command: new MyCommand())))
    .Build();
```

`GameFlowServices` 會回傳所有組好的服務（Parameters、TimeService、LocationService、TriggerService、FlowController、CommandRegistry、EffectRuntime…）。開新局先呼叫 `services.ResetForNewGame()`，由各 owner 重置自己的狀態。

## 預設實作內容（DefaultImplements）

| 區塊 | 內容 |
|---|---|
| `Data/` | 四張表的資料類別：TimePhaseData、PlayerActionData、LocationData、GameTextData（JSON 陣列）；Parameters 使用可多份的 `.parameters.json` 大表。 |
| `DataAccess/` | `ResourcesJsonStaticDataHandler`（Resources/GameData/{類別名}.txt）與 `TextAssetJsonStaticDataHandler`（Inspector 手動指定，檔名=型別名） |
| `Domain/` | GameFlowExpressions、TimeService、LocationService、PlayerActionProvider、EffectCommandExecutor、GameTextProvider、`IDialoguePlayer`、PerformanceRegistry、EffectCommandRegistrar |
| `Domain/Commands/` | 內建效果指令：AddParameter、SetParameter、AdvancePhase、SetPhase、MoveToLocation、StartDialogue、ShowHint、Monologue、PlayPerformance、OpenLocationMenu、ReturnToTitle、Wait |
| `Domain/Events/` | MessageBus 訊息：GameValueChanged、TimePhaseChanged、LocationChanged、MonologueRequested、ReturnToTitleRequested |
| `GameFlowSystemBuilder.cs` | 組裝器與 `GameFlowServices`；對話播放器以 `WithDialoguePlayerFactory(Func<ICommandExecutor, IDialoguePlayer>)` 注入 |

## 對話橋接（範例／各專案自備）

對話橋接**不是 Module 的共用實作**，而是範例與各專案各自擁有的程式碼，讓 DefaultImplements 保持與 Dialogue Module 無關。`DefaultViews` 內附一份範例：

| 內容 | 說明 |
|---|---|
| `DialoguePlayer` | 包裝 Dialogue Module 的 `DialogueManager`/`DialogueView`，使用 Dialogue 提供的預設 command container，補上 UniTask 等待介面並註冊 GameEffect adapter。建構子 `(DialogueView, GameStaticDataManager, ICommandExecutor)`，實作 `IDialoguePlayer` |
| `GameEffectDialogueCommand` | GameFlow 擁有的 Dialogue command adapter；把 `Arg1` 的 Effects command 字串交給共用 `ICommandExecutor`，完成後才讓 DialogueManager 推進下一行 |

`GameEffect` 不是 Dialogue 內建指令，也不是 `IEffectCommand`。它只在 `DialoguePlayer` 組裝時追加到 Dialogue command container：

```text
DialogueData（Command=GameEffect，Arg1=AddParameter(Spirit,10)）
→ DialogueManager
→ GameEffectDialogueCommand
→ ICommandExecutor
→ EffectRuntime
→ AddParameter
```

不使用這個 adapter 時，Dialogue module 可以獨立運作；換掉 Dialogue 系統時，GameFlow core 與 DefaultImplements 也不受影響。

> 換對話系統或客製對話演出，只需改各專案自己的這份橋接（或不用 Dialogue Module 時自行實作 `IDialoguePlayer`）。核心 `Scripts/` 與 `DefaultImplements/` 完全不受影響。

## 預設 UI（DefaultViews）

asmdef `KahaGameCore.Modules.GameFlowSystem.DefaultViews`（runtime）＋ `.DefaultViews.Editor`：

| 內容 | 說明 |
|---|---|
| `Views/`（9 個腳本） | 主選單、HUD（含 StatValueItem）、行動/移動選單（含按鈕 item）、提示視窗、製作名單。皆繼承 UserInterfaceSystem 的 `AView` |
| `Presenters/`（5 個腳本） | `IActionMenuPresenter` / `IHintPresenter` / `ILocationMenuPresenter` 的轉接實作＋HUD Presenter＋`IStagePerformance` 範例（CreditsPerformance） |
| `DefaultGameLauncher.cs` | 預設 Flow 組裝根：載入五張 Flow tables，並從 `GameEventCatalogAsset` 取得 Parameters 與 Events → Builder 組裝 → 主標題/流程切換、返回標題處理 |
| `Editor/DefaultUiBuilder.cs` | 選單 **KahaGameCore → GameFlowSystem → Build Default UI Prefabs And Scene**：在專案內生成 `Assets/Resources/GameFlowUIViews/` 九個 prefab 與 `Assets/Scenes/GameFlowGame.unity`（全部接好、測試表已掛上、可直接 Play）。全程式化版面（TMP 預設字型＋內建 UISprite），零美術資產依賴，可重複執行覆寫 |
| `SampleData/GameEvents/GameEventCatalog.asset` | Sample Game Events 的事件順序、Parameter Tables、Trigger Timings 與 Command assembly scopes。 |

最短組裝路徑：確認專案包含 KahaGameCore → 跑一次 builder 選單 → 開生成的場景直接 **Play**（測試內容可玩）→ 將欄位裡的 TextAsset 換成專案資料表。客製腳本放在專案 assembly 並使用專案命名；prefab 可直接修改，但重跑 builder 會覆寫生成內容。

注意：TMP 預設字型無 CJK，正式中文顯示需自建 TMP Font Asset 後替換 prefab 中的字型。

**DialogueView**：類別本體在 Dialogue Module（`DialogueManager` 直接依賴它，不能搬出），「怎麼接上」由 builder 處理——直接放在 Canvas 下錨點拉滿即可，內部元件錨點會自適應畫布大小，不需要縮放包覆層。其 `Update()` 使用 `UnityEngine.Input`，Active Input Handling 需設 Both 並重啟編輯器。

---

以下為核心層（`Scripts/`）的說明，**只在你不用預設實作、要從頭自接時才需要讀**。

```
開新遊戲 → GameStart 事件 → ┐
┌──────────────────────────┘
│ 階段開始 → PhaseStart 事件 → 行動選擇 → Action TriggerTiming → AfterAction → …
└─ 階段切換（由表中指令推動）後回到階段開始
```

任何時機點的事件若移動了地點，流程會自動補發 `EnterLocation` 事件（迴圈處理直到地點穩定，事件本身再移動地點也安全）。

## 相依

- **UniTask**（asmdef 參照名稱 `UniTask`）
- UnityEngine（僅用於 `Debug.LogWarning`）

不依賴 KahaGameCore 其他 Module——Effects、StaticData 等都只是「建議搭配」，由專案端自行組合。

## 接入核心介面

### 1. asmdef 加參照

```json
"references": [ "KahaGameCore.Modules.GameFlowSystem", "UniTask" ]
```

### 2. 實作 7 個介面

每個 Interface 都刻意縮到最小，大多只有一兩個成員。專案的 Interface／資料類別可直接繼承，不需要另寫 Adapter。

| 介面 | 成員 | 職責 |
|---|---|---|
| `IGameFlowTimePhase` | `ID`、`Key` | 一個時間階段（通常由表格資料類別實作） |
| `IGameFlowTimeService` | `CurrentPhase`、`ResetToFirstPhase()`、`AdvancePhase()` | Phase 推進；順序由實作方定義 |
| `IGameFlowLocationService` | `CurrentLocationID` | 流程只需要知道目前地點 |
| `IGameFlowAction` | `ID`、`Name`、`Description`、`TriggerTiming` | 一個玩家行動；效果由對應 Game Event 定義 |
| `IGameFlowActionProvider` | `GetVisibleActions(locationId)`、`IsEnabled(action)` | 依地點與條件過濾出可顯示的行動 |
| `IGameFlowEventTriggerService` | `RaiseTimingAsync(timing, token)` | 在時機點查事件表並依序執行命中的事件 |
| `IActionMenuPresenter` | `SelectActionAsync(entries)` | 顯示行動選單並等待玩家選擇；**回傳 null 表示流程被中止** |

實作時的約定：

- Phase 是否可行動沒有獨立旗標；目前地點若沒有任何 visible + enabled Action，Controller 自動 `AdvancePhase()`。
- `IGameFlowTimeService.CurrentPhase` 的 `ID` 是流程偵測「事件是否切換了階段」的依據——事件指令（如 SetPhase）改變階段後，流程會放棄目前階段、直接進入新階段。
- `RaiseTimingAsync` 收到取消的 token 後，不應再執行佇列中剩餘的事件（返回標題等中止情境）。

### 3. 組裝並啟動

```csharp
var flowController = new GameFlowController(
    timeService,        // IGameFlowTimeService
    locationService,    // IGameFlowLocationService
    actionProvider,     // IGameFlowActionProvider
    triggerService,     // IGameFlowEventTriggerService
    actionMenuPresenter // IActionMenuPresenter
);

// 開新局的狀態重置由呼叫端（組裝根）負責。
// 使用 DefaultImplements 時直接呼叫 services.ResetForNewGame()；
// 自組核心時則分別重置自己的 Parameters、TimeService 與 LocationService。

flowCts = new CancellationTokenSource();
flowController.RunNewGameAsync(flowCts.Token).Forget();
```

`RunNewGameAsync` 是無限迴圈，**唯一的結束方式是取消 token**。`RunNewGameAsync` 本身不重置狀態；組裝根必須在呼叫前完成新局重置。

### 4. 中止流程（返回標題）

```csharp
flowCts.Cancel();                      // 1. 取消流程迴圈與事件佇列
actionMenuPresenter.CancelPending();   // 2. 讓等待中的選單以 null 結束（自行在 Presenter 實作）
```

兩者都要做：若玩家正停在行動選單上，光取消 token 不會喚醒 `await SelectActionAsync`，需要 Presenter 自己把等待中的 UniTask 以 null 完成。

## 事件表時機字串（GameFlowTimings）

流程在以下時機呼叫 `RaiseTimingAsync`，事件表的 Timing 欄位填這些字串：

| 時機 | 字串 | 說明 |
|---|---|---|
| 開新遊戲 | `GameStart` | 組裝根完成 Parameters／Phase／Location 重置後、第一個階段之前（開場劇情） |
| 階段開始（共通） | `PhaseStart` | 每個 Phase 先觸發一次 |
| 階段開始（指定） | `PhaseStart:{Key}` | 共通 timing 完成後觸發，如 `PhaseStart:Morning` |
| 玩家行動 | `IGameFlowAction.TriggerTiming` | 由 Action data 明確指定，如 `Action:106` |
| 行動完成（共通） | `AfterAction` | Action timing 的 queue 完整結束後觸發 |
| 進入地點 | `EnterLocation:{ID}` | 如 `EnterLocation:2`，只在地點「改變」時觸發 |

Game Event timing 是 ordinal exact match，沒有 `Any` 萬用字。Action 自己的 timing 存在資料中；固定 lifecycle timing 用 `GameFlowTimings` 產生。

## 內建防呆行為

- 若沒有任何 visible + enabled Action，流程會輸出 LogWarning 並自動 `AdvancePhase()`。
- 連續自動推進再次遇到同一個 Phase ID 時丟出 `InvalidOperationException`，避免資料形成無限循環。
- `SelectActionAsync` 回傳 null 時，該輪行動直接略過（不執行指令、不發 AfterAction），由外層迴圈依 token / 階段狀態決定去留。

## 測試

`Tests/Editor/GameFlowControllerTest.cs` 是純 C#（無場景、無 MonoBehaviour）的時序測試，所有 Fake 同步完成、以「觸發 N 次後取消」收斂無限迴圈。修改流程前先執行：Test Runner → EditMode → `KahaGameCore.Modules.GameFlowSystem.Tests`。

## 參考實作

- **核心介面如何對接具體表格型別**：見 `DefaultImplements/` 本身——`ITimeService : IGameFlowTimeService`、`TimePhaseData`、`PlayerActionData.TriggerTiming` 與 `PlayerActionProvider`。
- **組裝根與 UI 層**：`DefaultViews/DefaultGameLauncher.cs`（Builder 用法、返回標題的 CancelFlow + CancelPending）與 `DefaultViews/Presenters/`。
- **Game Events 整合**：`GameFlowGameEventAdapter` 位於可選 integration assembly；GameFlow 與 Game Events 共用 `GameFlowServices.CommandRegistry`／`EffectRuntime`。`EffectCommandExecutor` 僅供 Dialogue bridge 等 DefaultImplements integration 使用。
