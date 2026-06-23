# GameFlowSystem

表驅動遊戲主流程包。提供固定的流程骨架，所有劇情、條件與數值變化都由各專案的表格定義，包內不含任何劇情內容。

包分三層：

- **`Scripts/`（核心）**：`GameFlowController` 與 8 個最小介面，只依賴 UniTask。
- **`DefaultImplements/`（預設實作）**：表驅動的整套預設服務（GameState、TimeService、LocationService、事件觸發、效果指令、條件式、對話橋接、表格定義）＋ `GameFlowSystemBuilder` 一鍵組裝。依賴 KahaGameCore（GameData/ValueContainer/GameEvent/EffectProcessor）與 DialogueSystem。新專案直接用預設實作，有新需求再自己實作傳入覆寫。
- **`DefaultViews/`（預設 UI）**：整套 UGUI View / Presenter / `DefaultGameLauncher`（組裝根）腳本＋`DefaultUiBuilder`（Editor 選單一鍵生成全部 prefab 與可運行場景）。純腳本、零美術資產——prefab 在各專案內按需生成。

> **開新專案請直接看 [`新專案實作指南.md`](新專案實作指南.md)**——含一鍵生成路線、表格規格全文、手動實作 UI 的完整程式碼與 prefab 結構規格、疑難排解。

## 快速接入（使用預設實作）

```csharp
// 1. 載入表格——二擇一：
//    a) Inspector 手動指定 TextAsset（推薦，不依賴 Resources 路徑；檔名需與型別名一致）
var staticDataManager = new GameStaticDataManager();
var handler = new TextAssetJsonStaticDataHandler(gameDataTables);   // TextAsset[] 序列化欄位
//    b) 或從 Resources/GameData/{類別名}.txt 載入：new ResourcesJsonStaticDataHandler()
GameFlowSystemBuilder.LoadDefaultTables(staticDataManager, handler);
staticDataManager.Add<DialogueData>(handler); // 對話表另外加

// 2. 組裝（UI 層是各專案的演出資產，由外部提供；其餘全用預設）
GameFlowServices services = new GameFlowSystemBuilder(staticDataManager)
    .WithDialogueView(dialogueView)                     // 必要（或 OverrideDialoguePlayer）
    .WithActionMenuPresenter(actionMenuPresenter)       // 必要
    .WithHintPresenter(hintPresenter)                   // 表格有用 ShowHint 才需要
    .WithLocationMenuPresenter(locationMenuPresenter)   // 表格有用 OpenLocationMenu 才需要
    .Build();

// 3. 啟動流程
flowCts = new CancellationTokenSource();
services.FlowController.RunNewGameAsync(flowCts.Token).Forget();
```

有新需求時，實作對應介面後以 `Override` 系列方法傳入，其餘維持預設：

```csharp
var services = new GameFlowSystemBuilder(staticDataManager)
    .WithDialogueView(dialogueView)
    .WithActionMenuPresenter(actionMenuPresenter)
    .OverrideTimeService(new MyRealTimeService())        // 例：改用真實時間制
    .OverrideConditionEvaluator(new MyLuaEvaluator())    // 例：改用 Lua 條件式
    .AddCommandRegistration(c => c.RegisterFactory(      // 例：追加專案自訂效果指令
        "MyCommand", new DelegateEffectCommandFactory(() => new MyCommand())))
    .Build();
```

`GameFlowServices` 會回傳所有組好的服務（GameState、TimeService、TriggerService、FlowController、FactoryContainer…），HUD 等 Presenter 可直接取用。

## 預設實作內容（DefaultImplements）

| 區塊 | 內容 |
|---|---|
| `Data/` | 六張表的資料類別：TimePhaseData、PlayerActionData、LocationData、GameEventTriggerData、GameValueData、GameTextData（JSON 陣列，欄位規格全文見 `新專案實作指南.md` 第 2 節） |
| `DataAccess/` | `ResourcesJsonStaticDataHandler`（Resources/GameData/{類別名}.txt）與 `TextAssetJsonStaticDataHandler`（Inspector 手動指定，檔名=型別名） |
| `Domain/` | GameState（數值鉗制＋事件發佈）、TimeService（階段推進/換日）、LocationService（解鎖旗標）、PlayerActionProvider、GameEventTriggerService（優先度＋Any 萬用字＋前後演出）、EffectCommandExecutor、FormulaConditionEvaluator（`$Tag >= 200` 語法 → Calculator）、GameTextProvider、DialoguePlayer、PerformanceRegistry、EffectCommandRegistrar |
| `Domain/Commands/` | 內建效果指令：AddValue、SetValue、AdvanceTime、SetPhase、MoveToLocation、StartDialogue、ShowHint、Monologue、PlayPerformance、OpenLocationMenu、ReturnToTitle、Wait |
| `Domain/Events/` | EventBus 事件：GameValueChanged、TimePhaseChanged、LocationChanged、MonologueRequested、ReturnToTitleRequested |
| `GameFlowSystemBuilder.cs` | 組裝器與 `GameFlowServices` |

