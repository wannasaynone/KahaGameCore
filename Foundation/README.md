# Foundation

## 目的

Foundation 包含 `Common` 與 `Messaging` 兩個 assembly。它們是低階 Unity utilities，不承載 Parameters、Game Events 或流程規則。

## 快速開始

- 需要型別化的 process-wide 訊息時，引用 `KahaGameCore.Foundation.Messaging`，依下方範例對稱 Subscribe／Unsubscribe。
- 需要 timer、coroutine runner 或 main-thread queue 時，引用 `KahaGameCore.Foundation.Common`，依下方 Common 範例使用。

## Messaging 目的

`MessageBus` 提供依 message concrete type 分流的同步 publish／subscribe。每個 handler 以原 delegate 保存，因此可以精確退訂。

## Messaging 快速開始

```csharp
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

Publish 是同步呼叫目前訂閱者的 snapshot。`ForceClearAll()` 只適合測試或明確的 application teardown，不要用它代替正常退訂。

## Common 目的

Common 提供小型 Unity utilities：`TimerManager`、`GeneralCoroutineRunner`、`UnityThread`、`GameUtility`，以及 Inspector attributes。

## Common 快速開始

```csharp
long timerId = TimerManager.Schedule(
    3f,
    onTimeEnded: OpenDoor,
    onTimeUpdated: remaining => timerLabel.text = remaining.ToString("0.0"));

// 不再需要時
TimerManager.Cancel(timerId);

// 從背景 thread 排入 Unity main thread；場景必須已有 UnityThread component。
UnityThread.Do(() => statusText.text = "Done");
```

## 注意事項

- MessageBus 是 static process-wide state；subscriber 必須對稱退訂。
- `TimerManager` 使用 `Time.deltaTime`，不是 realtime timer。
- `UnityThread.Do` 只有在場景已初始化 `UnityThread` component 時才會被 Update 消耗。
- Common 只收納 Unity／UniTask 未提供、且由多個 module 共用的 utilities；不要為單一 caller 擴張全域 helper。
