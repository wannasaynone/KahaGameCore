# Dialogue

## 目的

表驅動對話系統：對話內容寫在 `DialogueData` 表（JSON 陣列），每行一個指令（Say、選項、立繪、CG、音效……），程式只負責播放。asmdef 為 `KahaGameCore.Modules.Dialogue`，C# namespace 為 `KahaGameCore.Dialogue`。

> 此模組是 legacy runtime，尚未完成 cancellation、結構化錯誤與 Localization 重構。新專案可以使用現有功能，但不可假設所有舊 command 都已實作。

## 快速開始

```csharp
// 1. 場景中放入 Prefabs/DialogueView.prefab，取得其 DialogueView 參考
// 2. 載表
var staticDataManager = new GameStaticDataManager();
staticDataManager.Add<DialogueData>(jsonHandler);   // 任一 IGameStaticDataHandler

// 3. 建立 Manager —— 不傳 commandFactoryContainer 時，18 個內建指令會自動註冊
var dialogueManager = new DialogueManager(dialogueView, staticDataManager);

// 4. 播放；View 的顯示生命週期由呼叫端負責
dialogueView.gameObject.SetActive(true);
dialogueManager.StartDialogue(dialogueId, onDialogueComplete: () =>
{
    dialogueView.gameObject.SetActive(false);
});
```

連續呼叫 `StartDialogue` 會排入佇列依序播放。靜態事件 `DialogueManager.OnAnyDialogueReadyToStart / OnAnyDialogueEnded` 可監聽任意對話的起訖。

## 執行模型

Dialogue 自己擁有獨立的 command pipeline，不依賴 GameFlowSystem 或 Effects：

```text
DialogueData
→ DialogueManager
→ DialogueCommandFactoryContainer
→ DialogueCommandBase.Process(args, context)
```

`DialogueManager` 負責對話佇列、行序、建立 `DialogueContext` 與推進下一行；具體指令只處理自己的演出或跳轉行為。

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

## 自訂指令

要保留內建指令並追加專案指令，從預設容器開始組裝：

```csharp
DialogueCommandFactoryContainer commands =
    DialogueCommandFactoryContainer.CreateDefault();

commands.RegisterFactory("MyCommand", new MyCommandFactory());

var dialogueManager = new DialogueManager(
    dialogueView,
    staticDataManager,
    commands);
```

直接 `new DialogueCommandFactoryContainer()` 會得到空容器，適合完全取代內建指令的情境。自訂 command 實作 `DialogueCommandBase.Process()`，factory 實作 `DialogueCommandFactoryBase.Create()`。

## 已知陷阱

- **`DialogueView.Update()` 使用舊版 `UnityEngine.Input`**——專案 Active Input Handling 需設為 Both（ProjectSettings `activeInputHandler: 2`），改完必須重啟編輯器才生效。
- **放進場景時錨點記得拉滿**——DialogueView 內部元件的錨點會自適應畫布大小，直接放在 Canvas 下、根節點錨點 0,0~1,1 鋪滿即可，不需要縮放包覆層。
- **預設 CG / 音訊 Provider 走 Addressables**（`AddressablesCGProvider` / `AddressablesAudioProvider`）——專案未使用 Addressables 或資源不在其中時，需自行實作 `ICGProvider` / `IAudioProvider` 傳入建構子，否則 ShowCharacter / PlaySoundEffect 等指令會載不到資源。
- **TMP 預設字型無 CJK**——中文顯示為方塊，需自建中文 TMP Font Asset 並替換 prefab 字型。
- **未知 command 會記錄錯誤後跳過該行**，不會中止整段對話；command 內拋出的例外目前也沒有統一轉成結構化結果。

未列在「內建指令」清單內的舊 command 檔案可能仍存在但丟出 `NotImplementedException`；不要只因類別存在就把它加入內容表。

## 可選整合

Dialogue module 不認識 GameFlowSystem 或 Effects。需要接入 GameFlow 時，由外部 adapter 組裝；參見 [`GameFlowSystem/README.md`](../GameFlowSystem/README.md#對話橋接範例各專案自備)。
