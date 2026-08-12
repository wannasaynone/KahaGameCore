# Serialization

## 用途

Serialization 是 JsonFx runtime 的最薄 adapter。`GameStaticDataSerializer` 實作 `IJsonWriter`，`GameStaticDataDeserializer` 實作 `IJsonReader`，讓 caller 不必直接引用 JsonFx namespace。

## 第一次使用：物件轉成 JSON 再讀回

呼叫端 asmdef 引用 `KahaGameCore.Modules.Serialization`。`data` 是專案自己的可序列化資料物件：

```csharp
using KahaGameCore.Serialization;

public sealed class MyData
{
    public int Score { get; set; }
}

MyData data = new MyData { Score = 100 };

IJsonWriter writer = new GameStaticDataSerializer();
string json = writer.Write(data);

IJsonReader reader = new GameStaticDataDeserializer();
MyData restored = reader.Read<MyData>(json);
```

預期結果：`json` 是 JsonFx 產生的字串，`restored.Score` 是 `100`。這個 adapter 不會替資料驗證 schema。

## 限制

- 本模組不提供 schema version 驗證或轉換、檔案路徑或 Unity asset loading。
- Parameters、Game Events 與 Persistence 各有自己的驗證 codec；不要用這個通用 adapter 取代它們。
- JsonFx 是 KahaGameCore 的 JSON runtime；相同用途不應再引入第二套 runtime。
