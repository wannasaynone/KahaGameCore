# Expressions

## 用途

Expressions 負責解析與計算數值公式、Bool 條件。核心 assembly `KahaGameCore.Modules.Expressions` 不認得 Parameters、Caster、Target 或 `IValueContainer`；公式中的符號由呼叫端提供的 context 解讀。

## 第一次使用：先選入口

| 公式中的資料 | 使用入口 | 引用 assembly |
|---|---|---|
| `$Supplies`、`$DoorOpen` 等全域 Parameter | `ParameterStore.Calculate`／`EvaluateCondition` | `KahaGameCore.Modules.Parameters`、`KahaGameCore.Modules.Expressions` |
| `Caster.Attack`、`Target.Defense` 等角色數值 | `ValueContainerExpressions` | `KahaGameCore.Modules.ValueContainer`、`KahaGameCore.Modules.Expressions` |
| 專案自訂符號 | `Expressions` 搭配自己的 `IExpressionContext` | `KahaGameCore.Modules.Expressions` |

如果資料已在 `ParameterStore`，不需要自行建立 `Expressions` 或 expression context。

## 第一次計算 Parameter

Parameter name 在 Parameters module 中稱為 `Key`。以下 `Supplies` 與 `DoorOpen` 就是公式會查找的名稱：

```csharp
using KahaGameCore.Expressions;
using KahaGameCore.Parameters;

ParameterStore parameters = new ParameterStore(new[]
{
    ParameterDefinition.Int(
        key: "Supplies",
        displayName: "物資",
        initialValue: 10,
        minValue: 0,
        maxValue: 999),
    ParameterDefinition.Bool(
        key: "DoorOpen",
        displayName: "門已開啟",
        initialValue: false)
});

ExpressionResult<float> amount =
    parameters.Calculate("$Supplies * 1.5");
ExpressionResult<bool> canEnter =
    parameters.EvaluateCondition("$Supplies >= 10 && !$DoorOpen");

if (!amount.IsSuccess) UnityEngine.Debug.LogError(amount.Error.Message);
if (!canEnter.IsSuccess) UnityEngine.Debug.LogError(canEnter.Error.Message);
```

預期結果：`amount.Value` 是 `15`，`canEnter.Value` 是 `true`。`$` 後面必須是 `ParameterDefinition.Key`，不是 `DisplayName`。

Int、Float 會映射成 Number，Bool 會映射成 Boolean。String Parameter 與不存在的 Key 會得到 structured `UnknownSymbol` failure。

## 第一次計算 Caster／Target

開始前：專案必須已經有實作 `IValueContainer` 的角色數值物件。ValueContainer module 不提供 concrete stats container。

```csharp
IValueContainer caster = characterStats;
IValueContainer target = enemyStats;

ValueContainerExpressions expressions =
    new ValueContainerExpressions(caster, target);
ExpressionResult<float> damage =
    expressions.Calculate("Caster.Attack - Target.Defense");
```

預期結果：`Caster.Attack` 呼叫 `caster.GetTotal("Attack", false)`，`Target.Defense` 呼叫 `target.GetTotal("Defense", false)`，相減結果位於 `damage.Value`。

只讀 base value 時傳入 `baseOnly: true`：

```csharp
var expressions = new ValueContainerExpressions(
    caster,
    target,
    baseOnly: true);
```

`IValueContainer` 沒有 `Contains`／`TryGet`。因此 unknown tag 的結果取決於 container 自己的 `GetTotal`；unknown prefix、缺少 Caster／Target 或不完整符號則回傳 structured unknown-symbol failure。

## 自訂符號來源

只有不屬於 Parameters 或 ValueContainer 的符號才直接實作 `IExpressionContext`：

```csharp
public sealed class WeatherExpressionContext : IExpressionContext
{
    public bool TryResolve(string symbol, out ExpressionValue value)
    {
        if (symbol == "Temperature")
        {
            value = ExpressionValue.FromNumber(28f);
            return true;
        }

        value = default;
        return false;
    }
}

var expressions = new Expressions();
ExpressionResult<bool> isHot = expressions.EvaluateCondition(
    "Temperature >= 25",
    new WeatherExpressionContext());
```

## 語法與失敗處理

- 計算式支援一般數值運算與 `Random(min,max)`。
- 條件式支援比較、括號、`!`、`&&`、`||`，並禁止 `Random`。
- 所有入口都回傳 `ExpressionResult<T>`。先檢查 `IsSuccess`，失敗細節位於 `Error`。
- 空條件視為 `true`；空計算式是 failure。
