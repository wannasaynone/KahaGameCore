using System.Collections.Generic;
using KahaGameCore.Package.EffectProcessor.ValueContainer;
using UnityEngine;

namespace Assets.Scripts.StateMachine
{
    [CreateAssetMenu(fileName = "NewCondition", menuName = "StateMachine/Condition")]
    public class ConditionDefinition : ScriptableObject
    {
        public string conditionID;

        [TextArea(3, 10)]
        public string conditionContent;

        public bool Evaluate(IValueContainer self, List<IValueContainer> targets, StateDefinition stateDefinition)
        {
            if (string.IsNullOrEmpty(conditionContent))
            {
                Debug.LogWarning("Condition content is empty or null, will pass anyway.");
                return true;
            }

            string fullCommand = conditionContent.ReplaceWhitespace("").Replace("\n", "");
            string[] conditionParts = fullCommand.Split(';');

            for (int i = 0; i < conditionParts.Length; i++)
            {
                if (string.IsNullOrEmpty(conditionParts[i]))
                {
                    continue;
                }
                //Debug.Log($"Evaluating condition part: {conditionParts[i]}");
                if (conditionParts[i] == "IsControlable")
                {
                    if (self.GetTotal("IsControlable", false) != 1)
                    {
                        return false; // Not controllable
                    }
                }
                else if (conditionParts[i] == "IsHeroSameRoom")
                {
                    if (targets == null || targets.Count == 0)
                    {
                        return false; // No Hero target available
                    }

                    int selfRoomIndex = self.GetTotal("CurrentRoom", false);
                    int heroRoomIndex = targets[0].GetTotal("CurrentRoom", false);

                    // Debug.Log($"Self Room Index: {selfRoomIndex}, Hero Room Index: {heroRoomIndex} - IsHeroSameRoom");

                    if (selfRoomIndex < 0 || heroRoomIndex < 0
                        || selfRoomIndex != heroRoomIndex)
                    {
                        return false; // Invalid room index or not in the same room
                    }
                }
                else if (conditionParts[i] == "IsHeroInRange")
                {
                    if (targets == null || targets.Count == 0)
                    {
                        return false; // No Hero target available
                    }

                    float selfX = float.Parse(self.GetStringKeyValue("PositionX"));
                    float heroX = float.Parse(targets[0].GetStringKeyValue("PositionX"));
                    float range = float.Parse(self.GetStringKeyValue("AttackRange"));

                    // 檢查是否在攻擊範圍內
                    if (Mathf.Abs(selfX - heroX) > range)
                    {
                        return false; // Hero is out of range
                    }
                }
                else if (conditionParts[i] == "IsHeroLeft")
                {
                    if (targets == null || targets.Count == 0)
                    {
                        continue; // No Hero target available, just skip this condition
                    }

                    int selfRoomIndex = self.GetTotal("CurrentRoom", false);
                    int heroRoomIndex = targets[0].GetTotal("CurrentRoom", false);

                    // Debug.Log($"Self Room Index: {selfRoomIndex}, Hero Room Index: {heroRoomIndex} - IsHeroLeft");

                    if (selfRoomIndex < 0 || heroRoomIndex < 0)
                    {
                        continue; // Hero or self is not in any room, skip this condition
                    }

                    if (selfRoomIndex == heroRoomIndex)
                    {
                        return false; // Invalid room index or not in the same room
                    }
                }
                else if (!EvaluateCompareValue(conditionParts[i], self, targets, stateDefinition))
                {
                    return false;
                }
            }

            //Debug.Log("All condition parts passed.");
            return true;
        }

