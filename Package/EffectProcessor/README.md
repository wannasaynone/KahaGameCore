# KahaGameCore.Package.EffectProcessor 技術文件

## 介紹

KahaGameCore.Package.EffectProcessor 是一個效果處理系統，專為遊戲開發設計。它允許開發者定義、序列化和執行一系列命令（效果），這些命令可以按照特定的時序（timing）組織和觸發。

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

## 進階功能

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

### 使用 Calculator 進行數值計算

在命令執行過程中，可以使用 Calculator.Calculate 方法進行複雜的數值計算，並利用 ProcessData 中的 caster 和 targets：

```csharp
public class DamageCommand : EffectCommandBase
{
    public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
    {
        // 使用 Calculator 計算傷害值
        float damage = Calculator.Calculate(new Calculator.CalculateData
        {
            caster = processData.caster,
            target = processData.targets[0],
            formula = vars[0], // 例如: "Caster.Attack * 1.5 - Target.Defense"
            useBaseValue = false
        });
        
        // 應用傷害
        ApplyDamage(processData.targets[0], (int)damage);
        
        onCompleted?.Invoke();
    }
    
    private void ApplyDamage(IValueContainer target, int damage)
    {
        // 實現傷害邏輯
    }
}
```

Calculator 支持的功能：

1. **基本算術運算**：支持 +, -, *, / 運算符
2. **屬性引用**：可以使用 Caster.屬性名 和 Target.屬性名 引用施放者和目標的屬性值
3. **特殊命令**：
   - `Random(min, max)`：生成指定範圍內的隨機數
   - `Read(tag)`：讀取之前使用 Calculator.Remember 存儲的值

例如，以下是一些有效的公式：

```csharp
// 基本傷害計算
"Caster.Attack - Target.Defense"

// 包含隨機因素的傷害
"Caster.Attack * Random(0.8, 1.2) - Target.Defense"

// 使用記憶值的複雜計算
"Read(PreviousDamage) * 1.5 + Caster.Intelligence"

// 包含括號的先乘除後加減
"(Caster.Attack + Target.Defense)/2 - Target.Defense"
```

使用 Calculator.Remember 存儲值：

```csharp
// 存儲計算結果供後續使用
Calculator.Remember("DamageDealt", damage);
```

這種方式特別適合實現複雜的遊戲機制，如技能傷害計算、屬性加成、狀態效果等。
