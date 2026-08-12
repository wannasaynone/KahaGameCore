# Foundation

## 用途

Foundation 包含 `Common` 與 `Messaging` 兩個 assembly。它們是低階 Unity utilities，不承載 Parameters、Game Events 或流程規則。

## 第一次使用：先選需要的 assembly

| 需求 | 引用 assembly | 入口 |
|---|---|---|
| 不互相持有參考的物件要同步傳遞型別化訊息 | `KahaGameCore.Foundation.Messaging` | `MessageBus` |
| Timer、coroutine runner、main-thread queue 或 Inspector attributes | `KahaGameCore.Foundation.Common` | 對應 utility |

兩者可以分開引用。只需要訊息時不必引用 Common。

## 第一次使用 Messaging

`MessageBus` 提供依 message concrete type 分流的同步 publish／subscribe。每個 handler 以原 delegate 保存，因此可以精確退訂。

```csharp
using KahaGameCore.Foundation.Messaging;

public sealed class HealthChanged : MessageBase
{
    public int Value;
}

private void OnEnable()
{
    MessageBus.Subscribe<HealthChanged>(OnHealthChanged);
}

private void OnDisable()
{
    MessageBus.Unsubscribe<HealthChanged>(OnHealthChanged);
}

MessageBus.Publish(new HealthChanged { Value = 10 });
```

預期結果：`Publish` 當下同步呼叫 `OnHealthChanged`。Component disable 後已退訂，不會再收到訊息。

Publish 使用目前訂閱者的 snapshot。`ForceClearAll()` 只適合測試或明確的 application teardown，不要用它代替正常退訂。

## 第一次使用 Common

Common 提供小型 Unity utilities：`TimerManager`、`GeneralCoroutineRunner`、`UnityThread`、`GameUtility`，以及 Inspector attributes。

```csharp
using KahaGameCore.Foundation.Common;

long timerId = TimerManager.Schedule(
    3f,
    onTimeEnded: () => UnityEngine.Debug.Log("Timer ended"),
    onTimeUpdated: remaining =>
        UnityEngine.Debug.Log(remaining.ToString("0.0")));

// 不再需要時
TimerManager.Cancel(timerId);

// 從背景 thread 排入 Unity main thread；場景必須已有 UnityThread component。
UnityThread.Do(() => UnityEngine.Debug.Log("Main thread action"));
```

預期結果：Timer 每幀回報剩餘秒數並在約 3 秒後呼叫 `OpenDoor`；`UnityThread.Do` 的 action 會由場景內的 `UnityThread` component 在主執行緒執行。

## 限制

- MessageBus 是 static process-wide state；subscriber 必須對稱退訂。
- `TimerManager` 使用 `Time.deltaTime`，不是 realtime timer。
- `UnityThread.Do` 只有在場景已初始化 `UnityThread` component 時才會被 Update 消耗。
- Common 只收納 Unity／UniTask 未提供、且由多個 module 共用的 utilities；不要為單一 caller 擴張全域 helper。
