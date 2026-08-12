# Game Events

## 目的

Game Events 把 JSON 事件文件連到 Effects runtime。它依 `TriggerTiming` 過濾事件、先拍下符合 condition 的候選集合，再按 priority 由高至低與 catalog 輸入順序執行。所有 direct trigger 與 timing trigger 共用同一條 FIFO queue。

## 快速開始

準備 `.gameevent.json`：

```json
{
  "SchemaVersion": 1,
  "DocumentGuid": "b21ecb37-f6a7-413f-86b7-532d04c31f51",
  "DisplayName": "開門",
  "TriggerTiming": "Interact:Door",
  "Condition": "$DoorOpen == false",
  "Priority": 100,
  "Commands": "SetParameter(DoorOpen,true);"
}
```

由 composition root 建立並觸發：

```csharp
var codec = new GameEventDocumentJsonCodec();
var catalog = new GameEventCatalog(gameEventFiles, codec);
var runner = new GameEventRunner(catalog, effectRuntime, parameters, codec);
var context = new EventContext(cancellationToken);

await runner.TriggerAsync("Interact:Door", context);
```

場景按鈕可掛 `SceneGameEventTrigger`，指定單一 `TextAsset` 後，由 composition root 呼叫 `Initialize(runner, context)`。UnityEvent 綁 `Trigger()`；需要等待結果時直接 `await TriggerAsync()`。

## 重要規則

- `DocumentGuid` 是事件 identity；檔名與 `DisplayName` 只用於作者辨識與診斷。
- Timing 使用 ordinal exact match，沒有萬用字元。
- 空 condition 代表 true。錯誤 condition 會丟 `GameEventException`，不會當成 false。
- 相同 timing 可以有多份文件；priority 相同時按 catalog 輸入順序。
- `RunAsync(file, context)` 直接執行指定文件，不檢查它的 timing，也不必把檔案放進 catalog。
- 存檔前可用 `WaitUntilIdleAsync(token)` 等待 queue 清空。

## Assembly

引用 `KahaGameCore.Modules.GameEvents`，並同時引用實際使用的 Effects、Parameters 與 UniTask assemblies。
