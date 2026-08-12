# Parameters

## 目的

Parameters 是全域、可保存的內容值。它不負責角色 Stats、GameFlow phase／location、Expression parsing、Command execution 或存檔檔案位置。

## 快速開始

一般 gameplay caller 使用 typed methods，不需要依 `ParameterType` 選擇模式：

```csharp
ParameterStore parameters = new ParameterStore(definitions);

int supplies = parameters.GetInt("Supplies");
parameters.Add("Supplies", 10);
parameters.Set("OutingUnlocked", true);

ExpressionResult<float> cost = parameters.Calculate("$Supplies * 1.5");
ExpressionResult<bool> canLeave = parameters.EvaluateCondition(
    "$OutingUnlocked && $Supplies >= 10");
```

支援 `Int`、`Float`、`Bool`、`String`。Int／Float 依 definition 的 min／max clamp；`Add` 只接受與 definition 相同的數值型別。Unknown key 與 type mismatch 會明確丟出 `ParameterException` 子型別，不會默認為 `0`。

`Calculate` 與 `EvaluateCondition` 直接以目前的 Parameter 值求值，caller 不需要組裝 Expression context。`TryGetValue` 與 `ParameterValue` 提供給 Editor、Snapshot 等 module 內部或工具用途；一般 caller 不需要 switch `ParameterType`。

## Parameter Table JSON

一份 `.parameters.json` 是一張可容納多列的 Parameter 大表；一個專案可以建立多張表。`TableGuid` 識別整張表，每列的 `Key` 才是 Runtime identity：

```json
{
  "SchemaVersion": 1,
  "TableGuid": "28a2f269-173a-48d2-a8db-9cd6832ee2f3",
  "DisplayName": "Core Gameplay",
  "Parameters": [
    {
      "Key": "Supplies",
      "DisplayName": "物資",
      "Type": "Int",
      "InitialValue": "60",
      "MinValue": "0",
      "MaxValue": "9999"
    },
    {
      "Key": "OutingUnlocked",
      "DisplayName": "外出解鎖",
      "Type": "Bool",
      "InitialValue": "false"
    }
  ]
}
```

`ParameterTableJsonCodec` 使用 invariant culture 與 KahaGameCore 的 JsonFx runtime 讀寫。組裝端可載入多張表，將所有 `Parameters` 展平後建立同一個 `ParameterStore`。表內或跨表重複 `Key` 都會明確失敗，不會覆蓋。檔名與 DisplayName 都不是 Runtime identity。

## Parameter Table Editor

從 `KahaGameCore/Parameters/Parameter Table Editor` 開啟大表 EditorWindow。上方編輯表名，下方每列是一個 Parameter，可自行新增或刪除。欄位為 Key、Display Name、Type、Initial／Minimum／Maximum；Bool 與 String 不顯示數值界限。視窗提供 New、Load、Validate、Save 與 Save As，不提供可編輯 JSON 文字區。

Load 透過 `ParameterTableJsonCodec` 把 canonical JSON 填入表格；Save 先把整張表驗證成 `ParameterTable`，再由同一 codec 寫回 JSON。Save 只接受 `Assets/` 下的 `.parameters.json`。每次視窗編輯一張表；要有多張大表就建立多份表資產。

Editor 不建立 ScriptableObject 副本、ScriptedImporter 或自動掃描 registry；磁碟上的 JSON 仍是唯一權威資料。

## Snapshot

`Capture()` 產生 schema-versioned、獨立複本；`Restore()` 先以所有 definitions 的 InitialValue 建立候選狀態，再覆蓋 snapshot values。Schema、unknown key 或 type 驗證失敗時不修改目前狀態。
