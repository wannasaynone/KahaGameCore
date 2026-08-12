# ValueContainer

## 目的

ValueContainer 只定義舊式可疊加數值與字串 key/value 的 `IValueContainer` 契約。它不提供 concrete implementation，也不是新版全域 Parameters 的替代品。

## 快速開始

角色 Stats 實作 `IValueContainer` 後，可交給 Expressions adapter：

```csharp
IValueContainer caster = characterStats;
IValueContainer target = enemyStats;

var context = new ValueContainerExpressionContext(caster, target);
ExpressionResult<float> damage = expressions.Calculate(
    "Caster.Attack - Target.Defense",
    context);
```

自己的 asmdef 需要引用：

```json
{
  "references": [
    "KahaGameCore.Modules.ValueContainer",
    "KahaGameCore.Modules.Expressions",
    "KahaGameCore.Modules.Expressions.ValueContainer"
  ]
}
```

## 限制

- `GetTotal(tag, baseOnly)` 對 unknown tag 的具體行為由 implementation 決定；現有 Expressions adapter 無法用 `TryGet` 驗證 tag。
- 全域劇情旗標與可保存內容值請使用 Parameters。
- 新程式若不需要相容既有角色 Stats，不應主動擴充這個 legacy interface。

