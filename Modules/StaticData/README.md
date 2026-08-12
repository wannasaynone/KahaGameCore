# StaticData

## 目的

StaticData 以資料型別為 key，載入並查詢實作 `IGameData` 的表格列。它提供記憶體 registry 與可替換的同步／非同步 loader seam，不決定 JSON 格式或資產來源。

## 快速開始

定義資料列：

```csharp
public sealed class ItemData : IGameData
{
    public int ID { get; set; }
    public string Name { get; set; }
}
```

加入並查詢：

```csharp
var manager = new GameStaticDataManager();
manager.Add<ItemData>(new IGameData[]
{
    new ItemData { ID = 1, Name = "Potion" }
});

ItemData potion = manager.GetGameData<ItemData>(1);
ItemData[] allItems = manager.GetAllGameData<ItemData>();
```

若資料來自檔案，實作 `IGameStaticDataHandler.Load<T>()`／`LoadAsync<T>()`，再呼叫 `manager.Add<ItemData>(handler)`。GameFlowSystem 已提供 Resources 與 `TextAsset[]` 兩種 handler 範例。

## 規則與限制

- `IGameData.ID` 是同型別內的查詢 identity。
- 重複載入預設只記錄 log；要取代既有陣列必須傳 `isForceUpdate: true`。
- 找不到資料會回傳 `default`，不是 exception；caller 必須處理 null／default。
- 本模組不驗證 ID 重複，也不保存 runtime 狀態。