        private bool EvaluateCompareValue(string conditionPart, IValueContainer self, List<IValueContainer> targets, StateDefinition stateDefinition)
        {
            //Debug.Log("EvaluateCompareValue: " + conditionPart);

            // Use a more robust comparison parsing approach
            var comparisonResult = ParseComparison(conditionPart);
            if (comparisonResult == null)
            {
                Debug.LogError("Condition not recognized compare symbol: " + conditionPart);
                return false;
            }

            float leftValue = GetValue(self, targets, comparisonResult.LeftOperand, stateDefinition);
            float rightValue = GetValue(self, targets, comparisonResult.RightOperand, stateDefinition);

            //Debug.Log($"Comparing [{conditionPart}] {leftValue} {comparisonResult.Operator} {rightValue}");

            switch (comparisonResult.Operator)
            {
                case ">":
                    return leftValue > rightValue;
                case "<":
                    return leftValue < rightValue;
                case ">=":
                    return leftValue >= rightValue;
                case "<=":
                    return leftValue <= rightValue;
                case "==":
                    return Mathf.Approximately(leftValue, rightValue);
                case "!=":
                    return !Mathf.Approximately(leftValue, rightValue);
                default:
                    Debug.LogError($"Unknown comparison operator: {comparisonResult.Operator}");
                    return false;
            }
        }

        private ComparisonResult ParseComparison(string conditionPart)
        {
            // Check operators in order of precedence (longer operators first to avoid conflicts)
            // 檢查運算符的順序很重要：先檢查長的運算符（如">="），再檢查短的（如">"）
            // 這樣可以避免">="被誤認為">"

            if (conditionPart.Contains("<>")
                || conditionPart.Contains("><"))
            {
                return null;
            }

            string[] operators = { ">=", "<=", "==", "!=", ">", "<" };

            foreach (string op in operators)
            {
                int index = conditionPart.IndexOf(op);
                if (index > 0 && index < conditionPart.Length - op.Length)
                {
                    string leftOperand = conditionPart.Substring(0, index).Trim();
                    string rightOperand = conditionPart.Substring(index + op.Length).Trim();

                    if (!string.IsNullOrEmpty(leftOperand) && !string.IsNullOrEmpty(rightOperand))
                    {
                        return new ComparisonResult
                        {
                            LeftOperand = leftOperand,
                            Operator = op,
                            RightOperand = rightOperand
                        };
                    }
                }
            }

            return null;
        }

        private class ComparisonResult
        {
            public string LeftOperand { get; set; }
            public string Operator { get; set; }
            public string RightOperand { get; set; }
        }

        private float GetValue(IValueContainer self, List<IValueContainer> targets, string valueTarget, StateDefinition stateDefinition)
        {
            ValueResolverFactory.ResolveValue(valueTarget, self, targets, stateDefinition, out float resolvedValue);
            return resolvedValue;
        }
    }

    public static class ValueResolverFactory
    {
        public static bool ResolveValue(string valueTarget, IValueContainer self, List<IValueContainer> targets, StateDefinition stateDefinition, out float resolvedValue)
        {
            if (string.IsNullOrEmpty(valueTarget))
            {
                Debug.LogError("Value target is null or empty");
                resolvedValue = 0;
                return false;
            }

            // Try to parse as direct number first
            if (float.TryParse(valueTarget, out float directValue))
            {
                resolvedValue = directValue;
                return true;
            }

            // Check for function calls
            if (IsFunctionCall(valueTarget))
            {
                resolvedValue = ResolveFunctionCall(valueTarget, self, targets, stateDefinition);
                return true;
            }

            // Check for container.property format
            if (valueTarget.Contains("."))
            {
                resolvedValue = ResolveContainerProperty(valueTarget, self, targets);
                return true;
            }

            // Check for special values
            return ResolveSpecialValue(valueTarget, stateDefinition, out resolvedValue);
        }

        private static bool IsFunctionCall(string valueTarget)
        {
            return valueTarget.Contains("(") && valueTarget.Contains(")");
        }

        private static float ResolveFunctionCall(string valueTarget, IValueContainer self, List<IValueContainer> targets, StateDefinition stateDefinition)
        {
            if (IsRandomFunction(valueTarget))
            {
                return EvaluateRandomFunction(valueTarget, self, targets, stateDefinition);
            }

            Debug.LogError($"Unknown function call: {valueTarget}");
            return 0;
        }