## 預設 UI（DefaultViews）

asmdef `KahaGameCore.Package.GameFlowSystem.DefaultViews`（runtime）＋ `.DefaultViews.Editor`：

| 內容 | 說明 |
|---|---|
| `Views/`（9 個腳本） | 主選單、HUD（含 StatValueItem）、行動/移動選單（含按鈕 item）、提示視窗、製作名單。皆繼承 UserInterfaceSystem 的 `AView` |
| `Presenters/`（5 個腳本） | `IActionMenuPresenter` / `IHintPresenter` / `ILocationMenuPresenter` 的轉接實作＋HUD Presenter＋`IStagePerformance` 範例（CreditsPerformance） |
| `DefaultGameLauncher.cs` | 預設組裝根：載表（Inspector 指定 TextAsset，留空 fallback 到 Resources/GameData/）→ Builder 組裝 → 主標題/流程切換、返回標題處理 |
| `Editor/DefaultUiBuilder.cs` | 選單 **KahaGameCore → GameFlowSystem → Build Default UI Prefabs And Scene**：在專案內生成 `Assets/Resources/GameFlowUIViews/` 九個 prefab 與 `Assets/Scenes/GameFlowGame.unity`（全部接好、測試表已掛上、可直接 Play）。全程式化版面（TMP 預設字型＋內建 UISprite），零美術資產依賴，可重複執行覆寫 |
| `SampleData/*.txt` | 七張測試表：可完整遊玩的迷你生存循環（照顧小屋一週），涵蓋選項對話、GameEffect、Monologue、ShowHint、隨機數值、地點解鎖、Game Over、結局演出等全部系統功能，可當表格寫法範例。builder 會自動拖進 DefaultGameLauncher 的 `gameDataTables` |

新專案最短路徑：複製 KahaGameCore 包 → 跑一次 builder 選單 → 開生成的場景直接 **Play**（測試內容可玩）→ 之後把欄位裡的 TextAsset 換成自己的表。要客製時把對應腳本複製到專案改名修改（別直接改包內版本），prefab 直接改（重跑 builder 會覆寫）。

注意：TMP 預設字型無 CJK，正式中文顯示需自建 TMP Font Asset 後替換 prefab 中的字型。

**DialogueView**：類別本體在 DialogueSystem 包（`DialogueManager` 直接依賴它，不能搬出），「怎麼接上」由 builder 處理——直接放在 Canvas 下錨點拉滿即可，內部元件錨點會自適應畫布大小，不需要縮放包覆層。已知陷阱：其 `Update()` 用舊版 Input，Active Input Handling 需設 Both 並重啟編輯器。

---

以下為核心層（`Scripts/`）的說明，**只在你不用預設實作、要從頭自接時才需要讀**。

```
開新遊戲 → GameStart 事件 → ┐
┌──────────────────────────┘
│ 階段開始 → PhaseStart 事件 → （可行動階段）行動選擇 → 行動指令 → AfterAction 事件 → …
└─ 階段切換（由表中指令推動）後回到階段開始
```

任何時機點的事件若移動了地點，流程會自動補發 `EnterLocation` 事件（迴圈處理直到地點穩定，事件本身再移動地點也安全）。

## 相依

- **UniTask**（asmdef 參照名稱 `UniTask`）
- UnityEngine（僅用於 `Debug.LogWarning`）

不依賴 KahaGameCore 其他模組——EffectProcessor、GameData 等都只是「建議搭配」，由專案端自行組合。

## 接入步驟

### 1. asmdef 加參照

```json
"references": [ "KahaGameCore.Package.GameFlowSystem", "UniTask" ]
```

### 2. 實作 8 個介面

每個介面都刻意縮到最小，大多只有一兩個成員。建議讓專案既有的介面/資料類別直接繼承（參考 ProjectII 的做法，見文末），不用另寫轉接類別。

