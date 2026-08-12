# StaticData

## 用途

StaticData 以資料型別為 key，載入並查詢實作 `IGameData` 的表格列。它提供記憶體 registry 與可替換的同步／非同步 loader seam，不決定 JSON 格式或資產來源。

## 第一次使用：加入一列並查回

呼叫端 asmdef 引用 `KahaGameCore.Modules.StaticData`。資料列必須實作 `IGameData`，其中 `ID` 是同型別資料的查詢名稱：

定義資料列：

```csharp
using KahaGameCore.StaticData;

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

預期結果：`potion.Name` 是 `"Potion"`，`allItems` 有一列。專案應建立並共用同一個 `GameStaticDataManager`，不要讓每個 caller 各自載入一份表。

## 從檔案載入

實作 `IGameStaticDataHandler.Load<T>()`／`LoadAsync<T>()`，再呼叫 `manager.Add<ItemData>(handler)`。GameFlowSystem 的 DefaultImplements 提供 Resources 與 `TextAsset[]` 兩種 handler；使用它們時需再引用 `KahaGameCore.Modules.GameFlowSystem.DefaultImplements`。

## 規則與限制

- `IGameData.ID` 是同型別內的查詢 identity。
- 重複載入預設只記錄 log；要覆寫已載入的陣列必須傳 `isForceUpdate: true`。
- 找不到資料會回傳 `default`，不是 exception；caller 必須處理 null／default。
- 本模組不驗證 ID 重複，也不保存 runtime 狀態。
