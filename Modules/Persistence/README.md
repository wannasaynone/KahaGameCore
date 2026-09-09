# Persistence

## 這個模組怎麼用

Persistence 把 `ParameterStore` 的內容寫進 `slot-{n}.json`，之後再讀回。存檔內容只有兩樣東西：Scene name 與整份 Parameter snapshot。

它不是掛在 GameObject 上的自動存檔 component。專案在 Save 按鈕或選單事件中呼叫 `Capture → Write → Save`，在 Load 按鈕或讀檔流程中呼叫 `Load → Read → Restore`。

| 要保存的資料 | 做法 |
|---|---|
| 分數、旗標、資源量、進度、時段 | 定義成 Parameter；Persistence 會整份保存。 |
| 可由 Parameters 推導的門、機關、UI 顯示、角色位置 | 不保存；讀檔後由 Binder／Presenter 重新推導。 |
| 數量不定又帶連續值的東西（未爆投擲物、動態生成物） | 目前不支援，見「已知限制」。 |

最短心智模型：**世界狀態就是 Parameters；存檔就是 Parameter snapshot 加上 Scene name。**

## 建立一次並持續共用

以下物件放在專案啟動或場景組裝程式中，不要每次 Save／Load 都重新建立：

```csharp
using System.IO;
using KahaGameCore.Parameters;
using KahaGameCore.Persistence;
using UnityEngine;

ParameterStore parameters = new ParameterStore(new[]
{
    ParameterDefinition.Int(
        key: "PlayerScore",
        displayName: "玩家分數",
        initialValue: 0,
        minValue: 0,
        maxValue: 9999)
});

GameSaveDocumentJsonCodec saveCodec = new GameSaveDocumentJsonCodec();
GameSaveSlotStore slots = new GameSaveSlotStore(Path.Combine(
    Application.persistentDataPath,
    "Saves"));
```

| 物件 | 用途 |
|---|---|
| `parameters` | Gameplay 使用的同一份權威 Parameter 值。 |
| `saveCodec` | 將 snapshot 與 JSON 互相轉換。 |
| `slots` | 將 JSON 寫入或讀出 `slot-{n}.json`。 |

呼叫端 asmdef 引用 `KahaGameCore.Modules.Parameters` 與 `KahaGameCore.Modules.Persistence`。

## Save

```csharp
using UnityEngine.SceneManagement;

const int SaveSlot = 0;

slots.Save(SaveSlot, saveCodec.Write(
    sceneKey: SceneManager.GetActiveScene().name,
    parameters: parameters.Capture()));
```

結果：`{Application.persistentDataPath}/Saves/slot-0.json` 包含目前 Scene name 與整份 Parameter snapshot。

## Load

同一個 Scene 已經開啟時：

```csharp
GameSaveSnapshot snapshot = saveCodec.Read(slots.Load(SaveSlot));

string activeSceneKey = SceneManager.GetActiveScene().name;
if (!string.Equals(
        snapshot.SceneKey,
        activeSceneKey,
        System.StringComparison.Ordinal))
{
    throw new System.InvalidOperationException(
        $"Save belongs to Scene '{snapshot.SceneKey}'.");
}

parameters.Restore(snapshot.Parameters);
```

Load 前先用 `slots.Exists(SaveSlot)` 檢查檔案。跨 Scene 請看後面的 `GameLoadCoordinator`。

正式接到 UI 前，建議照這個順序驗證：

1. `parameters.Set("PlayerScore", 100)`。
2. 執行 Save。
3. `parameters.Set("PlayerScore", 200)`。
4. 執行 Load。
5. `parameters.GetInt("PlayerScore")` 應為 `100`。

## 不在 Parameters 裡的狀態怎麼辦

先問它是不是「可推導的」。玩家站在哪、門開著沒、HUD 顯示什麼，通常都能從語意狀態算出來：

```csharp
// 存的是語意結果
parameters.Set("Checkpoint", 3);

// 讀檔後推導出表現
player.position = checkpoints[parameters.GetInt("Checkpoint")].position;
```

