# Effects

## 用途

`KahaGameCore.Effects` 是文字效果指令的單一執行核心。它負責解析、序列化、指令定義驗證、依序等待執行，以及把解析錯誤、指令錯誤與取消回報成結構化結果。

## 第一次使用：文字文件 → 可用 runtime

呼叫端 asmdef 引用 `KahaGameCore.Modules.Effects` 與 `UniTask`。先建立 command handler：

```csharp
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;

public sealed class DebugLogCommand : IEffectCommand
{
    public UniTask ExecuteAsync(
        EffectExecutionContext context,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        UnityEngine.Debug.Log(arguments[0]);
        return UniTask.CompletedTask;
    }
}
```

將命令文字存成 `FirstRun.effects.txt`（副檔名只供作者辨識，Unity 會匯入為 `TextAsset`）：

```text
DebugLog(ready);
```

再由場景 composition root 註冊 definition、建立 runtime，並把文字資產內容交給它：

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;
using UnityEngine;

public sealed class EffectTextFileExample : MonoBehaviour
{
    [SerializeField] private TextAsset effectFile;

    private CancellationTokenSource lifetime;
    private EffectRuntime runtime;

    private void Awake()
    {
        if (effectFile == null)
        {
            throw new InvalidOperationException("Effect File is required.");
        }

        lifetime = new CancellationTokenSource();
        EffectCommandRegistry registry = new EffectCommandRegistry();
        registry.Register(new EffectCommandDefinition(
            name: "DebugLog",
            displayName: "Debug Log",
            category: "Debug",
            parameters: new[]
            {
                new EffectCommandParameterDefinition(
                    "message",
                    EffectCommandParameterKind.Literal)
            },
            command: new DebugLogCommand()));
        runtime = new EffectRuntime(registry);
    }

    public void Run()
    {
        RunAsync().Forget();
    }

    private async UniTaskVoid RunAsync()
    {
        EffectExecutionResult result = await runtime.ExecuteAsync(
            effectFile.text,
            new EffectExecutionContext(),
            lifetime.Token);

        if (result.Status == EffectExecutionStatus.Cancelled)
        {
            return;
        }

        if (!result.IsSuccess)
        {
            Debug.LogError(result.FormatDiagnostic());
        }
    }

    private void OnDestroy()
    {
        lifetime?.Cancel();
        lifetime?.Dispose();
    }
}
```

把 `FirstRun.effects.txt` 指定給 Inspector 的 Effect File，呼叫 `Run()` 後 Console 會輸出 `ready`。Effects runtime 不擁有檔案位置、catalog 或 Unity asset loading；它的輸入就是 command source 字串，因此 `TextAsset.text`、網路回應或其他文字來源都走同一個入口。Caller 必須檢查結果；失敗與取消不是成功的空操作。

## 核心型別

- `EffectRuntime`：對外 façade，協調解析、序列化、驗證與執行；parser／serializer implementation 位於 `Runtime/Internal`。
- `EffectCommandRegistry`：保存可用的 `EffectCommandDefinition`；`TryGetDefinition` 提供 metadata 查詢，重複名稱會在 composition 時立即拋出 `InvalidOperationException`。
- `EffectCommandDefinition`：名稱、顯示名稱、分類與參數 metadata；handler 只由 Effects Runtime 讀取。
- `IEffectCommand`：非同步 handler，接收 `EffectExecutionContext`、參數與 `CancellationToken`。
- `IEffectCommandModuleFactory`：同時公開 Editor descriptors，並以 composition root 已有的 services 建立 runtime module。
- `EffectCommandBootstrapper`：讀取 Catalog configuration，只建立被啟用的 modules 與 commands。
- `EffectExecutionResult`：區分 `Succeeded`、`Failed`、`Cancelled`；非成功結果必須附 `EffectDiagnostic`。
- `EffectExecutionContext`：Caster、Targets 與每次執行的 CustomData，不保存全域遊戲狀態。

Effects 不擁有 Game Event queue；跨事件 FIFO、條件與 priority 屬於 `KahaGameCore.GameEvents`。

## 指令格式

一般 GameFlow／Game Event 使用平面格式：

```text
SetParameter(machine_01_stage,1);ShowHint(machine_started);
```

需要指定 timing 時可使用 block：

```text
Before{Record(start);}After{Record(done);}
```

參數可加雙引號；逗號、分號、括號、大括號與跳脫字元會由 codec 保留。`Parse` 後再 `Serialize` 必須能 round-trip。

## Command definition 規則

Runtime 只以參數 metadata 的數量執行 arity validation，不解讀 `Kind`。參數名稱與 `Kind` 提供後續 Editor／authoring tooling 使用；具體 expression、parameter、text 或 asset 語意由對應整合模組負責。當前 metadata kind 包含 `Literal`、`NumberExpression`、`ConditionExpression`、`ParameterKey`、`TextKey`、`AssetKey`。

## 失敗與取消處理

```csharp
EffectRuntime runtime = new EffectRuntime(registry);
EffectExecutionResult result = await runtime.ExecuteAsync(
    "DebugLog(ready);",
    new EffectExecutionContext(),
    cancellationToken);

if (result.Status == EffectExecutionStatus.Cancelled)
{
    throw new OperationCanceledException(cancellationToken);
}

if (!result.IsSuccess)
{
    UnityEngine.Debug.LogError(result.FormatDiagnostic());
}
```

Runtime 嚴格依 source order 等待每個 handler。未知 command、參數數量不符、handler 例外與語法錯誤不會被靜默略過。

## GameFlow 整合

`GameFlowSystemBuilder` 會建立一份 registry 與 runtime，Phase control、Action menu 與 Game Events 共用同一組 command definitions。Catalog configuration 是 module factory 與 command 的啟用清單：

```csharp
builder
    .WithEffectCommandConfiguration(gameEventCatalog.CommandConfiguration);
```

不要直接修改 registry，也不要另建第二套 parser 或 callback processor；擴充點是 `IEffectCommandModuleFactory`、`EffectCommandDescriptor` 與 `IEffectCommand`。

## 讓 Game Event Editor 看見專案 Command

在擁有 Command 的 runtime asmdef 實作 factory；不要建立全域 editor initializer：

```csharp
public sealed class ProjectEffectCommandModuleFactory :
    IEffectCommandModuleFactory
{
    public IReadOnlyList<EffectCommandDescriptor> GetDescriptors()
        => ProjectEffectCommandManifest.Descriptors;

    public IEffectCommandModule Create(EffectCommandServiceRegistry services)
        => new ProjectEffectCommandModule(
            services.GetRequired<ProjectCommandServices>());
}
```

回到 Game Event Editor 的 `Commands` TAB 勾選 asmdef 與需要的 Commands。Catalog 會保存 factory type；啟動時 `EffectCommandBootstrapper` 只建立被啟用的 modules 與 commands，缺 factory、service 或 metadata 不一致會直接讓初始化失敗。

## 測試

EditMode assembly：`KahaGameCore.Modules.Effects.Tests`

測試涵蓋 quoted delimiter、brace literal、escape、nested parentheses、timing、round-trip、source order、未知 command、arity、例外、取消、non-success diagnostic invariant 與重複註冊。
