# KahaGameCore.Package.EffectProcessor 技術文件

## 目錄
1. [介紹](#介紹)
2. [系統架構](#系統架構)
3. [快速開始](#快速開始)
4. [使用範例](#使用範例)
5. [擴充方式](#擴充方式)
6. [進階功能](#進階功能)
7. [最佳實踐](#最佳實踐)

## 介紹

KahaGameCore.Package.EffectProcessor 是一個強大且靈活的效果處理系統，專為遊戲開發設計。它允許開發者以簡潔的方式定義、序列化和執行一系列命令（效果），這些命令可以按照特定的時序（timing）組織和觸發。

EffectProcessor 的核心優勢：

- **命令模式**：使用命令模式設計，每個效果都是獨立的命令對象
- **工廠模式**：通過工廠模式創建命令實例，便於擴展
- **序列化支持**：可從文本格式反序列化命令，便於設計師編輯
- **時序控制**：按照時序組織和觸發效果
- **非同步處理**：支持非同步命令執行和回調

EffectProcessor 適用於多種遊戲功能實現，如：
- 過場動畫（Cutscene）
- 對話系統
- 技能效果
- 事件序列
- 遊戲流程控制

## 系統架構

EffectProcessor 系統由以下主要組件構成：

### 核心組件

1. **EffectProcessor**：
   - 主要處理類，管理效果的執行和時序
   - 維護時序到效果數據的映射
   - 提供事件回調：OnProcessEnded, OnProcessQuitted, OnProcessDataUpdated

2. **EffectData**：
   - 實現 IProcessable 接口
   - 包含命令實例和參數數組
   - 負責設置處理數據和執行命令

3. **EffectCommandBase**：
   - 所有效果命令的基類
   - 定義 Process 方法，接收參數和回調
   - 包含處理數據引用

4. **ProcessData**：
   - 包含處理上下文信息
   - 包括時序、施放者、目標和條件控制

5. **IValueContainer**：
   - 值容器接口，用於存儲和操作數值
   - 提供標籤值的增加、設置和獲取方法
   - 支持字符串鍵值對的操作

6. **EffectCommandFactoryContainer**：
   - 管理命令工廠的容器
   - 註冊和獲取命令工廠

7. **EffectCommandFactoryBase**：
   - 命令工廠的基類
   - 定義 Create 方法創建命令實例

8. **EffectCommandDeserializer**：
   - 從文本格式反序列化命令
   - 解析時序和命令參數

### 類圖關係

```
EffectProcessor
├── Dictionary<string, List<EffectData>> m_timingToData
├── Dictionary<string, Processor<EffectData>> m_timingToProcesser
├── event Action OnProcessEnded
├── event Action OnProcessQuitted
├── event Action<ProcessData> OnProcessDataUpdated
├── SetUp(Dictionary<string, List<EffectData>> timingToData)
├── Start(ProcessData processData)
└── Dispose()

EffectData : IProcessable
├── EffectCommandBase command
├── string[] vars
├── Process(Action onCompleted, Action onForceQuit)
└── SetProcessData(ProcessData processData)

EffectCommandBase
├── ProcessData processData
├── bool IsIfCommand
└── abstract void Process(string[] vars, Action onCompleted, Action onForceQuit)

ProcessData
├── string timing
├── IValueContainer caster
├── List<IValueContainer> targets
└── int skipIfCount

IValueContainer
├── int GetTotal(string tag, bool baseOnly)
├── Guid Add(string tag, int value)
├── void AddToTemp(Guid guid, int value)
├── void SetTemp(Guid guid, int value)
├── void AddBase(string tag, int value)
├── void SetBase(string tag, int value)
├── void Remove(Guid guid)
├── void AddStringKeyValue(string key, string value)
├── string GetStringKeyValue(string key)
├── void RemoveStringKeyValue(string key)
├── void SetStringKeyValue(string key, string value)
└── Dictionary<string, string> GetAllStringKeyValuePairs()

EffectCommandFactoryContainer
├── Dictionary<string, EffectCommandFactoryBase> m_commandNameToFactory
├── RegisterFactory(string command, EffectCommandFactoryBase factoryBase)
└── EffectCommandBase GetEffectCommand(string commandName)

EffectCommandFactoryBase
└── abstract EffectCommandBase Create()

EffectCommandDeserializer
├── EffectCommandFactoryContainer m_effectCommandFactoryContainer
├── DeserializeAsync(string rawCommandString)
└── Deserialize(string rawCommandString)
```

## 快速開始

### 1. 創建自定義命令

首先，創建一個繼承自 `EffectCommandBase` 的命令類：

```csharp
public class DebugLogCommand : EffectCommandBase
{
    public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
    {
        // 執行命令邏輯
        Debug.Log(vars[0]);
        
        // 完成後調用回調
        onCompleted?.Invoke();
    }
}
```

### 2. 創建命令工廠

為命令創建一個工廠類：

```csharp
public class DebugLogCommandFactory : EffectCommandFactoryBase
{
    public override EffectCommandBase Create()
    {
        return new DebugLogCommand();
    }
}
```

### 3. 註冊命令工廠

```csharp
EffectCommandFactoryContainer factoryContainer = new EffectCommandFactoryContainer();
factoryContainer.RegisterFactory("DebugLog", new DebugLogCommandFactory());
```

### 4. 創建效果數據

```csharp
Dictionary<string, List<EffectProcessor.EffectData>> timingToEffectDatas = 
    new Dictionary<string, List<EffectProcessor.EffectData>>
{
    { 
        "MyTiming", 
        new List<EffectProcessor.EffectData> 
        { 
            new EffectProcessor.EffectData(
                new DebugLogCommand(), 
                new string[] { "Hello, World!" }
            ) 
        } 
    }
};
```

### 5. 設置和啟動處理器

```csharp
EffectProcessor effectProcessor = new EffectProcessor();
effectProcessor.OnProcessEnded += OnProcessEnded;
effectProcessor.SetUp(timingToEffectDatas);
effectProcessor.Start(new ProcessData { timing = "MyTiming" });
```

### 6. 處理完成回調

```csharp
private void OnProcessEnded()
{
    Debug.Log("All effects processed!");
}
```

## 使用範例

### 範例1：從文本反序列化命令

```csharp
// 命令文本格式
string commandText = @"
MyTiming {
    DebugLog(Hello, World!);
    Wait(2.5);
    DebugLog(Processing completed!);
}
";

// 創建和註冊工廠
EffectCommandFactoryContainer factoryContainer = new EffectCommandFactoryContainer();
factoryContainer.RegisterFactory("DebugLog", new DebugLogCommandFactory());
factoryContainer.RegisterFactory("Wait", new WaitCommandFactory());

// 反序列化命令
EffectCommandDeserializer deserializer = new EffectCommandDeserializer(factoryContainer);
Dictionary<string, List<EffectProcessor.EffectData>> timingToEffectDatas = 
    deserializer.Deserialize(commandText);

// 設置和啟動處理器
EffectProcessor effectProcessor = new EffectProcessor();
effectProcessor.SetUp(timingToEffectDatas);
effectProcessor.Start(new ProcessData { timing = "MyTiming" });
```

### 範例2：過場動畫系統

```csharp
// 註冊過場動畫命令
effectCommandFactoryContainer.RegisterFactory("MoveActor", new MoveActorCommandFactory());
effectCommandFactoryContainer.RegisterFactory("PlayAnimation", new PlayAnimationCommandFactory());
effectCommandFactoryContainer.RegisterFactory("SetCamera", new SetCameraCommandFactory());
effectCommandFactoryContainer.RegisterFactory("FadeIn", new FadeInCommandFactory());
effectCommandFactoryContainer.RegisterFactory("FadeOut", new FadeOutCommandFactory());

// 從資源加載過場動畫數據
string cutsceneData = Resources.Load<TextAsset>("Data/MyCutscene").text;
Dictionary<string, List<EffectProcessor.EffectData>> cutsceneEffects = 
    deserializer.Deserialize(cutsceneData);

// 播放過場動畫
effectProcessor.SetUp(cutsceneEffects);
effectProcessor.Start(new ProcessData { timing = "IntroScene" });
```

### 範例3：技能效果系統

```csharp
// 創建技能效果
Dictionary<string, List<EffectProcessor.EffectData>> skillEffects = 
    new Dictionary<string, List<EffectProcessor.EffectData>>
{
    { 
        "CastStart", 
        new List<EffectProcessor.EffectData> 
        { 
            new EffectProcessor.EffectData(new PlayParticleCommand(), new string[] { "CastParticle" }),
            new EffectProcessor.EffectData(new PlaySoundCommand(), new string[] { "CastSound" })
        } 
    },
    { 
        "OnHit", 
        new List<EffectProcessor.EffectData> 
        { 
            new EffectProcessor.EffectData(new DamageCommand(), new string[] { "50" }),
            new EffectProcessor.EffectData(new ApplyStatusCommand(), new string[] { "Stun", "2" })
        } 
    }
};

// 設置技能效果
EffectProcessor skillProcessor = new EffectProcessor();
skillProcessor.SetUp(skillEffects);

// 施放技能
skillProcessor.Start(new ProcessData 
{ 
    timing = "CastStart",
    caster = playerCharacter,
    targets = new List<IValueContainer> { enemyCharacter }
});

// 當命中目標時
skillProcessor.Start(new ProcessData 
{ 
    timing = "OnHit",
    caster = playerCharacter,
    targets = new List<IValueContainer> { enemyCharacter }
});
```

### 範例4：對話系統

```csharp
// 定義對話數據
string dialogueData = @"
Dialogue1 {
    ShowDialogueBox();
    SetSpeaker(Hero);
    SetDialogueText(Hello, I'm looking for the ancient artifact.);
    Wait(2);
    SetSpeaker(Villager);
    SetDialogueText(You should check the old ruins to the north.);
    Wait(2);
    HideDialogueBox();
}
";

// 註冊對話命令
effectCommandFactoryContainer.RegisterFactory("ShowDialogueBox", new ShowDialogueBoxCommandFactory());
effectCommandFactoryContainer.RegisterFactory("SetSpeaker", new SetSpeakerCommandFactory());
effectCommandFactoryContainer.RegisterFactory("SetDialogueText", new SetDialogueTextCommandFactory());
effectCommandFactoryContainer.RegisterFactory("Wait", new WaitCommandFactory());
effectCommandFactoryContainer.RegisterFactory("HideDialogueBox", new HideDialogueBoxCommandFactory());

// 反序列化對話
Dictionary<string, List<EffectProcessor.EffectData>> dialogueEffects = 
    deserializer.Deserialize(dialogueData);

// 播放對話
effectProcessor.SetUp(dialogueEffects);
effectProcessor.Start(new ProcessData { timing = "Dialogue1" });
```

## 擴充方式

EffectProcessor 系統設計為高度可擴展，以下是幾種擴展方式：

### 1. 創建自定義命令

創建新的命令是最常見的擴展方式：

```csharp
public class MyCustomCommand : EffectCommandBase
{
    public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
    {
        // 實現自定義邏輯
        
        // 完成後調用回調
        onCompleted?.Invoke();
        
        // 或在需要強制退出時
        // onForceQuit?.Invoke();
    }
}

public class MyCustomCommandFactory : EffectCommandFactoryBase
{
    public override EffectCommandBase Create()
    {
        return new MyCustomCommand();
    }
}
```

### 2. 創建條件命令

條件命令可以控制後續命令的執行：

```csharp
public class IfCommand : EffectCommandBase
{
    public IfCommand()
    {
        IsIfCommand = true; // 標記為條件命令
    }
    
    public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
    {
        bool condition = EvaluateCondition(vars);
        
        if (condition)
        {
            onCompleted?.Invoke(); // 繼續執行後續命令
        }
        else
        {
            processData.skipIfCount++; // 增加跳過計數
            onCompleted?.Invoke(); // 繼續但會跳過非條件命令
        }
    }
    
    private bool EvaluateCondition(string[] vars)
    {
        // 實現條件評估邏輯
        return true;
    }
}
```

### 3. 擴展 IValueContainer 實現

創建自定義值容器實現：

```csharp
public class GameCharacter : MonoBehaviour, IValueContainer
{
    private Dictionary<string, int> baseValues = new Dictionary<string, int>();
    private Dictionary<Guid, KeyValuePair<string, int>> guidToValue = new Dictionary<Guid, KeyValuePair<string, int>>();
    private Dictionary<Guid, int> tempValues = new Dictionary<Guid, int>();
    private Dictionary<string, string> stringKeyValues = new Dictionary<string, string>();
    
    // 實現 IValueContainer 接口方法
    public int GetTotal(string tag, bool baseOnly)
    {
        // 實現邏輯
        int total = 0;
        
        if (baseValues.ContainsKey(tag))
        {
            total += baseValues[tag];
        }
        
        if (!baseOnly)
        {
            foreach (var kvp in guidToValue)
            {
                if (kvp.Value.Key == tag)
                {
                    int value = kvp.Value.Value;
                    if (tempValues.ContainsKey(kvp.Key))
                    {
                        value = tempValues[kvp.Key];
                    }
                    total += value;
                }
            }
        }
        
        return total;
    }
    
    public Guid Add(string tag, int value)
    {
        Guid guid = Guid.NewGuid();
        guidToValue.Add(guid, new KeyValuePair<string, int>(tag, value));
        return guid;
    }
    
    // 實現其他接口方法...
}
```

### 4. 自定義反序列化

擴展反序列化邏輯以支持更複雜的格式：

```csharp
public class CustomEffectDeserializer : EffectCommandDeserializer
{
    public CustomEffectDeserializer(EffectCommandFactoryContainer factoryContainer) 
        : base(factoryContainer)
    {
    }
    
    public Dictionary<string, List<EffectProcessor.EffectData>> DeserializeFromJson(string jsonData)
    {
        // 從 JSON 格式反序列化
        Dictionary<string, List<EffectProcessor.EffectData>> result = 
            new Dictionary<string, List<EffectProcessor.EffectData>>();
            
        // 解析 JSON 並創建效果數據
        
        return result;
    }
    
    public Dictionary<string, List<EffectProcessor.EffectData>> DeserializeFromXml(string xmlData)
    {
        // 從 XML 格式反序列化
        Dictionary<string, List<EffectProcessor.EffectData>> result = 
            new Dictionary<string, List<EffectProcessor.EffectData>>();
            
        // 解析 XML 並創建效果數據
        
        return result;
    }
}
```

## 進階功能

### 非同步命令執行

命令可以實現非同步操作，只需在完成時調用回調：

```csharp
public class AsyncCommand : EffectCommandBase
{
    public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
    {
        StartCoroutine(AsyncOperation(vars, onCompleted));
    }
    
    private IEnumerator AsyncOperation(string[] vars, Action onCompleted)
    {
        // 執行非同步操作
        yield return new WaitForSeconds(2f);
        
        // 完成後調用回調
        onCompleted?.Invoke();
    }
}
```

### 命令參數解析

處理複雜的命令參數：

```csharp
public class ComplexCommand : EffectCommandBase
{
    public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
    {
        // 解析數值參數
        float value = float.Parse(vars[0]);
        
        // 解析向量參數
        Vector3 position = ParseVector3(vars[1]);
        
        // 解析目標參數
        string targetId = vars[2];
        GameObject target = GameObject.Find(targetId);
        
        // 執行命令邏輯
        
        onCompleted?.Invoke();
    }
    
    private Vector3 ParseVector3(string vectorString)
    {
        string[] components = vectorString.Split(',');
        return new Vector3(
            float.Parse(components[0]),
            float.Parse(components[1]),
            float.Parse(components[2])
        );
    }
}
```

### 命令組合

創建組合命令以執行多個子命令：

```csharp
public class CompositeCommand : EffectCommandBase
{
    private List<EffectCommandBase> subCommands = new List<EffectCommandBase>();
    
    public CompositeCommand(params EffectCommandBase[] commands)
    {
        subCommands.AddRange(commands);
    }
    
    public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
    {
        // 創建子命令處理器
        Processor<EffectProcessor.EffectData> processor = new Processor<EffectProcessor.EffectData>(
            subCommands.Select(cmd => new EffectProcessor.EffectData(cmd, vars)).ToArray()
        );
        
        // 啟動處理器
        processor.Start(onCompleted, onForceQuit);
    }
}
```

### 命令參數綁定

實現參數綁定以動態獲取值：

```csharp
public class BindableCommand : EffectCommandBase
{
    public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
    {
        // 檢查參數是否為綁定表達式
        for (int i = 0; i < vars.Length; i++)
        {
            if (vars[i].StartsWith("$"))
            {
                // 解析綁定表達式
                string bindingExpression = vars[i].Substring(1);
                vars[i] = ResolveBinding(bindingExpression);
            }
        }
        
        // 執行命令邏輯
        
        onCompleted?.Invoke();
    }
    
    private string ResolveBinding(string bindingExpression)
    {
        // 解析綁定表達式
        // 例如：$caster.hp 或 $target[0].name
        
        string[] parts = bindingExpression.Split('.');
        
        if (parts[0] == "caster")
        {
            return processData.caster.GetStringKeyValue(parts[1]);
        }
        else if (parts[0].StartsWith("target"))
        {
            int index = 0;
            if (parts[0].Contains("[") && parts[0].Contains("]"))
            {
                string indexStr = parts[0].Substring(parts[0].IndexOf("[") + 1, parts[0].IndexOf("]") - parts[0].IndexOf("[") - 1);
                index = int.Parse(indexStr);
            }
            
            if (index < processData.targets.Count)
            {
                return processData.targets[index].GetStringKeyValue(parts[1]);
            }
        }
        
        return "";
    }
}
```

## 最佳實踐

### 命令設計原則

1. **單一職責**：每個命令應專注於單一功能
2. **參數驗證**：在命令開始時驗證參數
3. **錯誤處理**：適當處理異常情況
4. **回調調用**：確保在所有路徑上都調用回調
5. **資源釋放**：在命令完成時釋放資源

### 性能優化

1. **對象池**：使用對象池減少垃圾回收
   ```csharp
   public class CommandPool<T> where T : EffectCommandBase, new()
   {
       private Stack<T> pool = new Stack<T>();
       
       public T Get()
       {
           if (pool.Count > 0)
           {
               return pool.Pop();
           }
           return new T();
       }
       
       public void Return(T command)
       {
           pool.Push(command);
       }
   }
   ```

2. **參數緩存**：緩存解析後的參數
   ```csharp
   public class CachedCommand : EffectCommandBase
   {
       private Dictionary<string, object> paramCache = new Dictionary<string, object>();
       
       public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
       {
           // 緩存參數
           if (!paramCache.ContainsKey(vars[0]))
           {
               paramCache[vars[0]] = ParseParameter(vars[0]);
           }
           
           // 使用緩存的參數
           object param = paramCache[vars[0]];
           
           // 執行命令邏輯
           
           onCompleted?.Invoke();
       }
       
       private object ParseParameter(string param)
       {
           // 解析參數
           return null;
       }
   }
   ```

3. **延遲加載**：按需加載資源
   ```csharp
   public class LazyLoadCommand : EffectCommandBase
   {
       private static Dictionary<string, UnityEngine.Object> resourceCache = 
           new Dictionary<string, UnityEngine.Object>();
       
       public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
       {
           string resourcePath = vars[0];
           
           // 延遲加載資源
           if (!resourceCache.ContainsKey(resourcePath))
           {
               resourceCache[resourcePath] = Resources.Load(resourcePath);
           }
           
           // 使用資源
           UnityEngine.Object resource = resourceCache[resourcePath];
           
           // 執行命令邏輯
           
           onCompleted?.Invoke();
       }
   }
   ```

4. **命令批處理**：合併相似命令減少開銷
   ```csharp
   public class BatchCommand : EffectCommandBase
   {
       public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
       {
           // 解析批處理參數
           string[] commandIds = vars[0].Split(',');
           
           // 創建批處理命令
           List<EffectProcessor.EffectData> batchCommands = new List<EffectProcessor.EffectData>();
           
           foreach (string commandId in commandIds)
           {
               // 創建命令
               EffectCommandBase command = CreateCommand(commandId);
               batchCommands.Add(new EffectProcessor.EffectData(command, new string[0]));
           }
           
           // 創建處理器
           Processor<EffectProcessor.EffectData> processor = 
               new Processor<EffectProcessor.EffectData>(batchCommands.ToArray());
           
           // 啟動處理器
           processor.Start(onCompleted, onForceQuit);
       }
       
       private EffectCommandBase CreateCommand(string commandId)
       {
           // 創建命令
           return null;
       }
   }
   ```

### 調試技巧

1. **日誌命令**：添加日誌命令輸出調試信息
   ```csharp
   public class LogCommand : EffectCommandBase
   {
       public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
       {
           string message = vars[0];
           string category = vars.Length > 1 ? vars[1] : "Info";
           
           switch (category.ToLower())
           {
               case "error":
                   Debug.LogError(message);
                   break;
               case "warning":
                   Debug.LogWarning(message);
                   break;
               default:
                   Debug.Log(message);
                   break;
           }
           
           onCompleted?.Invoke();
       }
   }
   ```

2. **命令可視化**：創建編輯器工具可視化命令序列
   ```csharp
   #if UNITY_EDITOR
   public class EffectProcessorDebugger : EditorWindow
   {
       [MenuItem("Tools/Effect Processor Debugger")]
       public static void ShowWindow()
       {
           GetWindow<EffectProcessorDebugger>("Effect Processor");
       }
       
       private void OnGUI()
       {
           // 顯示當前命令序列
           // 顯示處理狀態
           // 提供調試控制
       }
   }
   #endif
   ```

3. **步進執行**：實現步進執行模式便於調試
   ```csharp
   public class DebugProcessor<T> : Processor<T> where T : IProcessable
   {
       private bool isStepMode = false;
       private bool isWaitingForStep = false;
       
       public DebugProcessor(T[] items) : base(items)
       {
       }
       
       public void EnableStepMode(bool enable)
       {
           isStepMode = enable;
       }
       
       public void Step()
       {
           if (isStepMode && isWaitingForStep)
           {
               isWaitingForStep = false;
               // 繼續執行下一步
           }
       }
       
       protected override void RunProcessableItems()
       {
           if (isStepMode)
           {
               isWaitingForStep = true;
               // 等待步進命令
           }
           else
           {
               base.RunProcessableItems();
           }
       }
   }
   ```

4. **狀態檢查**：添加命令檢查系統狀態
   ```csharp
   public class AssertCommand : EffectCommandBase
   {
       public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
       {
           string condition = vars[0];
           string message = vars.Length > 1 ? vars[1] : "Assertion failed";
           
           bool result = EvaluateCondition(condition);
           
           if (!result)
           {
               Debug.LogError("Assert: " + message);
               onForceQuit?.Invoke();
           }
           else
           {
               onCompleted?.Invoke();
           }
       }
       
       private bool EvaluateCondition(string condition)
       {
           // 評估條件
           return true;
       }
   }
   ```

### 擴展建議

1. **命令分類**：按功能領域組織命令
   ```csharp
   namespace KahaGameCore.Combat.Processor.EffectProcessor.Commands
   {
       namespace Animation
       {
           // 動畫相關命令
       }
       
       namespace Audio
       {
           // 音頻相關命令
       }
       
       namespace UI
       {
           // UI 相關命令
       }
       
       namespace Logic
       {
           // 邏輯控制命令
       }
   }
   ```

2. **參數類型化**：實現強類型參數系統
   ```csharp
   public abstract class TypedEffectCommandBase<T1> : EffectCommandBase
   {
       public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
       {
           T1 param1 = ParseParam<T1>(vars[0]);
           ProcessTyped(param1, onCompleted, onForceQuit);
       }
       
       protected abstract void ProcessTyped(T1 param1, Action onCompleted, Action onForceQuit);
       
       protected T ParseParam<T>(string value)
       {
           // 解析參數
           return default(T);
       }
   }
   
   public abstract class TypedEffectCommandBase<T1, T2> : EffectCommandBase
   {
       public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
       {
           T1 param1 = ParseParam<T1>(vars[0]);
           T2 param2 = ParseParam<T2>(vars[1]);
           ProcessTyped(param1, param2, onCompleted, onForceQuit);
       }
       
       protected abstract void ProcessTyped(T1 param1, T2 param2, Action onCompleted, Action onForceQuit);
       
       protected T ParseParam<T>(string value)
       {
           // 解析參數
           return default(T);
       }
   }
   ```

3. **條件系統**：擴展條件系統支持複雜邏輯
   ```csharp
   public class ConditionSystem
   {
       public static bool Evaluate(string condition, ProcessData processData)
       {
           // 解析條件表達式
           // 支持比較操作：==, !=, >, <, >=, <=
           // 支持邏輯操作：&&, ||, !
           // 支持變量引用：$caster.hp, $target[0].mp
           
           return true;
       }
   }
   ```

4. **事件集成**：與遊戲事件系統集成
   ```csharp
   public class EventCommand : EffectCommandBase
   {
       public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
       {
           string eventName = vars[0];
           
           // 觸發遊戲事件
           EventManager.TriggerEvent(eventName);
           
           onCompleted?.Invoke();
       }
   }
   ```

5. **編輯器支持**：創建自定義編輯器便於設計
   ```csharp
   #if UNITY_EDITOR
   public class EffectCommandEditor : EditorWindow
   {
       [MenuItem("Tools/Effect Command Editor")]
       public static void ShowWindow()
       {
           GetWindow<EffectCommandEditor>("Effect Commands");
       }
       
       private void OnGUI()
       {
           // 提供命令編輯界面
           // 支持拖放操作
           // 提供參數編輯
           // 提供預覽功能
       }
   }
   #endif
   ```

---

通過本文檔，您應該能夠理解 KahaGameCore.Package.EffectProcessor 系統的核心概念、使用方法和擴展方式。這個系統提供了一個強大的框架，用於實現各種遊戲效果和序列。通過創建自定義命令和擴展現有功能，您可以根據項目需求靈活應用這個系統。
