# DialogueSystem 套件文檔

## 目錄
1. [概述](#概述)
2. [核心組件](#核心組件)
3. [系統架構](#系統架構)
4. [使用指南](#使用指南)
5. [API 參考](#api-參考)

## 概述

DialogueSystem 是一個用於 Unity 的綜合性對話系統套件，專為開發具有豐富對話和敘事體驗的遊戲而設計。它提供了一套完整的工具和組件，用於創建互動式對話、角色立繪、CG 圖像顯示和玩家選項等功能。

### 主要特點

- **完整的對話系統**：支持文本顯示、角色立繪、CG 圖像和選項系統
- **命令系統**：豐富的對話命令集，用於控制對話流程和特效
- **多語言支持**：內建支持繁體中文、簡體中文、英文和日文
- **玩家數據管理**：追蹤已讀對話、物品收集和標籤系統
- **條件分支**：基於玩家選擇、物品擁有和標籤的條件分支
- **視覺效果**：支持角色高亮、CG 圖像顯示、閃爍和轉場效果
- **音效和動畫**：支持背景音樂和角色動畫

## 核心組件

### DialogueManager（對話管理器）

DialogueManager 是對話系統的核心控制器，負責初始化對話系統、處理對話觸發和管理對話流程。

#### 主要功能

- **對話觸發**：啟動指定 ID 的對話
- **對話隊列**：管理多個對話的排隊和順序執行
- **多語言支持**：切換不同語言的對話內容

### DialogueProcesser（對話處理器）

DialogueProcesser 負責處理單個對話的執行流程，將對話數據轉換為對話命令並按順序執行。

#### 主要功能

- **命令處理**：創建和執行對話命令
- **對話流程控制**：管理對話的開始、進行和結束

### DialogueView（對話視圖）

DialogueView 是對話系統的視覺表現層，負責顯示對話文本、角色立繪、CG 圖像和選項按鈕。

#### 主要功能

- **文本顯示**：顯示對話文本和角色名稱
- **角色立繪**：顯示和控制左右兩側的角色立繪
- **CG 圖像**：顯示全屏和小型 CG 圖像
- **選項按鈕**：創建和管理玩家選項按鈕
- **特效**：提供閃爍、轉場和震動等特效

### Player（玩家）

Player 類管理玩家的數據，包括已讀對話、擁有物品和標籤。

#### 主要功能

- **物品管理**：添加、移除和檢查物品
- **對話記錄**：記錄已讀對話
- **標籤系統**：添加、移除和檢查標籤
- **數據保存**：保存和加載玩家數據

### DialogueCommandBase（對話命令基類）

DialogueCommandBase 是所有對話命令的基類，提供了命令執行的基本框架。

#### 主要功能

- **命令處理**：執行特定的對話命令
- **回調管理**：處理命令完成和強制退出的回調

## 系統架構

DialogueSystem 套件使用命令模式和工廠模式設計，實現了高度可擴展的對話系統架構。

```
DialogueManager
    |
    ├── DialogueProcesser
    |       |
    |       └── DialogueCommandBase (各種對話命令)
    |               |
    |               ├── DialogueCommand_Say
    |               ├── DialogueCommand_SetCharacter
    |               ├── DialogueCommand_AddOption
    |               ├── DialogueCommand_ShowCG
    |               └── ...
    |
    ├── DialogueView
    |       |
    |       ├── 角色立繪顯示
    |       ├── 文本顯示
    |       ├── CG 圖像顯示
    |       └── 選項按鈕
    |
    └── Player
            |
            ├── 物品管理
            ├── 對話記錄
            └── 標籤系統
```

## 使用指南

### 初始化對話系統

1. 在場景中添加 DialogueView 預製體：
   - 可以使用 `Prefabs/DialogueView.prefab`

2. 初始化 DialogueManager：
   ```csharp
   // Initialize player manager
   DialogueSystem.PlayerManager.Initialize();
   
   // Initialize dialogue manager
   DialogueSystem.DialogueManager.Initialize(
       gameStaticDataManager,  // Game data manager
       new DialogueCommandFactory(DialogueSystem.PlayerManager.Instance.Player),  // Dialogue command factory
       DialogueSystem.DialogueManager.LanguageType.TranditionalChinese  // Language type
   );
   ```

### 創建對話數據

1. 創建對話數據文件：
   - 使用 JSON 格式創建對話數據
   - 每行對話包含 ID、Line、Command 和參數

2. 對話數據格式示例：
   ```json
   [
     {
       "ID": 1,
       "Line": 1,
       "Command": "SetCharacter",
       "Arg1": "Cutout_Maya_1",
       "Arg2": "",
       "Arg3": "",
       "Arg1_en": "Maya",
       "Arg2_en": "",
       "Arg3_en": "",
       "Arg1_hans": "玛雅",
       "Arg2_hans": "",
       "Arg3_hans": ""
     },
     {
       "ID": 1,
       "Line": 2,
       "Command": "Say",
       "Arg1": "瑪雅",
       "Arg2": "你好！這是一個測試對話。",
       "Arg3": "",
       "Arg1_en": "Maya",
       "Arg2_en": "Hello! This is a test dialogue.",
       "Arg3_en": "",
       "Arg1_hans": "玛雅",
       "Arg2_hans": "你好！这是一个测试对话。",
       "Arg3_hans": ""
     },
     {
       "ID": 1,
       "Line": 3,
       "Command": "AddOption",
       "Arg1": "繼續",
       "Arg2": "2",
       "Arg3": "",
       "Arg1_en": "Continue",
       "Arg2_en": "2",
       "Arg3_en": "",
       "Arg1_hans": "继续",
       "Arg2_hans": "2",
       "Arg3_hans": ""
     },
     {
       "ID": 1,
       "Line": 4,
       "Command": "AddOption",
       "Arg1": "結束",
       "Arg2": "",
       "Arg3": "",
       "Arg1_en": "End",
       "Arg2_en": "",
       "Arg3_en": "",
       "Arg1_hans": "结束",
       "Arg2_hans": "",
       "Arg3_hans": ""
     }
   ]
   ```

### 觸發對話

```csharp
// Create dialogue data
DialogueManager.PendingDialogueData pendingDialogueData = new DialogueManager.PendingDialogueData
{
    id = 1,  // Dialogue ID
    dialogueView = dialogueView,  // Dialogue view
    onCompleted = () => {
        // Callback when dialogue is completed
    }
};

// Trigger dialogue
DialogueManager.Instance.TriggerDialogue(pendingDialogueData);
```

### 創建自定義對話命令

1. 創建繼承自 DialogueCommandBase 的新類：
   ```csharp
   public class DialogueCommand_Custom : DialogueCommandBase
   {
       public DialogueCommand_Custom(DialogueData dialogueData, IDialogueView dialogueView)
           : base(dialogueData, dialogueView)
       {
       }

       public override void Process(Action onCompleted, Action onForceQuit)
       {
           // Implement command logic
           
           // Call callback when completed
           onCompleted?.Invoke();
       }
   }
   ```

2. 在 DialogueCommandFactory 中註冊新命令：
   ```csharp
   public DialogueCommandBase CreateDialogueCommand(DialogueData dialogueData, IDialogueView dialogueView)
   {
       switch (dialogueData.Command)
       {
           // ...
           case "Custom":
               return new DialogueCommand_Custom(dialogueData, dialogueView);
           // ...
       }
   }
   ```

## 支持的對話命令

DialogueSystem 支持多種對話命令，用於控制對話流程和視覺效果：

| 命令 | 描述 | 參數 |
|------|------|------|
| Say | 顯示對話文本 | Arg1: 角色名稱, Arg2: 對話內容 |
| SetCharacter | 設置角色立繪 | Arg1: 角色圖像名稱 |
| AddOption | 添加選項按鈕 | Arg1: 選項文本, Arg2: 跳轉對話 ID |
| ShowCG | 顯示 CG 圖像 | Arg1: CG 圖像名稱 |
| HideCG | 隱藏 CG 圖像 | - |
| AddItem | 添加物品 | Arg1: 物品 ID, Arg2: 數量 |
| IfHasItem | 條件分支：如果有物品 | Arg1: 物品 ID, Arg2: 跳轉行號 |
| GoTo | 跳轉到指定行 | Arg1: 行號 |
| IfRead | 條件分支：如果已讀對話 | Arg1: 對話 ID, Arg2: 跳轉行號 |
| AddTag | 添加標籤 | Arg1: 標籤名稱 |
| RemoveTag | 移除標籤 | Arg1: 標籤名稱 |
| IfHasTags | 條件分支：如果有標籤 | Arg1: 標籤名稱, Arg2: 跳轉行號 |
| Flash | 閃爍效果 | Arg1: 淡入時間, Arg2: 停留時間, Arg3: 淡出時間 |
| Transition | 轉場效果 | Arg1: 淡入時間, Arg2: 停留時間, Arg3: 淡出時間 |
| ShakeCG | 震動 CG 圖像 | Arg1: 強度, Arg2: 時間 |
| PlayBGM | 播放背景音樂 | Arg1: 音樂名稱 |
| StopBGM | 停止背景音樂 | - |
| Wait | 等待指定時間 | Arg1: 等待時間（秒） |

## API 參考

### DialogueManager 類

```csharp
// Static methods
public static void Initialize(GameStaticDataManager gameStaticDataManager, IDialogueFactory dialogueFactory, LanguageType languageType);

// Instance methods
public void TriggerDialogue(PendingDialogueData pendingDialogueData);
```

### DialogueView 類

```csharp
// Character portrait methods
public void SetLeftCharacterImage(Sprite sprite, Action onCompleted = null);
public void SetRightCharacterImage(Sprite sprite, Action onCompleted = null);
public void HighlightLeftCharacterImage(Action onCompleted = null);
public void HighlightRightCharacterImage(Action onCompleted = null);

// Text display methods
public void SetContentText(string text, Action onCompleted = null);
public void SetNameText(string text);

// CG image methods
public void ShowCGImage(Sprite sprite, Action onCompleted = null);
public void HideCGImage(Action onCompleted = null);
public void ShakeCGImage(float strength, float time, Action onCompleted = null);

// Option methods
public IDialogueOptionButton AddOptionButton();
public void ClearOptions();

// Effect methods
public void Flash(float inTime, float stay, float outTime, Action onCompleted = null);
public void Transition(float inTime, float stay, float outTime, Action onCompleted = null);

// Control methods
public void Clear();
public void Hide(float fadeOutTime, Action onCompleted = null);
```

### Player 類

```csharp
// Item methods
public void AddItem(int id, int count);
public void RemoveItem(int id, int count);
public bool HasItem(int id);

// Dialogue record methods
public void ReadDialogue(int id);
public bool HasReadDialogue(int id);

// Tag methods
public void AddTag(string tag);
public bool HasTag(params string[] tags);
public void RemoveTag(string tag);

// Data saving methods
public void SetDataToSaveField();
public void LoadDataFromSaveField();
```

### DialogueCommandBase 類

```csharp
// Constructor
public DialogueCommandBase(DialogueData dialogueData, IDialogueView dialogueView);

// Abstract methods
public abstract void Process(Action onCompleted, Action onForceQuit);
```

---

本文檔提供了 DialogueSystem 套件的基本概述和使用指南。對於更詳細的實現細節，請參考源代碼和示例場景。
