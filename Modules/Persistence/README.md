# Persistence

## 目的

Persistence 保存 `ParameterSnapshot`、場景 key，以及由 composition root 明確註冊的 typed participants。它不掃描場景、不尋找 static singleton，也不保存可由 Parameters 推導的 GameObject active 狀態。

## 快速開始：Participant

```csharp
using KahaGameCore.Persistence;

public sealed class PlayerStateParticipant : ISaveParticipant<PlayerStateSnapshot>
{
    public string SaveKey => "Player";

    public PlayerStateSnapshot Capture()
    {
        return new PlayerStateSnapshot { X = player.position.x };
    }

    public void Restore(PlayerStateSnapshot snapshot)
    {
        player.position = new Vector3(snapshot.X, 0f, 0f);
    }
}

var participants = new SaveParticipantRegistry();
participants.Register(new PlayerStateParticipant());
```

`SaveKey` 必須穩定且唯一。Snapshot 必須是脫離 runtime mutable state 的資料；`Restore` 套用權威狀態，不重播歷史 gameplay action。

## 快速開始：Save

Game Events integration 會等 queue idle 才 capture：

```csharp
var codec = new GameSaveDocumentJsonCodec();
var slots = new GameSaveSlotStore(Application.persistentDataPath);
var saver = new GameSaveCoordinator(
    gameEventRunner,
    parameters,
    participants,
    codec,
    slots);

await saver.SaveAsync(0, sceneKey, cancellationToken);
```

Slot 檔名是 `slot-{n}.json`，使用 UTF-8 without BOM。`GameSaveSlotStore.Exists` 與 `Delete` 可管理 slot。

## 快速開始：Load core

```csharp
public sealed class ProjectLoadHost : IGameLoadHost
{
    public async UniTask<SaveParticipantRegistry> LoadSceneAsync(
        string sceneKey,
        ParameterStore restoredParameters,
        CancellationToken token)
    {
        await LoadAndComposeScene(sceneKey, restoredParameters, token);
        return BuildSceneParticipantRegistry();
    }
}

var loader = new GameLoadCoordinator(
    parameters,
    beforeSceneParticipants,
    codec,
    slots,
    new ProjectLoadHost());

await loader.LoadAsync(0, cancellationToken);
```

固定順序是 Parameters → 場景載入前 participants → load／compose Scene → 場景 participants。Host 必須在回傳前完成場景 Binder 初始化並明確建立 registry。

## 狀態與限制

- Save 已通過手動 PlayMode 驗收。
- Load core 有自動測試，但尚未人工驗收；KahaGameCore 不提供 ProjectTentacle 專用 Scene host adapter。
- Load 不是 transaction。場景載入失敗時，先前已還原的 Parameters 不會 rollback。
- 同一 `SaveKey` 不可同時註冊於換場景前與場景 registry，也不可漏註冊。

