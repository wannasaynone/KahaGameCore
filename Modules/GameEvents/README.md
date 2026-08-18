# Game Events

## 用途

Game Events 把 JSON 事件文件連到 Effects runtime。它依 `TriggerTiming` 過濾事件、先拍下符合 condition 的候選集合，再按 priority 由高至低與 catalog 輸入順序執行。所有 direct trigger 與 timing trigger 共用同一條 FIFO queue。

## 第一次使用：先準備事件文件

呼叫端 asmdef 引用 `KahaGameCore.Modules.GameEvents`、`KahaGameCore.Modules.Effects`、`KahaGameCore.Modules.Parameters` 與 `UniTask`。完整可掛載範例位於下一節；這裡先看事件文件如何對應 runtime 物件。

準備 `.gameevent.json`：

```json
{
  "SchemaVersion": 1,
  "DocumentGuid": "b21ecb37-f6a7-413f-86b7-532d04c31f51",
  "DisplayName": "開門",
  "TriggerTiming": "Interact:Door",
  "Condition": "$DoorOpen == false",
  "Priority": 100,
  "Commands": "OpenDoor();"
}
```

事件文件不會自行執行。專案啟動／場景組裝程式建立以下四個物件後才能觸發：

```csharp
var codec = new GameEventDocumentJsonCodec();
var catalog = new GameEventCatalog(gameEventFiles, codec);
var runner = new GameEventRunner(catalog, effectRuntime, parameters, codec);
var context = new EventContext(cancellationToken);

await runner.TriggerAsync("Interact:Door", context);
```

上面四行是依賴關係摘要；其中 `gameEventFiles`、`effectRuntime`、`parameters` 與 `cancellationToken` 的完整建立方式，請直接照下一節的 `DoorGameEventExample`，不要把未定義的變數原樣複製。

相關物件的職責：

| 物件 | 功能 | 直接依賴 |
|---|---|---|
| `GameEventDocumentJsonCodec` | 把 Game Event JSON 解析成 `GameEventDocument`，並驗證必要欄位、schema version 與 `DocumentGuid`；也能把 document 寫回 JSON。 | JsonFx |
| `GameEventCatalog` | 啟動時讀取 `gameEventFiles`，建立可依 `TriggerTiming` 篩選的事件清單，保留輸入順序，並拒絕重複的 `DocumentGuid`。 | `gameEventFiles`、建立時使用 `codec` |
| `ParameterStore` | 保存由 `ParameterDefinition` 宣告的 Parameter 與目前值。Runner 以 definition 的 `Key` 查找 condition 中的 `$ParameterKey`。 | `ParameterDefinition` 集合；每個 definition 必須有唯一 `Key` |
| `GameEventRunner` | 接收觸發要求、依 timing 與 Parameter condition 選出事件、按 priority 和輸入順序排序，再透過 `EffectRuntime` 依序執行 commands；所有要求共用同一條 FIFO queue。 | `catalog`、`effectRuntime`、`parameters`；直接執行單一檔案時使用 `codec` |
| `EventContext` | 保存這一次執行的取消權杖，以及傳給 Effects commands 的 `EffectExecutionContext`（例如 Caster／Targets）。它不保存事件清單或 Parameter 狀態。 | `cancellationToken`；可選的 `EffectExecutionContext` |

建立與執行的依賴方向：

```text
gameEventFiles ──→ GameEventCatalog ──┐
        codec ──↗                     │
                                      ├─→ GameEventRunner
effectRuntime ────────────────────────┤       │
parameterDefinitions → ParameterStore ───────┘
                                              │ TriggerAsync / RunAsync
cancellationToken ─→ EventContext ────────────┘
```

`GameEventCatalog` 建立完成後保存的是解析完成的 documents，不會保留 `codec`。`GameEventRunner` 持有的 `codec` 只供 `RunAsync(file, context)` 解析指定的單一檔案；`TriggerAsync(timing, context)` 使用 catalog 中已解析的 documents。

## 第一次完整示範：互動後開門

以下範例可直接對應前面的 `.gameevent.json`。場景物件呼叫 `InteractWithDoor()` 後，Runner 會找出 `TriggerTiming` 為 `Interact:Door`、且 condition 成立的事件，最後執行已註冊的 `OpenDoor` command。

Parameter name 在 Parameters module 中稱為 `Key`。這個示範將它集中在 `DoorParameterNames.DoorOpen`，避免 command、definition 與查詢各自寫一份字串：

| 使用位置 | 寫法 | 意義 |
|---|---|---|
| `ParameterDefinition` | `key: DoorParameterNames.DoorOpen` | 向 `ParameterStore` 宣告 Parameter identity |
| Game Event condition | `$DoorOpen == false` | `$` 後的名稱會查找同一個 `Key` |
| Command／gameplay code | `Set(DoorParameterNames.DoorOpen, ...)` | 讀寫同一個 Parameter |
| `displayName` | `"門已開啟"` | 只供作者與 UI 顯示，不參與查找 |

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;
using KahaGameCore.GameEvents;
using KahaGameCore.Parameters;
using UnityEngine;

public static class DoorParameterNames
{
    public const string DoorOpen = "DoorOpen";
}

public sealed class OpenDoorCommand : IEffectCommand
{
    private readonly ParameterStore parameters;

    public OpenDoorCommand(ParameterStore parameters)
    {
        this.parameters = parameters;
    }

