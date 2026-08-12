# ValueContainer

## 目的

ValueContainer 定義角色可疊加數值與字串 key/value 的 `IValueContainer` 契約，並提供 Caster／Target Expressions 求值入口。它不提供 concrete container implementation；全域劇情旗標與可保存內容值由 Parameters 負責。

## 快速開始

角色 Stats 實作 `IValueContainer` 後，可交給 `ValueContainerExpressions`：

```csharp
IValueContainer caster = characterStats;
IValueContainer target = enemyStats;

var expressions = new ValueContainerExpressions(caster, target);
ExpressionResult<float> damage =
    expressions.Calculate("Caster.Attack - Target.Defense");
```

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
