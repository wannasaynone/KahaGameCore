# Dialogue

## 目的

表驅動對話系統：對話內容寫在 `DialogueData` 表（JSON 陣列），每行一個指令（Say、選項、立繪、CG、音效……），程式只負責播放。asmdef 為 `KahaGameCore.Modules.Dialogue`，C# namespace 為 `KahaGameCore.Dialogue`。

> 此模組是 legacy runtime，未完成原訂的 Effects command／cancellation／Localization 重構。新專案可以使用現有功能，但不可假設所有舊 command 都已實作。

## 快速開始

### A. 搭配 GameFlowSystem（最短，推薦）

GameFlowSystem 的 `DefaultViews/DialoguePlayer` 已把本系統包好（UniTask 等待 + GameEffect 橋接指令）：

1. 執行選單 **KahaGameCore → GameFlowSystem → Build Default UI Prefabs And Scene**——生成的場景已含 DialogueView，並接好 DefaultGameLauncher（其載表流程已包含 DialogueData）。
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

內建指令（由 `DialogueCommandFactoryContainer.CreateDefault()` 建立，`DialogueManager` 未傳容器時也會使用）：
`Say`、`AddOption`、`ShowOptions`、`GoToLine`、`BlackIn`、`BlackOut`、`ShowFullScreenImage`、`HideFullScreenImage`、`HideDialogueBox`、`PlaySoundEffect`、`PlayBackgroundMusic`、`ShowCharacter`、`HideCharacter`、`ChangeCharacter`、`MoveCharacterX`、`MoveCharacterY`、`CharacterJump`、`ScaleCharacter`

## 已知陷阱

- **加入自訂指令並保留內建指令**——先呼叫 `DialogueCommandFactoryContainer.CreateDefault()`，再以 `RegisterFactory()` 追加自訂 factory，最後把容器傳給 `DialogueManager`。
- **`DialogueView.Update()` 使用舊版 `UnityEngine.Input`**——專案 Active Input Handling 需設為 Both（ProjectSettings `activeInputHandler: 2`），改完必須重啟編輯器才生效。
- **放進場景時錨點記得拉滿**——DialogueView 內部元件的錨點會自適應畫布大小，直接放在 Canvas 下、根節點錨點 0,0~1,1 鋪滿即可，不需要縮放包覆層（GameFlowSystem 的 `DefaultUiBuilder.InstantiateDialogueView()` 已示範）。
- **預設 CG / 音訊 Provider 走 Addressables**（`AddressablesCGProvider` / `AddressablesAudioProvider`）——專案未使用 Addressables 或資源不在其中時，需自行實作 `ICGProvider` / `IAudioProvider` 傳入建構子，否則 ShowCharacter / PlaySoundEffect 等指令會載不到資源。
- **TMP 預設字型無 CJK**——中文顯示為方塊，需自建中文 TMP Font Asset 並替換 prefab 字型。

## 與 GameFlowSystem 的整合細節

`DialoguePlayer`（在 GameFlowSystem 的 DefaultViews 範例）使用 Dialogue 提供的預設容器，再額外註冊 `GameEffect` 橋接指令——對話行裡可以把 Effects command 字串交給 GameFlow 共用的 `EffectRuntime`（例如選項選完改數值、移動地點），這是表驅動流程「對話 ↔ 遊戲狀態」互通的關鍵。

未列在「內建指令」清單內的舊 command 檔案可能仍存在但丟出 `NotImplementedException`；不要只因類別存在就把它加入內容表。
