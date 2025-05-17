# Game Flow System

這個系統允許在Unity編輯器中自定義遊戲開始流程，無需修改代碼。

## 概述

Game Flow系統使用ScriptableObject來定義一系列可自定義的步驟，這些步驟將按順序執行。每個步驟都是一個獨立的ScriptableObject，可以在Unity編輯器中配置。

## 如何使用

### 創建新的遊戲流程

1. 在Project視窗中右鍵點擊 -> Create -> Game Flow -> Sequence
2. 為新的流程序列命名（例如 "CustomGameFlow"）
3. 在Inspector中設置序列的名稱和描述
4. 使用 "+" 按鈕添加步驟，或點擊 "Create Default Game Flow" 按鈕創建默認流程

### 創建步驟

可以創建以下類型的步驟：

1. 在Project視窗中右鍵點擊 -> Create -> Game Flow -> Steps -> 選擇步驟類型
2. 配置步驟的屬性
3. 將步驟添加到流程序列中

### 在代碼中使用自定義流程

```csharp
// 獲取流程序列資產
GameFlowSequence customFlow = Resources.Load<GameFlowSequence>("Path/To/YourGameFlow");

// 在CombatState_GameStartFlowController中使用
combatStateGameStartFlowController.StartProcess(customFlow);
```

## 可用步驟類型

### LoadLevelStep

加載指定的關卡。

屬性：
- `levelPath`: 要加載的關卡路徑

### PlayCutsceneStep

播放過場動畫。

屬性：
- `cutsceneName`: 要播放的過場動畫名稱
- `skipCutscene`: 是否跳過過場動畫（用於測試）

### ActivateGameObjectStep

激活或停用遊戲對象。

屬性：
- `targetType`: 目標類型（InGameView或Custom）
- `customGameObjectPath`: 如果targetType為Custom，則為遊戲對象的路徑
- `activate`: 是否激活遊戲對象

### StartLevelStep

啟動關卡。

### WaitForSecondsStep

等待指定的秒數。

屬性：
- `waitTime`: 等待時間（秒）

## 擴展系統

要添加新的步驟類型：

1. 創建一個繼承自GameFlowStep的新類
2. 實現Execute方法
3. 在完成時調用CompleteStep方法

例如：

```csharp
[CreateAssetMenu(fileName = "MyCustomStep", menuName = "Game Flow/Steps/My Custom Step")]
public class MyCustomStep : GameFlowStep
{
    public string customParameter;
    
    private FlowContext currentContext;
    
    public override void Execute(FlowContext context)
    {
        currentContext = context;
        
        // 執行自定義邏輯
        Debug.Log($"Executing custom step with parameter: {customParameter}");
        
        // 完成步驟
        CompleteStep(currentContext);
    }
}
```

## 注意事項

- 確保每個步驟在完成時調用CompleteStep方法，否則流程將不會繼續
- 步驟按照在序列中的順序執行
- 可以在編輯器中重新排序步驟
- 可以創建多個不同的流程序列，並根據需要在運行時切換
