# Persistence

## 這個模組怎麼用

Persistence 把遊戲狀態寫進 `slot-{n}.json`，之後再讀回。先判斷資料屬於哪一類：

它不是需要掛在 GameObject 上的自動存檔 component。專案在 Save 按鈕或選單事件中呼叫 `Capture → Write → Save`，在 Load 按鈕或讀檔流程中呼叫 `Load → Read → Restore`。

| 要保存的資料 | 做法 |
|---|---|
| 已在 `ParameterStore` 的分數、旗標、資源量 | 不用另外註冊；Persistence 會保存整份 `ParameterStore`。 |
| 玩家位置、背包、時間服務等 runtime state | 實作 `ISaveParticipant<TSnapshot>`，註冊到 `SaveParticipantRegistry`。 |
| 可由 Parameters 推導的門、機關或 UI 顯示狀態 | 不保存；讀檔後由 Binder／Presenter 重新推導。 |

第一次使用先完成「只存 Parameters」。確認存讀檔可運作後，再加入 participant。

最短心智模型：Parameters 自動整份保存；其他狀態先註冊 participant；最後由 codec 與 slot store 寫檔或讀檔。

## 第一次使用：只存 Parameters

呼叫端 asmdef 引用：

- `KahaGameCore.Modules.Parameters`
- `KahaGameCore.Modules.Persistence`

### 1. 建立一次並持續共用

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

SaveParticipantRegistry participants =
    new SaveParticipantRegistry();
GameSaveDocumentJsonCodec saveCodec =
    new GameSaveDocumentJsonCodec();
GameSaveSlotStore slots = new GameSaveSlotStore(Path.Combine(
    Application.persistentDataPath,
    "Saves"));
```

這四個物件的用途：

| 物件 | 用途 |
|---|---|
| `parameters` | Gameplay 使用的同一份權威 Parameter 值。 |
| `participants` | 保存不在 Parameters 裡的狀態；目前是空的也沒問題。 |
| `saveCodec` | 將 snapshot 與 JSON 互相轉換。 |
| `slots` | 將 JSON 寫入或讀出 `slot-{n}.json`。 |

### 2. Save

```csharp
using UnityEngine.SceneManagement;

const int SaveSlot = 0;

string json = saveCodec.Write(
    sceneKey: SceneManager.GetActiveScene().name,
    parameters: parameters.Capture(),
    participants: participants.Capture());

slots.Save(SaveSlot, json);
```

結果：`{Application.persistentDataPath}/Saves/slot-0.json` 會包含目前 Scene name、整份 Parameter snapshot，以及空的 participant 集合。

### 3. Load

同一個 Scene 已經開啟時：

```csharp
GameSaveSnapshot snapshot = saveCodec.Read(
    slots.Load(SaveSlot),
    participants);

string activeSceneKey = UnityEngine.SceneManagement.SceneManager
    .GetActiveScene()
    .name;
if (!string.Equals(
        snapshot.SceneKey,
        activeSceneKey,
        System.StringComparison.Ordinal))
{
    throw new System.InvalidOperationException(
        $"Save belongs to Scene '{snapshot.SceneKey}'.");
}

parameters.Restore(snapshot.Parameters);
participants.Restore(snapshot.Participants);
```

結果：`PlayerScore` 回到按下 Save 當下的值。

正式接到 UI 前，建議照這個順序驗證：

1. `parameters.Set("PlayerScore", 100)`。
2. 執行 Save。
3. `parameters.Set("PlayerScore", 200)`。
4. 執行 Load。
5. `parameters.GetInt("PlayerScore")` 應為 `100`。

Load 前先用 `slots.Exists(SaveSlot)` 檢查檔案。這個直接 Load 寫法只適用於存檔所屬 Scene 已經開啟的情況；跨 Scene 請看後面的 `GameLoadCoordinator`。

## 加入玩家位置

玩家位置不在 `ParameterStore` 中，所以需要 participant。Participant 只做兩件事：

- `Capture()`：把 runtime object 轉成純資料 snapshot。
- `Restore(snapshot)`：把 snapshot 套回 runtime object。

```csharp
using System;
using KahaGameCore.Persistence;
using UnityEngine;

public sealed class TransformSnapshot
{
    public float X;
    public float Y;
    public float Z;
}

public sealed class TransformSaveParticipant :
    ISaveParticipant<TransformSnapshot>
{
    private readonly Transform target;

    public TransformSaveParticipant(string saveKey, Transform target)
    {
        SaveKey = string.IsNullOrWhiteSpace(saveKey)
            ? throw new ArgumentException("SaveKey is required.", nameof(saveKey))
            : saveKey;
        this.target = target ?? throw new ArgumentNullException(nameof(target));
    }

    public string SaveKey { get; }

    public TransformSnapshot Capture()
    {
        Vector3 position = target.position;
        return new TransformSnapshot
        {
            X = position.x,
            Y = position.y,
            Z = position.z
        };
    }

    public void Restore(TransformSnapshot snapshot)
    {
        target.position = new Vector3(
            snapshot.X,
            snapshot.Y,
            snapshot.Z);
    }
}
```

`participants` 就是前面建立的 `SaveParticipantRegistry`。加入玩家位置時，把前面的空 registry 建立程式：

```csharp
SaveParticipantRegistry participants =
    new SaveParticipantRegistry();
```

改成下面這段完整組裝：

```csharp
SaveParticipantRegistry participants =
    new SaveParticipantRegistry();

TransformSaveParticipant playerTransformParticipant =
    new TransformSaveParticipant(
        saveKey: "Player.Transform",
        target: player);