    public UniTask ExecuteAsync(
        EffectExecutionContext context,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        parameters.Set(DoorParameterNames.DoorOpen, true);
        return UniTask.CompletedTask;
    }
}

public sealed class DoorGameEventExample : MonoBehaviour
{
    [SerializeField]
    private TextAsset[] gameEventFiles;

    private CancellationTokenSource lifetime;
    private ParameterStore parameters;
    private GameEventRunner runner;

    private void Awake()
    {
        lifetime = new CancellationTokenSource();

        parameters = new ParameterStore(new[]
        {
            ParameterDefinition.Bool(
                key: DoorParameterNames.DoorOpen,
                displayName: "門已開啟",
                initialValue: false)
        });

        EffectCommandRegistry registry = new EffectCommandRegistry();
        registry.Register(new EffectCommandDefinition(
            name: "OpenDoor",
            displayName: "Open Door",
            category: "Door",
            parameters: Array.Empty<EffectCommandParameterDefinition>(),
            command: new OpenDoorCommand(parameters)));

        EffectRuntime effectRuntime = new EffectRuntime(registry);
        GameEventDocumentJsonCodec codec = new GameEventDocumentJsonCodec();
        GameEventCatalog catalog = new GameEventCatalog(gameEventFiles, codec);
        runner = new GameEventRunner(catalog, effectRuntime, parameters, codec);
    }

    // 可直接綁到 Button.onClick 或其他 UnityEvent。
    public void InteractWithDoor()
    {
        InteractWithDoorAsync().Forget();
    }

    private async UniTask InteractWithDoorAsync()
    {
        EventContext context = new EventContext(lifetime.Token);
        await runner.TriggerAsync("Interact:Door", context);

        Debug.Log(
            $"DoorOpen = {parameters.GetBool(DoorParameterNames.DoorOpen)}");
        // 輸出：DoorOpen = True
    }

    private void OnDestroy()
    {
        lifetime?.Cancel();
        lifetime?.Dispose();
    }
}
```

使用步驟：

1. 將前面的 JSON 儲存為 `.gameevent.json`，讓 Unity 以 `TextAsset` 匯入。
2. 在場景物件加入 `DoorGameEventExample`。
3. 把事件檔拖進 `Game Event Files`。
4. 將 Button 或互動元件的 UnityEvent 綁到 `InteractWithDoor()`。
5. 互動時 condition 先讀取 `$DoorOpen`；第一次為 `false`，所以執行 `OpenDoor()` 並把 Parameter 設為 `true`。再次互動時 condition 不成立，不會重複執行 command。

這個示範中的 `OpenDoorCommand` 是專案 command。Game Events 不內建遊戲行為；所有 `Commands` 中使用的名稱都必須先以 `EffectCommandDefinition` 註冊進同一個 `EffectCommandRegistry`。

### 使用 SceneGameEventTrigger

若場景物件固定執行一份事件檔，可掛 `SceneGameEventTrigger`，在 Inspector 指定該 `TextAsset` 與允許進入的 Collider Layers，並由 composition root 完成初始化：

```csharp
sceneGameEventTrigger.Initialize(
    runner,
    new EventContext(lifetime.Token));
```

初始化後，允許 Layer 的 Collider 進入時，`OnTriggerEnter` 會呼叫 `runner.RunAsync(file, context)`；也可以由 UnityEvent 手動綁定 `SceneGameEventTrigger.Trigger()`。這條路徑會直接執行指定文件並檢查 condition，但不比對文件的 `TriggerTiming`。

`SceneGameEventTrigger` 不保存碰撞歷史，也不自行判斷「只能觸發一次」。需要跨存檔保持的一次性事件應使用 Parameter 作為權威狀態：

```json
{
  "SchemaVersion": 1,
  "DocumentGuid": "b21ecb37-f6a7-413f-86b7-532d04c31f51",
  "DisplayName": "首次進入倉庫",
  "TriggerTiming": "",
  "Condition": "$WarehouseEntered == false",
  "Priority": 0,
  "Commands": "StartDialogue(12);SetParameter(WarehouseEntered,true);"
}
```

`WarehouseEntered` 必須由 Parameter Table 宣告。使用 `GameSaveCoordinator` 存檔時，它會先等待 Game Event queue 清空，再保存 Parameters；載入後 condition 因此仍能阻止事件重複執行。Collider 接觸本身是瞬時輸入，不進存檔。

2D 場景使用 `SceneGameEventTrigger2D`。它提供相同的 Game Event File、Triggering Layers、`Initialize`、`Trigger` 與存檔語意，但自動入口是 `OnTriggerEnter2D(Collider2D)`；composition root 必須將它加入自己的 2D trigger 清單並初始化。

## 重要規則

- `DocumentGuid` 是事件 identity；檔名與 `DisplayName` 只用於作者辨識與診斷。
- Timing 使用 ordinal exact match，沒有萬用字元。
- 空 condition 代表 true。錯誤 condition 會丟 `GameEventException`，不會當成 false。
- 相同 timing 可以有多份文件；priority 相同時按 catalog 輸入順序。
- `RunAsync(file, context)` 直接執行指定文件，不檢查它的 timing，也不必把檔案放進 catalog。
- 存檔前可用 `WaitUntilIdleAsync(token)` 等待 queue 清空。
