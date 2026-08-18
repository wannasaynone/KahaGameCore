using System;
using System.Collections.Generic;
using KahaGameCore.Expressions;

namespace KahaGameCore.Parameters
{
    public sealed class ParameterStore
    {
        private readonly Dictionary<string, ParameterDefinition> definitions = new Dictionary<string, ParameterDefinition>();
        private readonly Dictionary<string, ParameterValue> values = new Dictionary<string, ParameterValue>();
        private readonly Expressions.Expressions expressions = new Expressions.Expressions();
        private readonly IExpressionContext expressionContext;

        public event Action<ParameterChanged> Changed;

        public ParameterStore(IEnumerable<ParameterDefinition> definitions)
        {
            expressionContext = new ParameterExpressionContext(this);
            foreach (ParameterDefinition definition in definitions)
            {
                if (this.definitions.ContainsKey(definition.Key))
                {
                    throw new InvalidParameterDefinitionException(definition.Key, "Key is duplicated.");
                }

                this.definitions.Add(definition.Key, definition);
                values.Add(definition.Key, definition.InitialValue);
            }
        }

        public int GetInt(string key)
        {
            return GetValue(key, ParameterType.Int).AsInt();
        }

        public float GetFloat(string key)
        {
            return GetValue(key, ParameterType.Float).AsFloat();
        }

        public bool GetBool(string key)
        {
            return GetValue(key, ParameterType.Bool).AsBool();
        }

        public string GetString(string key)
        {
            return GetValue(key, ParameterType.String).AsString();
        }

        public bool TryGetValue(string key, out ParameterValue value)
        {
            return values.TryGetValue(key, out value);
        }

        public IReadOnlyList<ParameterRuntimeValue> CaptureCurrentValues()
        {
            List<ParameterRuntimeValue> currentValues =
                new List<ParameterRuntimeValue>(definitions.Count);
            foreach (KeyValuePair<string, ParameterDefinition> pair in definitions)
            {
                currentValues.Add(new ParameterRuntimeValue(
                    pair.Value,
                    values[pair.Key]));
            }

            currentValues.Sort((left, right) => string.Compare(
                left.Definition.Key,
                right.Definition.Key,
                StringComparison.Ordinal));
            return currentValues.AsReadOnly();
        }

        public ExpressionResult<float> Calculate(string formula)
        {
            return expressions.Calculate(formula, expressionContext);
        }

        public ExpressionResult<bool> EvaluateCondition(string condition)
        {
            return expressions.EvaluateCondition(condition, expressionContext);
        }

        public void ResetToInitial()
        {
            Dictionary<string, ParameterValue> initialValues = new Dictionary<string, ParameterValue>();
            foreach (KeyValuePair<string, ParameterDefinition> pair in definitions)
            {
                initialValues.Add(pair.Key, pair.Value.InitialValue);
            }

            ReplaceValues(initialValues);
        }

        public ParameterSnapshot Capture()
        {
            return new ParameterSnapshot(ParameterSnapshot.CurrentSchemaVersion, values);
        }

        public void Restore(ParameterSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.SchemaVersion != ParameterSnapshot.CurrentSchemaVersion)
            {
                throw new ParameterSnapshotException(
                    $"Unsupported parameter snapshot schema {snapshot.SchemaVersion}.");
            }

            Dictionary<string, ParameterValue> restoredValues = new Dictionary<string, ParameterValue>();
            foreach (KeyValuePair<string, ParameterDefinition> pair in definitions)
            {
                restoredValues.Add(pair.Key, pair.Value.InitialValue);
            }

            foreach (KeyValuePair<string, ParameterValue> pair in snapshot.Values)
            {
                ParameterDefinition definition = GetDefinition(pair.Key);
                EnsureType(definition, pair.Value.Type);
                restoredValues[pair.Key] = Clamp(definition, pair.Value);
            }

            ReplaceValues(restoredValues);
        }

        public void Set(string key, int value)
        {
            ParameterDefinition definition = GetDefinition(key);
            EnsureType(definition, ParameterType.Int);
            int clamped = Math.Min(
                definition.MaxValue.Value.AsInt(),
                Math.Max(definition.MinValue.Value.AsInt(), value));
            SetValue(key, ParameterValue.FromInt(clamped));
        }

        public void Set(string key, float value)
        {
            ParameterDefinition definition = GetDefinition(key);
            EnsureType(definition, ParameterType.Float);
            float clamped = Math.Min(
                definition.MaxValue.Value.AsFloat(),
                Math.Max(definition.MinValue.Value.AsFloat(), value));
            SetValue(key, ParameterValue.FromFloat(clamped));
        }

        public void Set(string key, bool value)
        {
            SetValue(key, ParameterValue.FromBool(value));
        }

        public void Set(string key, string value)
        {
            SetValue(key, ParameterValue.FromString(value));
        }

        public void Add(string key, int amount)
        {
            Set(key, GetInt(key) + amount);
        }

        public void Add(string key, float amount)
        {
            Set(key, GetFloat(key) + amount);
        }

        private void SetValue(string key, ParameterValue newValue)
        {
            ParameterDefinition definition = GetDefinition(key);
            EnsureType(definition, newValue.Type);
            ParameterValue oldValue = GetValue(key);
            if (oldValue == newValue)
            {
                return;
            }

            values[key] = newValue;
            Changed?.Invoke(new ParameterChanged(key, oldValue, newValue));
        }

        private void ReplaceValues(Dictionary<string, ParameterValue> newValues)
        {
            List<ParameterChanged> changes = new List<ParameterChanged>();
            foreach (KeyValuePair<string, ParameterValue> pair in newValues)
            {
                ParameterValue oldValue = values[pair.Key];
                if (oldValue != pair.Value)
                {
                    changes.Add(new ParameterChanged(pair.Key, oldValue, pair.Value));
                }
            }

            values.Clear();
            foreach (KeyValuePair<string, ParameterValue> pair in newValues)
            {
                values.Add(pair.Key, pair.Value);
            }

            foreach (ParameterChanged change in changes)
            {
                Changed?.Invoke(change);
            }
        }

        private static ParameterValue Clamp(ParameterDefinition definition, ParameterValue value)
        {
            if (definition.Type == ParameterType.Int)
            {
                int clamped = Math.Min(
                    definition.MaxValue.Value.AsInt(),
                    Math.Max(definition.MinValue.Value.AsInt(), value.AsInt()));
                return ParameterValue.FromInt(clamped);
            }

            if (definition.Type == ParameterType.Float)
            {
                float clamped = Math.Min(
                    definition.MaxValue.Value.AsFloat(),
                    Math.Max(definition.MinValue.Value.AsFloat(), value.AsFloat()));
                return ParameterValue.FromFloat(clamped);
            }

            return value;
        }

        private ParameterDefinition GetDefinition(string key)
        {
            if (!definitions.TryGetValue(key, out ParameterDefinition definition))
            {
                throw new UnknownParameterException(key);
            }

            return definition;
        }

        private ParameterValue GetValue(string key)
        {
            if (!values.TryGetValue(key, out ParameterValue value))
            {
                throw new UnknownParameterException(key);
            }

            return value;
        }

        private ParameterValue GetValue(string key, ParameterType expectedType)
        {
            ParameterValue value = GetValue(key);
            if (value.Type != expectedType)
            {
                throw new ParameterTypeMismatchException(key, expectedType, value.Type);
            }

            return value;
        }

        private static void EnsureType(ParameterDefinition definition, ParameterType actualType)
        {
            if (definition.Type != actualType)
            {
                throw new ParameterTypeMismatchException(definition.Key, definition.Type, actualType);
            }
        }
    }
}