participants.Register(playerTransformParticipant);
```

其中：

- `participants`：registry，持有這次存讀檔使用的所有 participants。
- `playerTransformParticipant`：知道如何擷取與還原玩家座標。
- `player`：場景中實際玩家物件的 `Transform`，由 Inspector 或場景 composition root 提供。

不要另外建立第二個 registry。Save 與 Load 必須繼續使用這個已註冊玩家位置的同一個 `participants`：

```csharp
string json = saveCodec.Write(
    sceneKey,
    parameters.Capture(),
    participants.Capture());

GameSaveSnapshot snapshot = saveCodec.Read(
    slots.Load(SaveSlot),
    participants);

participants.Restore(snapshot.Participants);
```

呼叫關係是：

```text
專案建立 SaveParticipantRegistry
→ 專案建立 TransformSaveParticipant
→ registry.Register(participant)
→ Save 時 registry.Capture()
→ Load 時 codec.Read(..., registry)
→ registry.Restore(...)
```

`TransformSaveParticipant` 不會建立或尋找 registry；是專案先建立 registry，再將 participant 放進去。

以下是最短寫法，功能相同：

```csharp
participants.Register(new TransformSaveParticipant(
    saveKey: "Player.Transform",
    target: player));
```

Save／Load 程式不需要改。Save 時 registry 自動呼叫 `Capture()`；Load 時自動呼叫 `Restore()`。

`Player.Transform` 是這筆資料在存檔中的 identity。每個 participant 的 `SaveKey` 必須穩定且唯一。Scene 重新載入後，應使用新的玩家 `Transform` 建立 participant，但仍註冊同一個 `Player.Transform` key。

Snapshot 只能保存純資料，不可保存 `Transform`、`GameObject` 或其他 runtime object reference。

## 專案有 Game Events 時怎麼 Save

前面的直接 Save 不會等待正在執行的 Game Event。專案已使用 Game Events 時，改用 `GameSaveCoordinator`，確保存檔前 queue 已清空。

呼叫端再引用：

- `KahaGameCore.Modules.GameEvents`
- `KahaGameCore.Modules.Persistence.GameEventsIntegration`
- `UniTask`

使用 gameplay 已經共用的 `gameEventRunner`、`parameters` 與 `participants`：

```csharp
using KahaGameCore.Persistence.GameEventsIntegration;

GameSaveCoordinator saver = new GameSaveCoordinator(
    gameEventRunner,
    parameters,
    participants,
    saveCodec,
    slots);

await saver.SaveAsync(
    slot: 0,
    sceneKey: UnityEngine.SceneManagement.SceneManager
        .GetActiveScene()
        .name,
    cancellationToken);
```

`SaveAsync` 取代前面的 `saveCodec.Write(...)` 加 `slots.Save(...)`。其餘資料定義、participant 與 Load 方法不變。

## 跨 Scene Load

如果 slot 可能屬於另一個 Scene，使用 `GameLoadCoordinator`。它會依固定順序執行：

```text
讀 slot
→ Restore Parameters
→ Restore 不依賴 Scene 的 participants
→ 載入並組裝存檔指定的 Scene
→ Restore Scene 中的 participants
```

Participants 因此分成兩組：

| Registry | 放什麼 |
|---|---|
| `beforeSceneParticipants` | 不依賴 Scene 物件的服務，例如全域時間狀態。 |
| `sceneParticipants` | Scene 載入後才存在的物件，例如玩家 Transform、寶箱與機關。 |

專案實作 `IGameLoadHost`。它的責任是載入 `sceneKey`、完成該 Scene 的 composition，然後回傳已註冊 Scene 物件的 registry：

```csharp
public sealed class ProjectLoadHost : IGameLoadHost
{
    public async UniTask<SaveParticipantRegistry> LoadSceneAsync(
        string sceneKey,
        ParameterStore restoredParameters,
        CancellationToken token)
    {
        await LoadAndComposeScene(
            sceneKey,
            restoredParameters,
            token);

        SaveParticipantRegistry sceneParticipants =
            new SaveParticipantRegistry();
        sceneParticipants.Register(new TransformSaveParticipant(
            "Player.Transform",
            loadedPlayer));
        return sceneParticipants;
    }
}
```

`LoadAndComposeScene` 與 `loadedPlayer` 由專案的 Scene 載入／組裝程式提供，並不是 Persistence 的場景掃描 API。

建立 coordinator 並 Load：

```csharp
SaveParticipantRegistry beforeSceneParticipants =
    new SaveParticipantRegistry();

GameLoadCoordinator loader = new GameLoadCoordinator(
    parameters,
    beforeSceneParticipants,
    saveCodec,
    slots,
    new ProjectLoadHost());

await loader.LoadAsync(0, cancellationToken);
```

存檔中的每個 participant key 必須恰好註冊在其中一組。重複註冊或沒有註冊都會明確失敗。

## 直接看可操作 Sample

開啟 [GameSaveTest.unity](GameEventsIntegration/Samples/GameSaveTest/GameSaveTest.unity)，依序操作：

1. `CHANGE STATE`
2. `SAVE`
3. `CHANGE STATE` 或 `RELOAD SCENE`
4. `LOAD`

完整接線在 [GameSaveTestController.cs](GameEventsIntegration/Samples/GameSaveTest/GameSaveTestController.cs)。Sample 同時保存 Parameters、`TimeService` participant 與玩家 Transform participant。

## 規則與限制

- `GameSaveSlotStore` 使用 UTF-8 without BOM，檔名為 `slot-{n}.json`。
- `Exists(slot)` 檢查檔案，`Delete(slot)` 刪除檔案。
- `GameLoadCoordinator` 不是 transaction；Scene 載入失敗時，先前已 Restore 的 Parameters 不會 rollback。
- `GameSaveCoordinator` 已由 PlayMode sample 覆蓋；`GameLoadCoordinator` 由自動測試覆蓋，具體 Scene host 由專案提供。
