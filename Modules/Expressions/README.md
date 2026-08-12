# Expressions

## 目的

通用計算式與條件式 Module。核心 assembly `KahaGameCore.Modules.Expressions` 不依賴 Unity、GameFlow、Effects 或 ValueContainer。

## 快速開始

```csharp
ExpressionResult<float> number = expressions.Calculate(formula, context);
ExpressionResult<bool> condition = expressions.EvaluateCondition(source, context);
```

符號一律交給 `IExpressionContext` adapter 解讀。計算式允許 `Random(min,max)`；條件式支援比較、括號、`!`、`&&`、`||`，並禁止 `Random`。

## Caster／Target ValueContainer

Caster／Target 計算式使用 ValueContainer module 提供的求值入口：

- `KahaGameCore.Modules.Expressions`
- `KahaGameCore.Modules.ValueContainer`

```csharp
var expressions = new ValueContainerExpressions(caster, target);

ExpressionResult<float> result =
    expressions.Calculate("Caster.HP - Target.Defense");
```

`Caster.X` 讀取 `caster.GetTotal("X", false)`；`Target.X` 同理。只讀 base value 時：

```csharp
var expressions = new ValueContainerExpressions(caster, target, baseOnly: true);
```

Expressions core 不認得 Caster／Target，也不依賴 `IValueContainer`；映射是 ValueContainer module 的內部 implementation。

### 已知限制

`IValueContainer` 未定義 `Contains`／`TryGet`，所以 container 已提供時，`Caster.UnknownTag` 會依 `GetTotal` 的語意得到 `0`。未知 prefix、缺少對應 container 或不完整的 `Caster.`／`Target.` 則回傳 structured unknown-symbol failure。

## Parameters

`ParameterStore` 直接提供參數計算與條件求值：

- `KahaGameCore.Modules.Expressions`
- `KahaGameCore.Modules.Parameters`

```csharp
ExpressionResult<float> result =
    parameters.Calculate("$Day + $Supplies * 2");
```

`Int`、`Float` 會映射成 Expressions 的 Number，`Bool` 會映射成 Boolean。Expressions 不支援 String 值型別，因此 String Parameter 與不存在的 key 都會回傳 structured `UnknownSymbol` failure。

Expressions core 不依賴 Parameters；Parameters module 內部使用 Expressions implementation。