| 介面 | 成員 | 職責 |
|---|---|---|
| `IGameFlowState` | `ResetToInitial()` | 開新遊戲時把所有可變數值重設回初始值 |
| `IGameFlowTimePhase` | `ID`、`Key`、`AllowAction` | 一個時間階段（通常由表格資料類別實作） |
| `IGameFlowTimeService` | `CurrentPhase`、`ResetToFirstPhase()`、`AdvanceTime()` | 時間流動；階段順序由實作方定義 |
| `IGameFlowLocationService` | `CurrentLocationID` | 流程只需要知道目前地點 |
| `IGameFlowAction` | `ID`、`Name`、`Description`、`Commands` | 一個玩家行動（通常由表格資料類別實作） |
| `IGameFlowActionProvider` | `GetVisibleActions(locationId)`、`IsEnabled(action)` | 依地點與條件過濾出可顯示的行動 |
| `IGameFlowEventTriggerService` | `RaiseTimingAsync(timing, token)` | 在時機點查事件表並依序執行命中的事件 |
| `IGameFlowCommandExecutor` | `ExecuteAsync(rawCommands)` | 執行行動的效果指令串（建議接 EffectProcessor） |
| `IActionMenuPresenter` | `SelectActionAsync(entries)` | 顯示行動選單並等待玩家選擇；**回傳 null 表示流程被中止** |

實作時的約定：

- `IGameFlowTimePhase.AllowAction`：true = 此階段開放玩家選行動；false = 觸發完 PhaseStart 事件後流程自動呼叫 `AdvanceTime()`。表格若以 int 儲存（0/1），用顯式實作轉型即可。
- `IGameFlowTimeService.CurrentPhase` 的 `ID` 是流程偵測「事件是否切換了階段」的依據——事件指令（如 SetPhase）改變階段後，流程會放棄目前階段、直接進入新階段。
- `RaiseTimingAsync` 收到取消的 token 後，不應再執行佇列中剩餘的事件（返回標題等中止情境）。

### 3. 組裝並啟動

```csharp
var flowController = new GameFlowController(
    gameState,          // IGameFlowState
    timeService,        // IGameFlowTimeService
    locationService,    // IGameFlowLocationService
    actionProvider,     // IGameFlowActionProvider
    triggerService,     // IGameFlowEventTriggerService
    commandExecutor,    // IGameFlowCommandExecutor
    actionMenuPresenter // IActionMenuPresenter
);

flowCts = new CancellationTokenSource();
flowController.RunNewGameAsync(flowCts.Token).Forget();
```

`RunNewGameAsync` 是無限迴圈，**唯一的結束方式是取消 token**。

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
| 開新遊戲 | `GameStart` | ResetToInitial / ResetToFirstPhase 之後、第一個階段之前（開場劇情） |
| 階段開始 | `PhaseStart:{Key}` | 如 `PhaseStart:Morning` |
| 行動結束 | `AfterAction:{ID}` | 如 `AfterAction:106` |
| 進入地點 | `EnterLocation:{ID}` | 如 `EnterLocation:2`，只在地點「改變」時觸發 |

字串一律用 `GameFlowTimings` 的常數/方法產生，不要手刻。萬用字（如 `PhaseStart:Any`）是事件表服務端的慣例（ProjectII 的 GameEventTriggerService 有實作），不是本包的功能。

## 內建防呆行為

- 可行動階段若查無任何可選行動，流程會輸出 LogWarning 並自動 `AdvanceTime()`，避免卡死——看到這個警告請檢查行動表。
- `SelectActionAsync` 回傳 null 時，該輪行動直接略過（不執行指令、不發 AfterAction），由外層迴圈依 token / 階段狀態決定去留。

## 測試

`Tests/Editor/GameFlowControllerTest.cs` 是純 C#（無場景、無 MonoBehaviour）的時序測試，所有 Fake 同步完成、以「觸發 N 次後取消」收斂無限迴圈。新專案改動流程前先跑一次：Test Runner → EditMode → `KahaGameCore.Package.GameFlowSystem.Tests`。

## 參考實作

- **核心介面如何對接具體表格型別**：見 `DefaultImplements/` 本身——`ITimeService : IGameFlowTimeService`（以 `new TimePhaseData CurrentPhase` 覆蓋成具體型別供 HUD 使用，`TimeService` 再顯式實作 `IGameFlowTimeService.CurrentPhase`）、`TimePhaseData`（`bool IGameFlowTimePhase.AllowAction => AllowAction == 1;`）、`PlayerActionProvider`（利用 `IReadOnlyList<T>` 協變顯式實作 `IGameFlowActionProvider`）。
- **組裝根與 UI 層**：`DefaultViews/DefaultGameLauncher.cs`（Builder 用法、返回標題的 CancelFlow + CancelPending）與 `DefaultViews/Presenters/`；實際運行中的版本見 `Assets/ProjectII/Scripts/`。
- **注意**：`EffectProcessor` 在 `KahaGameCore.Package.*` 命名空間底下會優先解析為命名空間而非類別，需要用 using 別名（見 `DefaultImplements/Domain/EffectCommandExecutor.cs` 開頭）。