`ParameterStateBinder` 就是用來做這件事的，讀檔後 `ParameterStore.Changed` 會讓它自動重算。

服務類的狀態同樣改用 Parameter 持有。`TimeService` 是範例：目前時段存在 `CurrentPhase` String Parameter，服務只負責解析與發佈 `TimePhaseChangedEvent`，因此讀檔走的是跟一般階段變更完全相同的路徑。

## 專案有 Game Events 時怎麼 Save

前面的直接 Save 不會等待正在執行的 Game Event。專案已使用 Game Events 時，改用 `GameSaveCoordinator`，確保存檔前 queue 已清空。

呼叫端再引用 `KahaGameCore.Modules.GameEvents`、`KahaGameCore.Modules.Persistence.GameEventsIntegration` 與 `UniTask`。

```csharp
using KahaGameCore.Persistence.GameEventsIntegration;

GameSaveCoordinator saver = new GameSaveCoordinator(
    gameEventRunner,
    parameters,
    saveCodec,
    slots);

await saver.SaveAsync(
    slot: 0,
    sceneKey: SceneManager.GetActiveScene().name,
    cancellationToken);
```

`SaveAsync` 取代前面的 `saveCodec.Write(...)` 加 `slots.Save(...)`。Load 方法不變。

## 跨 Scene Load

如果 slot 可能屬於另一個 Scene，使用 `GameLoadCoordinator`。它會依固定順序執行：

```text
讀 slot
→ Restore Parameters
→ 載入並組裝存檔指定的 Scene
```

順序是重點：Parameters 先還原，Scene 組裝時的 Binder 與 Presenter 才會看到正確的值。

專案實作 `IGameLoadHost`，負責載入 `sceneKey` 並完成該 Scene 的 composition：

```csharp
public sealed class ProjectLoadHost : IGameLoadHost
{
    public async UniTask LoadSceneAsync(
        string sceneKey,
        ParameterStore restoredParameters,
        CancellationToken token)
    {
        await LoadAndComposeScene(sceneKey, restoredParameters, token);
    }
}
```

```csharp
GameLoadCoordinator loader = new GameLoadCoordinator(
    parameters,
    saveCodec,
    slots,
    new ProjectLoadHost());

await loader.LoadAsync(0, cancellationToken);
```

## 直接看可操作 Sample

開啟 [GameSaveTest.unity](GameEventsIntegration/Samples/GameSaveTest/GameSaveTest.unity)，依序操作：

1. `CHANGE STATE`
2. `SAVE`
3. `CHANGE STATE` 或 `RELOAD SCENE`
4. `LOAD`

完整接線在 [GameSaveTestController.cs](GameEventsIntegration/Samples/GameSaveTest/GameSaveTestController.cs)。Sample 保存 `MachineStage` 與 `CurrentPhase` 兩個 Parameter，玩家位置與方塊顯示都是讀檔後推導出來的。

## 規則與限制

- `GameSaveSlotStore` 使用 UTF-8 without BOM，檔名為 `slot-{n}.json`。
- `Exists(slot)` 檢查檔案，`Delete(slot)` 刪除檔案。
- `ParameterStore` 的定義在建構時凍結，`Restore` 遇到未定義的 key 會丟例外。因此**動態生成、沒有預先宣告 key 的物件無法保存**；`ParameterValue` 也只有 Int／Float／Bool／String，沒有集合型別，所以背包、未爆投擲物這類「數量不定」的狀態目前存不了。真的需要時，要在本模組加回一層 participant 或另建集合型 snapshot，不要把 JSON 塞進 String Parameter。
- `GameSaveDocumentJsonCodec` 只接受 `SchemaVersion == 1`，沒有舊存檔遷移機制。
- `GameLoadCoordinator` 不是 transaction；Scene 載入失敗時，先前已 Restore 的 Parameters 不會 rollback。
- `GameSaveCoordinator` 由 PlayMode sample 覆蓋；`GameLoadCoordinator` 由自動測試覆蓋，具體 Scene host 由專案提供。
