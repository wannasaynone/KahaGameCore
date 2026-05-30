# ActorSystem

一個輕量級、基於 **Channel 優先權** 的角色行為管理框架，適用於 Unity 專案。  
它讓多個 Action（行為）可以同時啟用，並透過 Channel 競爭機制自動決定每個面向（動畫、移動…）由哪個 Action 掌控，避免行為之間的衝突。

---

## 目錄

- [核心概念](#核心概念)
- [架構總覽](#架構總覽)
- [類別說明](#類別說明)
  - [IActor](#iactor)
  - [AActorAction](#aactoraction)
  - [ActorController](#actorcontroller)
  - [ChannelBinding](#channelbinding)
- [Channel 競爭規則](#channel-競爭規則)
- [Quick Start](#quick-start)
  - [Step 1 — 定義 Channel](#step-1--定義-channel)
  - [Step 2 — 實作 IActor](#step-2--實作-iactor)
  - [Step 3 — 建立 Action](#step-3--建立-action)
  - [Step 4 — 組裝 Controller 並驅動](#step-4--組裝-controller-並驅動)
  - [Step 5 — 場景設定](#step-5--場景設定)
- [進階範例：攻擊行為（有限時間 Action）](#進階範例攻擊行為有限時間-action)
- [設計要點與最佳實踐](#設計要點與最佳實踐)

---

## 核心概念

| 概念 | 說明 |
|------|------|
| **Actor** | 被控制的角色實體，提供 `Transform` 等基本資訊。 |
| **Action** | 一個可啟用 / 停用的行為單元（如移動、攻擊、受傷）。 |
| **Channel** | 角色的 **輸出面向**，例如「動畫」「X 軸移動」「Y 軸移動」。每個 Channel 在一個 Tick 中只會有一個 Action 的 Handler 被執行。 |
| **Priority** | 每個 Action 對每個 Channel 宣告的優先權數值，數字越大優先權越高。 |
| **Controller** | 負責管理所有啟用中的 Action，每次 Tick 解算 Channel 競爭並執行勝出的 Handler。 |

**一句話總結：** 多個 Action 可以同時 Active，但每個 Channel 每個 Tick 只有優先權最高的那個 Action 的 Handler 會被呼叫。

---

## 架構總覽

```
┌─────────────────────────────────────────────────┐
│                ActorController                  │
│                                                 │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐      │
│  │ Action A │  │ Action B │  │ Action C │ ...  │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘      │
│       │              │              │            │
│       ▼              ▼              ▼            │
│  ┌─────────────────────────────────────────┐    │
│  │         Channel 競爭解算                 │    │
│  │  Animation  → 最高 Priority 的 Handler  │    │
│  │  MovementX  → 最高 Priority 的 Handler  │    │
│  │  MovementY  → 最高 Priority 的 Handler  │    │
│  └─────────────────────────────────────────┘    │
│                      │                           │
│                      ▼                           │
│                   IActor                         │
└─────────────────────────────────────────────────┘
```

---

## 類別說明

### IActor

```csharp
public interface IActor
{
    Transform Transform { get; }
}
```

代表被控制的角色。實作此介面即可被 ActorSystem 管理。  
你可以在實作類別中加入遊戲特有的方法（如 `PlayAnimation`）。

---

### AActorAction

```csharp
public abstract class AActorAction : MonoBehaviour
```

所有行為的基底類別，繼承自 `MonoBehaviour`，因此可以直接掛載在 GameObject 上並使用 Inspector 設定參數。

#### 核心成員

| 成員 | 類型 | 說明 |
|------|------|------|
| `IsActive` | `bool` | 是否正在啟用中（由 Controller 管理）。 |
| `ActivationOrder` | `int` | 啟用順序（由 Controller 自動遞增）。 |
| `Completed` | `event Action<AActorAction>` | 當 Action 完成時觸發，Controller 會自動停用該 Action。 |

#### 生命週期方法

| 方法 | 呼叫時機 | 說明 |
|------|----------|------|
| `OnBind()` | 首次 Init 時（只呼叫一次） | **必須覆寫**。在此使用 `BindChannel()` 宣告要競爭的 Channel。 |
| `OnStart(IActor)` | 被 Controller 啟用時 | 可選覆寫。初始化 Action 的執行狀態。 |
| `OnTick()` | 每次 `Controller.Tick()` 時 | 可選覆寫。執行每幀的邏輯（如計時器）。 |
| `OnEnd(IActor)` | 被 Controller 停用時 | 可選覆寫。清理或還原狀態。 |

#### 關鍵方法

```csharp
// 在 OnBind() 中呼叫，宣告此 Action 要競爭哪個 Channel
protected void BindChannel<TChannel>(TChannel channel, int priority, Action<IActor> handler)

// 在 OnTick() 中呼叫，通知 Controller 此 Action 已完成（會自動觸發停用）
protected void Complete()
```

---

### ActorController

```csharp
public class ActorController
```

不繼承 MonoBehaviour 的純 C# 類別，負責管理 Action 的生命週期與 Channel 競爭解算。

#### 方法

| 方法 | 說明 |
|------|------|
| `Initialize<TChannel>(IActor actor)` | 初始化 Controller，傳入 Actor 與 Channel 枚舉型別。會根據枚舉值建立所有 Channel Slot。 |
| `SetActionActive(AActorAction action)` | 啟用一個 Action。若已啟用則忽略。 |
| `SetActionInactive(AActorAction action)` | 停用一個 Action。若未啟用則忽略。 |
| `Tick()` | 每幀呼叫。依序：1) 對所有啟用中的 Action 呼叫 `OnTick()`　2) 解算 Channel 競爭　3) 執行各 Channel 勝出的 Handler。 |

---

### ChannelBinding

```csharp
public class ChannelBinding
{
    public int ChannelId { get; }
    public int Priority { get; }
    public Action<IActor> Handler { get; }
}
```

儲存一個 Action 對一個 Channel 的綁定資訊（Channel ID、優先權、處理函式）。  
由 `AActorAction.BindChannel()` 內部自動建立，通常不需手動操作。

---

## Channel 競爭規則

每次 `Tick()` 時，Controller 會對每個 Channel 進行以下解算：

1. 遍歷所有 **啟用中** 的 Action 的所有 Binding。
2. 對同一個 Channel，比較 **Priority**：**數字越大越優先**。
3. 若 Priority 相同，則比較 **ActivationOrder**：**越晚啟用的越優先**（後來居上）。
4. 每個 Channel 最終只有一個 Handler 會被執行。
5. 沒有任何 Action 綁定的 Channel 不執行任何操作。

> **範例：** MoveAction 綁定 `Animation` Priority=1，AttackAction 綁定 `Animation` Priority=2。  
> 當兩者同時啟用時，AttackAction 的動畫 Handler 會勝出，角色播放攻擊動畫而非行走動畫。

---

## Quick Start

以下以一個簡單的「角色移動」為例，示範如何從零開始使用 ActorSystem。

### Step 1 — 定義 Channel

建立一個枚舉，列出你的角色會用到的所有輸出面向：

```csharp
namespace Game.GamePlay
{
    public enum ActorChannel
    {
        Animation,   // 動畫控制
        MovementX,   // X 軸位移
        MovementY,   // Y 軸位移
    }
}
```

> 💡 Channel 的定義完全由你的遊戲需求決定，可以自由增減。

---

### Step 2 — 實作 IActor

建立角色類別，實作 `IActor` 介面：

```csharp
using KahaGameCore.ActorSystem;
using UnityEngine;

namespace Game.GamePlay
{
    public class GameActor : MonoBehaviour, IActor
    {
        public Transform Transform => transform;

        private string _currentAnimation = "";

        public void PlayAnimation(string animationName)
        {
            if (_currentAnimation == animationName) return;
            _currentAnimation = animationName;
            Debug.Log($"[GameActor] Play: {animationName}");
        }
    }
}
```

---

### Step 3 — 建立 Action

繼承 `AActorAction`，在 `OnBind()` 中宣告要競爭的 Channel：

```csharp
using KahaGameCore.ActorSystem;
using UnityEngine;

namespace Game.GamePlay.Actions
{
    public class MoveAction : AActorAction
    {
        [SerializeField] private float speed = 5f;
        private float _direction;

        public void SetDirection(float direction) => _direction = direction;

        // 只會被呼叫一次，在此宣告要競爭的 Channel
        protected override void OnBind()
        {
            BindChannel(ActorChannel.Animation,  1, OnAnimation);
            BindChannel(ActorChannel.MovementX,  1, OnMovementX);
        }

        // 當此 Action 贏得 Animation Channel 時執行
        private void OnAnimation(IActor actor)
        {
            if (actor is GameActor gameActor)
                gameActor.PlayAnimation("Walk");
        }

        // 當此 Action 贏得 MovementX Channel 時執行
        private void OnMovementX(IActor actor)
        {
            var pos = actor.Transform.position;
            pos.x += _direction * speed * Time.deltaTime;
            actor.Transform.position = pos;
        }

        // 停用時還原為 Idle 動畫
        public override void OnEnd(IActor actor)
        {
            if (actor is GameActor gameActor)
                gameActor.PlayAnimation("Idle");
        }
    }
}
```

---

### Step 4 — 組裝 Controller 並驅動

建立一個管理者腳本，初始化 `ActorController` 並在 `Update()` 中驅動：

```csharp
using Game.GamePlay.Actions;
using KahaGameCore.ActorSystem;
using UnityEngine;

namespace Game.GamePlay
{
    public class GameInitializer : MonoBehaviour
    {
        [SerializeField] private GameActor actor;
        [SerializeField] private MoveAction moveAction;

        private ActorController _controller;

        private void Start()
        {
            // 建立 Controller 並用你的 Channel 枚舉初始化
            _controller = new ActorController();
            _controller.Initialize<ActorChannel>(actor);
        }

        private void Update()
        {
            // 根據輸入啟用 / 停用 Action
            float h = Input.GetAxisRaw("Horizontal");
            if (h != 0f)
            {
                moveAction.SetDirection(h);
                _controller.SetActionActive(moveAction);
            }
            else
            {
                _controller.SetActionInactive(moveAction);
            }

            // 每幀驅動 Controller（解算 Channel 競爭 + 執行 Handler）
            _controller.Tick();
        }
    }
}
```

---

### Step 5 — 場景設定

1. 建立一個 GameObject，掛上 `GameActor` 元件。
2. 在同一個（或子物件的）GameObject 上掛上 `MoveAction` 元件。
3. 建立另一個 GameObject，掛上 `GameInitializer` 元件。
4. 在 Inspector 中將 `GameActor` 和 `MoveAction` 拖曳指定到 `GameInitializer` 的欄位。
5. 執行遊戲，按左右鍵即可看到角色移動。

---

## 進階範例：攻擊行為（有限時間 Action）

攻擊是一個有持續時間的 Action，結束後會自動停用。注意它用更高的 Priority 搶佔了 Animation 和 Movement Channel，讓角色在攻擊時無法移動：

```csharp
using KahaGameCore.ActorSystem;
using UnityEngine;

namespace Game.GamePlay.Actions
{
    public class AttackAction : AActorAction
    {
        [SerializeField] private float duration = 0.5f;
        private float _timer;
        private GameActor _currentActor;

        protected override void OnBind()
        {
            // Priority=2，比 MoveAction 的 Priority=1 高
            BindChannel(ActorChannel.Animation,  2, OnAnimation);
            BindChannel(ActorChannel.MovementX,  2, delegate { });  // 空 Handler → 凍結 X 移動
            BindChannel(ActorChannel.MovementY,  2, delegate { });  // 空 Handler → 凍結 Y 移動
        }

        public override void OnStart(IActor actor)
        {
            _timer = duration;
        }

        public override void OnTick()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _currentActor.PlayAnimation("Idle");
                Complete();  // 呼叫 Complete() 通知 Controller 此 Action 完成
            }
        }

        private void OnAnimation(IActor actor)
        {
            if (actor is GameActor gameActor)
            {
                _currentActor = gameActor;
                gameActor.PlayAnimation("Attack");
            }
        }
    }
}
```

**運作流程：**
1. 玩家按下攻擊鍵 → `Controller.SetActionActive(attackAction)`
2. AttackAction 以 Priority=2 搶佔 Animation、MovementX、MovementY Channel
3. 即使 MoveAction 仍然啟用，移動 Handler 不會被執行（被 AttackAction 的空 Handler 蓋過）
4. 攻擊持續 0.5 秒後呼叫 `Complete()`，Controller 自動停用 AttackAction
5. MoveAction 重新取回 Channel 控制權，角色恢復移動

---

## 設計要點與最佳實踐

| 要點 | 說明 |
|------|------|
| **Channel 粒度** | Channel 不要太粗（如只有一個 `Body`），也不要太細。依據「哪些面向需要被獨立搶佔」來劃分。 |
| **Priority 規劃** | 建議用常數或 enum 統一管理 Priority 等級（如 Normal=1, High=2, Override=10），避免散落的魔術數字。 |
| **空 Handler 搶佔** | 想凍結某個面向時，綁定該 Channel 並給一個空的 `delegate { }` 作為 Handler。 |
| **OnTick vs Handler** | `OnTick()` 每幀必定執行（不受 Channel 競爭影響），適合計時器等邏輯；Handler 只有贏得 Channel 時才執行，適合實際的輸出操作。 |
| **Complete() 自動停用** | 呼叫 `Complete()` 會透過事件觸發 `SetActionInactive()`，不需手動停用。 |
| **Action 是 MonoBehaviour** | 可以使用 `[SerializeField]` 在 Inspector 設定參數，也可以 GetComponent 取得。 |
| **Controller 是純 C#** | `ActorController` 不繼承 MonoBehaviour，需要由外部在 `Update()` 中呼叫 `Tick()`。 |
