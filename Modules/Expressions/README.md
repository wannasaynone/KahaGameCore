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

需要舊 Calculator 的 Caster／Target 能力時，另外引用 integration assembly：

- `KahaGameCore.Modules.Expressions`
- `KahaGameCore.Modules.Expressions.ValueContainer`

```csharp
var context = new ValueContainerExpressionContext(caster, target);

ExpressionResult<float> result = expressions.Calculate(
    "Caster.HP - Target.Defense",
    context);
```

`Caster.X` 讀取 `caster.GetTotal("X", false)`；`Target.X` 同理。只讀 base value 時：

```csharp
var context = new ValueContainerExpressionContext(caster, target, baseOnly: true);
```

Expressions core 不認得 Caster／Target，也不依賴 `IValueContainer`；映射完全位於 adapter assembly。

### 已知限制

`IValueContainer` 目前沒有 `Contains`／`TryGet`，所以 container 已提供時，`Caster.UnknownTag` 會遵循 `GetTotal` 的既有語意得到 `0`。未知 prefix、缺少對應 container 或不完整的 `Caster.`／`Target.` 則回傳 structured unknown-symbol failure。

## Parameters

需要讓計算式或條件式讀取 `ParameterStore` 時，另外引用 integration assembly：

- `KahaGameCore.Modules.Expressions`
- `KahaGameCore.Modules.Parameters`
- `KahaGameCore.Modules.Expressions.Parameters`

```csharp
var context = new ParameterExpressionContext(parameters);

ExpressionResult<float> result = expressions.Calculate(
    "$Day + $Supplies * 2",
    context);
```

`Int`、`Float` 會映射成 Expressions 的 Number，`Bool` 會映射成 Boolean。Expressions 目前沒有 String 值型別，因此 String Parameter 與不存在的 key 都不會被 context 解讀，最終回傳 structured `UnknownSymbol` failure。

Expressions core 不依賴 Parameters；這個依賴只存在於 adapter assembly。
