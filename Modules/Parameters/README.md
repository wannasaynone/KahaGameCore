# Parameters

## 用途

Parameters 是全域、可保存的內容值。它不負責角色 Stats、GameFlow phase／location、Expression parsing、Command execution 或存檔檔案位置。

## 第一次使用：建立兩個 Parameters

呼叫端 asmdef 引用 `KahaGameCore.Modules.Parameters`。若程式碼直接宣告 `ExpressionResult<T>`，也引用 `KahaGameCore.Modules.Expressions`。

Parameter name 在此模組稱為 `Key`。先定義 Key、型別、初始值與範圍，再以 definitions 建立唯一的 gameplay `ParameterStore`：

```csharp
using KahaGameCore.Expressions;
using KahaGameCore.Parameters;

public static class ParameterNames
{
    public const string Supplies = "Supplies";
    public const string OutingUnlocked = "OutingUnlocked";
}

ParameterStore parameters = new ParameterStore(new[]
{
    ParameterDefinition.Int(
        key: ParameterNames.Supplies,
        displayName: "物資",
        initialValue: 10,
        minValue: 0,
        maxValue: 999),
    ParameterDefinition.Bool(
        key: ParameterNames.OutingUnlocked,
        displayName: "外出解鎖",
        initialValue: false)
});

parameters.Add(ParameterNames.Supplies, 10);
parameters.Set(ParameterNames.OutingUnlocked, true);

int supplies = parameters.GetInt(ParameterNames.Supplies);
ExpressionResult<bool> canLeave = parameters.EvaluateCondition(
    "$OutingUnlocked && $Supplies >= 10");
```

預期結果：`supplies` 是 `20`，`canLeave.Value` 是 `true`。`DisplayName` 只供作者與 UI 顯示；程式、條件式與存檔都使用 `Key`。

支援 `Int`、`Float`、`Bool`、`String`。Int／Float 依 definition 的 min／max clamp；`Add` 只接受與 definition 相同的數值型別。Unknown key 與 type mismatch 會明確丟出 `ParameterException` 子型別，不會默認為 `0`。

`Calculate` 與 `EvaluateCondition` 直接以目前的 Parameter 值求值，caller 不需要組裝 Expression context。一般 gameplay caller 使用 typed methods，不需要依 `ParameterType` switch；`TryGetValue` 與 `ParameterValue` 主要供 Editor、Snapshot 與工具使用。

## 使用 Parameter Table JSON

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

## 第一次建立 Parameter Table

從 `KahaGameCore/Parameters/Parameter Table Editor` 開啟大表 EditorWindow。上方編輯表名，下方每列是一個 Parameter，可自行新增或刪除。欄位為 Key、Display Name、Type、Initial／Minimum／Maximum；Bool 與 String 不顯示數值界限。視窗提供 New、Load、Validate、Save 與 Save As，不提供可編輯 JSON 文字區。

Load 透過 `ParameterTableJsonCodec` 把 canonical JSON 填入表格；Save 先把整張表驗證成 `ParameterTable`，再由同一 codec 寫回 JSON。Save 只接受 `Assets/` 下的 `.parameters.json`。每次視窗編輯一張表；要有多張大表就建立多份表資產。

Editor 不建立 ScriptableObject 副本、ScriptedImporter 或自動掃描 registry；磁碟上的 JSON 仍是唯一權威資料。

同一套表格編輯介面也以 `ParameterTableEditorPanel` 提供給其他 EditorWindow 重用；Game Event Editor 會用它直接編輯已選取的 Authoring Parameter Table。獨立視窗與內嵌面板共用驗證、讀取與寫回流程，不各自維護一份格式邏輯。

## Snapshot 與 Persistence

`Capture()` 產生 schema-versioned、獨立複本；`Restore()` 先以所有 definitions 的 InitialValue 建立候選狀態，再覆蓋 snapshot values。Schema、unknown key 或 type 驗證失敗時不修改目前狀態。

需要寫入 slot 檔案時使用 [Persistence](../Persistence/README.md)；Parameters 只負責擷取與還原 snapshot，不決定路徑或檔名。
