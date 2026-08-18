# Parameters

## 用途

Parameters 是全域、可保存的內容值。它不負責角色 Stats、GameFlow phase／location、Expression parsing、Command execution 或存檔檔案位置。

## 第一次使用：建立兩個 Parameters

純資料與 gameplay 邏輯的 asmdef 引用 `KahaGameCore.Modules.Parameters`。場景中的 composition root 若繼承 `ParameterRuntimeSource`，還要引用 `KahaGameCore.Modules.Parameters.Unity`；若程式碼直接宣告 `ExpressionResult<T>`，也引用 `KahaGameCore.Modules.Expressions`。

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

若 Parameter 是由 Parameter Table Editor 建立的 `.parameters.json` 文字資產，不要在程式中重複宣告 definitions。只使用 Parameters + Effects + Game Events 時，直接把 Parameter Table 加入 `GameEventCatalogAsset`，並在場景掛上 `DefaultSimpleGameLauncher`；它會建立唯一的 live `ParameterStore`、註冊共用 Parameter Commands，並初始化 child Game Event triggers。

```csharp
using KahaGameCore.Parameters;
using UnityEngine;

public sealed class ParametersOnlyLauncher : ParameterRuntimeSource
{
    [SerializeField] private TextAsset paramTable;

    private void Awake()
    {
        ParameterTableJsonCodec parameterTableJson = new ParameterTableJsonCodec();
        ParameterTable table = parameterTableJson.Read(paramTable.text);
        ParameterStore parameterStore = new ParameterStore(table.Definitions);
        Initialize(parameterStore);
    }
}
```

上面是完全不使用 Game Events 時的最小手動版本。一般專案把 `DefaultSimpleGameLauncher` 掛到場景 GameObject、指定 Game Event Catalog 即可；進入 Play Mode 後它本身也是 Runtime Parameter Monitor 可讀取的 `ParameterRuntimeSource`。若專案載入多張表，composition root 應先展平 definitions 建立同一份 Store，不要為每張表各建 Store。

`KahaGameCore.Modules.Parameters.EffectsIntegration` 提供 `AddParameter`、`SetParameter`、`ParameterEffectCommandRegistrar.RegisterAll(...)` 與對應 descriptor provider。這是 Parameters 與 Effects 的明確整合模組；專案不需要再複製自己的 `AddParameter`。

## 查看 Runtime Parameter

Play Mode 時可開啟 `KahaGameCore/Parameters/Runtime Parameter Monitor`。視窗會尋找已載入場景中的 `ParameterRuntimeSource` 衍生元件，以唯讀表格持續顯示每個 Parameter 的 Key、Display Name、Type 與目前值，並可搜尋；它不依賴特定 Launcher，也不提供修改功能。

程式工具可呼叫 `ParameterStore.CaptureCurrentValues()` 取得同一份 `ParameterRuntimeValue` 唯讀快照。若現有 composition root 無法直接繼承 `ParameterRuntimeSource`，才建立可掛載的空白 adapter，並在建立唯一的 live Store 後初始化：

```csharp
public sealed class MyParameterRuntimeSource : ParameterRuntimeSource
{
}

// composition root 建立 ParameterStore 後：
runtimeSource.Initialize(parameters);
```

把 `MyParameterRuntimeSource` 掛到已載入場景中的 GameObject，並讓 composition root 持有它的 serialized reference。`IsInitialized` 代表是否已收到 Store；`CaptureCurrentValues()` 在尚未初始化時回傳空集合。`DefaultGameLauncher` 只是這個 abstract source 的一個現成 adapter，不是 Editor 的必要依賴。

宣告或引用 `ParameterRuntimeSource` 的 asmdef 必須加入 `KahaGameCore.Modules.Parameters.Unity`；純資料與 gameplay 邏輯仍只引用 `KahaGameCore.Modules.Parameters`，因此核心 module 保持零 UnityEngine 依賴。

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
