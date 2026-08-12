# Serialization

## 目的

Serialization 是專案既有 JsonFx runtime 的最薄 adapter。`GameStaticDataSerializer` 實作 `IJsonWriter`，`GameStaticDataDeserializer` 實作 `IJsonReader`，讓 caller 不必直接引用 JsonFx namespace。

## 快速開始

```csharp
using KahaGameCore.Serialization;

IJsonWriter writer = new GameStaticDataSerializer();
string json = writer.Write(data);

IJsonReader reader = new GameStaticDataDeserializer();
MyData restored = reader.Read<MyData>(json);
```

引用 assembly：`KahaGameCore.Modules.Serialization`。

## 限制

- 本模組不提供 schema version、migration、檔案路徑或 Unity asset loading。
- Parameters、Game Events 與 Persistence 各有自己的驗證 codec；不要用這個通用 adapter 取代它們。
- JsonFx 是專案現有依賴，不要再為相同用途新增第二套 JSON runtime。