        private static float ResolveContainerProperty(string valueTarget, IValueContainer self, List<IValueContainer> targets)
        {
            string[] valueParts = valueTarget.Split('.');
            if (valueParts.Length != 2 || string.IsNullOrEmpty(valueParts[1]))
            {
                Debug.LogError($"Invalid container.property format: {valueTarget}");
                return 0;
            }

            string container = valueParts[0];
            string valueProperty = valueParts[1];

            switch (container)
            {
                case "Self":
                    return self.GetTotal(valueProperty, false);
                case "Hero":
                    if (targets == null || targets.Count == 0)
                    {
                        Debug.LogError("Hero target not available");
                        return 0;
                    }
                    return targets[0].GetTotal(valueProperty, false);
                default:
                    Debug.LogError($"Container not recognized: {container}");
                    return 0;
            }
        }

        private static bool ResolveSpecialValue(string valueTarget, StateDefinition stateDefinition, out float resolvedValue)
        {
            switch (valueTarget)
            {
                case "StateTime":
                    resolvedValue = stateDefinition.stateTimer;
                    return true;
                default:
                    Debug.LogError($"Value not recognized: {valueTarget}");
                    resolvedValue = 0;
                    return false;
            }
        }

        private static bool IsRandomFunction(string valueTarget)
        {
            if (string.IsNullOrEmpty(valueTarget))
                return false;

            // Check basic format: starts with "Random(" and ends with ")"
            if (!valueTarget.StartsWith("Random(") || !valueTarget.EndsWith(")"))
            {
                return false;
            }

            // Extract content between parentheses
            string content = "";
            int startIdx = "Random(".Length;
            int endIdx = valueTarget.LastIndexOf(')');

            if (endIdx <= startIdx)
            {
                Debug.LogError($"Invalid Random function syntax: {valueTarget}");
                return false;
            }

            for (int i = startIdx; i < endIdx; i++)
            {
                content += valueTarget[i];
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                Debug.LogError($"Random function has empty parameters: {valueTarget}");
                return false;
            }

            return true;
        }

        private static float EvaluateRandomFunction(string randomExpression, IValueContainer self, List<IValueContainer> targets, StateDefinition stateDefinition)
        {
            // Extract parameters safely
            int startIndex = "Random(".Length;
            int endIndex = randomExpression.LastIndexOf(')');

            if (endIndex <= startIndex)
            {
                Debug.LogError($"Invalid Random function syntax: {randomExpression}");
                return 0;
            }

            // Extract parameters content
            string parameters = "";
            for (int i = startIndex; i < endIndex; i++)
            {
                parameters += randomExpression[i];
            }

            // Split parameters by comma
            string[] parts = parameters.Split(',');

            if (parts.Length != 2)
            {
                Debug.LogError($"Random function requires exactly 2 parameters separated by comma. Got {parts.Length} parameters in: {randomExpression}");
                return 0;
            }

            // Trim whitespace from parameters
            string minParam = parts[0].Trim();
            string maxParam = parts[1].Trim();

            // Check for empty parameters after trimming
            if (string.IsNullOrEmpty(minParam) || string.IsNullOrEmpty(maxParam))
            {
                Debug.LogError($"Random function has empty parameters: min='{minParam}', max='{maxParam}' in {randomExpression}");
                return 0;
            }

            // Recursively evaluate min and max values
            if (!ResolveValue(minParam, self, targets, stateDefinition, out float minValue)
                || !ResolveValue(maxParam, self, targets, stateDefinition, out float maxValue))
            {
                Debug.LogError($"Failed to resolve Random function parameters: min='{minParam}', max='{maxParam}' in {randomExpression}");
                return 0;
            }

            // Validate the resolved values
            if (float.IsNaN(minValue) || float.IsInfinity(minValue))
            {
                Debug.LogError($"Random function min value is invalid (NaN or Infinity): {minValue} from '{minParam}' in {randomExpression}");
                return 0;
            }

            if (float.IsNaN(maxValue) || float.IsInfinity(maxValue))
            {
                Debug.LogError($"Random function max value is invalid (NaN or Infinity): {maxValue} from '{maxParam}' in {randomExpression}");
                return 0;
            }

            // Ensure min <= max
            if (minValue > maxValue)
            {
                Debug.LogWarning($"Random function: min value ({minValue}) is greater than max value ({maxValue}) in {randomExpression}. Swapping values.");
                return 0f;
            }

            // Generate random value between min and max
            float randomValue = Random.Range(minValue, maxValue);

            return randomValue;
        }
    }
}
