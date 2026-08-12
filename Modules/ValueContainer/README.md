# ValueContainer

## 用途

ValueContainer 定義角色可疊加數值與字串 key/value 的 `IValueContainer` 契約，並提供 Caster／Target Expressions 求值入口。它不提供 concrete container implementation；全域劇情旗標與可保存內容值由 Parameters 負責。

## 第一次使用前先確認

ValueContainer 只有契約，沒有可直接 `new` 的 stats container。第一次使用前，專案必須已經有一個實作 `IValueContainer` 的角色數值類別；如果需求只是全域旗標、資源量或可保存內容值，應改用 [Parameters](../Parameters/README.md)。

## 第一次計算角色數值

角色 Stats 實作 `IValueContainer` 後，可交給 `ValueContainerExpressions`：

```csharp
using KahaGameCore.Expressions;
using KahaGameCore.ValueContainer;

IValueContainer caster = characterStats;
IValueContainer target = enemyStats;

var expressions = new ValueContainerExpressions(caster, target);
ExpressionResult<float> damage =
    expressions.Calculate("Caster.Attack - Target.Defense");
```

預期結果：`Caster.Attack` 讀取 `characterStats.GetTotal("Attack", false)`，`Target.Defense` 讀取 `enemyStats.GetTotal("Defense", false)`，相減結果位於 `damage.Value`。呼叫端必須先檢查 `damage.IsSuccess`。

自己的 asmdef 需要引用：

```json
{
  "references": [
    "KahaGameCore.Modules.ValueContainer",
    "KahaGameCore.Modules.Expressions"
  ]
}
```

## 限制

- `GetTotal(tag, baseOnly)` 對 unknown tag 的具體行為由 implementation 決定；ValueContainer 的求值 implementation 無法用 `TryGet` 驗證 tag。
- 全域劇情旗標與可保存內容值請使用 Parameters。
- 只有需要角色可疊加 Stats 或字串 key/value 的 module 才應依賴 `IValueContainer`；全域語意狀態使用 Parameters。
