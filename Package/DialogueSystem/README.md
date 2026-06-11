# DialogueSystem

表驅動對話系統：對話內容寫在 `DialogueData` 表（JSON 陣列），每行一個指令（Say、選項、立繪、CG、音效……），程式只負責播放。asmdef / 命名空間為 `ProjectBSR.DialogueSystem`（歷史名稱，沿用勿改）。

## 最短啟動流程

### A. 搭配 GameFlowSystem（最短，推薦）

GameFlowSystem 的 `DefaultImplements/DialoguePlayer` 已把本系統包好（UniTask 等待 + 內建指令註冊 + GameEffect 橋接指令）：

1. 執行選單 **KahaGameCore → GameFlowSystem → Build Default UI Prefabs And Scene**——生成的場景已含 DialogueView（連同 1080p 縮放包覆層），並接好 DefaultGameLauncher（其載表流程已包含 DialogueData）。
2. 在事件表/行動表用 `StartDialogue(對話ID)` 指令即可播放，對話結束自動接回流程。

詳見 `GameFlowSystem/新專案實作指南.md`。

### B. 單獨使用

```csharp
// 1. 場景中放入 Prefabs/DialogueView.prefab，取得其 DialogueView 參考
// 2. 載表
var staticDataManager = new GameStaticDataManager();
staticDataManager.Add<DialogueData>(jsonHandler);   // 任一 IGameStaticDataHandler

// 3. 建立 Manager —— 不傳 commandFactoryContainer 時，18 個內建指令會自動註冊
var dialogueManager = new DialogueManager(dialogueView, staticDataManager);

// 4. 播放（開始前要自己開啟 view；全部對話播完 Manager 會自動 SetActive(false)）
dialogueView.gameObject.SetActive(true);
dialogueManager.StartDialogue(dialogueId, onDialogueComplete: () => { /* 結束 */ });
```

連續呼叫 `StartDialogue` 會排入佇列依序播放。靜態事件 `DialogueManager.OnAnyDialogueReadyToStart / OnAnyDialogueEnded` 可監聽任意對話的起訖。

## DialogueData 表格式

| 欄位 | 說明 |
|---|---|
| `ID` | 對話段落 ID（同一段對話的所有行共用） |
| `Line` | 行序（從 1 開始，播放時依序遞增；GoToLine 可跳行） |
| `Command` | 指令名稱（見下表） |
| `Arg1`~`Arg5` | 指令參數 |
| `Arg1_en`~`Arg5_jp` | 多語系欄位（en / hans / jp） |

內建指令（`DialogueManager` 未傳容器時自動註冊）：
`Say`、`AddOption`、`ShowOptions`、`GoToLine`、`BlackIn`、`BlackOut`、`ShowFullScreenImage`、`HideFullScreenImage`、`HideDialogueBox`、`PlaySoundEffect`、`PlayBackgroundMusic`、`ShowCharacter`、`HideCharacter`、`ChangeCharacter`、`MoveCharacterX`、`MoveCharacterY`、`CharacterJump`、`ScaleCharacter`

## 陷阱（皆在 ProjectII 踩過）

- **自訂 `DialogueCommandFactoryContainer` 時，內建指令不會自動註冊**——`DialogueManager` 只在「沒傳容器」時才註冊預設指令。要加自訂指令又保留內建，請照 `GameFlowSystem/DefaultImplements/Domain/DialoguePlayer.CreateFactoryContainerWithDefaults()` 的清單補齊後再追加。
- **`DialogueView.Update()` 使用舊版 `UnityEngine.Input`**——專案 Active Input Handling 需設為 Both（ProjectSettings `activeInputHandler: 2`），改完必須重啟編輯器才生效。
- **DialogueView prefab 以 1920x1080 設計**——高解析度 Canvas（如 4K）需用一層固定 1080p 尺寸、等比放大的節點包覆（`GameFlowSampleRoot.prefab` 與 `SampleUiBuilder.InstantiateDialogueView()` 已示範）。
- **預設 CG / 音訊 Provider 走 Addressables**（`AddressablesCGProvider` / `AddressablesAudioProvider`）——專案未使用 Addressables 或資源不在其中時，需自行實作 `ICGProvider` / `IAudioProvider` 傳入建構子，否則 ShowCharacter / PlaySoundEffect 等指令會載不到資源。
- **TMP 預設字型無 CJK**——中文顯示為方塊，需自建中文 TMP Font Asset 並替換 prefab 字型。

## 與 GameFlowSystem 的整合細節

`DialoguePlayer`（在 GameFlowSystem 的 DefaultImplements）除了補齊內建指令外，額外註冊了 `GameEffect` 橋接指令——對話行裡可以直接執行 EffectProcessor 效果指令串（例如選項選完改數值、移動地點），這是表驅動流程「對話 ↔ 遊戲狀態」互通的關鍵。
