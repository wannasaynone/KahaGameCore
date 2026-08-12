# Effects

## 目的

`KahaGameCore.Effects` 是文字效果指令的單一執行核心。它負責解析、序列化、指令定義驗證、依序等待執行，以及把解析錯誤、指令錯誤與取消回報成結構化結果。

## 快速開始

建立 `EffectCommandRegistry`、註冊至少一個 `EffectCommandDefinition`，再用同一 registry 建立 `EffectRuntime`。完整 command 實作見下方「建立與註冊指令」；執行入口為：

```csharp
EffectRuntime runtime = new EffectRuntime(registry);
EffectExecutionResult result = await runtime.ExecuteAsync(
    "DebugLog(ready);",
    new EffectExecutionContext(),
    cancellationToken);
```

caller 必須檢查 `result.IsSuccess`；失敗與取消不是成功的空操作。

## 核心型別

- `EffectRuntime`：對外 façade，協調解析、序列化、驗證與執行；parser／serializer implementation 位於 `Runtime/Internal`。
- `EffectCommandRegistry`：保存可用的 `EffectCommandDefinition`；`TryGetDefinition` 提供 metadata 查詢，重複名稱會在 composition 時立即拋出 `InvalidOperationException`。
- `EffectCommandDefinition`：名稱、顯示名稱、分類與參數 metadata；handler 只由 Effects Runtime 讀取。
- `IEffectCommand`：非同步 handler，接收 `EffectExecutionContext`、參數與 `CancellationToken`。
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

## 建立與註冊指令

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
```

Runtime 只以參數 metadata 的數量執行 arity validation，不解讀 `Kind`。參數名稱與 `Kind` 提供後續 Editor／authoring tooling 使用；具體 expression、parameter、text 或 asset 語意由對應整合模組負責。當前 metadata kind 包含 `Literal`、`NumberExpression`、`ConditionExpression`、`ParameterKey`、`TextKey`、`AssetKey`。

## 執行與錯誤處理

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

`GameFlowSystemBuilder` 會建立一份 registry 與 runtime，GameFlow 和 Game Events 共用同一組 command definitions。專案指令透過 `AddCommandRegistration` 追加：

```csharp
builder.AddCommandRegistration(registry =>
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
        command: new DebugLogCommand())));
```

不要另建第二套 parser、factory 或 callback processor；擴充點就是 `EffectCommandDefinition` 與 `IEffectCommand`。

## 測試

EditMode assembly：`KahaGameCore.Modules.Effects.Tests`

測試涵蓋 quoted delimiter、brace literal、escape、nested parentheses、timing、round-trip、source order、未知 command、arity、例外、取消、non-success diagnostic invariant 與重複註冊。
