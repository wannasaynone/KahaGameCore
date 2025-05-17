using System.Collections.Generic;
using KahaGameCore.Package.EffectProcessor.ValueContainer;
using UnityEngine;

namespace Assets.Scripts.StateMachine
{
    [CreateAssetMenu(fileName = "NewCondition", menuName = "StateMachine/Condition")]
    public class ConditionDefinition : ScriptableObject
    {
        public string conditionID;
        public string conditionName;

        [TextArea(3, 10)]
        public string conditionContent;

        public bool Evaluate(IValueContainer self, List<IValueContainer> targets)
        {
            string fullCommand = conditionContent.ReplaceWhitespace("").Replace("\n", "");
            string[] conditionParts = fullCommand.Split(';');

            for (int i = 0; i < conditionParts.Length; i++)
            {
                if (string.IsNullOrEmpty(conditionParts[i]))
                {
                    continue;
                }

                if (conditionParts[i] == "IsControlable")
                {
                    return self.GetTotal("IsControlable", false) == 1;
                }
                else if (!EvaluateCompareValue(conditionParts[i], self, targets))
                {
                    return false;
                }
            }

            return true;
        }

        private bool EvaluateCompareValue(string conditionPart, IValueContainer self, List<IValueContainer> targets)
        {
            //Debug.Log("EvaluateCompareValue: " + conditionPart);

            if (conditionPart.Contains(">") && !conditionPart.Contains("="))
            {
                string[] parts = conditionPart.Split('>');
                float value = GetValue(self, targets, parts[0]);
                float compareValue = GetValue(self, targets, parts[1]);

                //Debug.Log("Value: " + value + " CompareValue: " + compareValue);

                if (value <= compareValue)
                {
                    return false;
                }
            }
            else if (conditionPart.Contains("<") && conditionPart.Contains("="))
            {
                string[] parts = conditionPart.Split('<');
                float value = GetValue(self, targets, parts[0]);
                float compareValue = GetValue(self, targets, parts[1]);

                //Debug.Log("Value: " + value + " CompareValue: " + compareValue);

                if (value >= compareValue)
                {
                    return false;
                }
            }
            else if (conditionPart.Contains(">="))
            {
                string[] parts = conditionPart.Replace(">=", "|").Split('|');
                float value = GetValue(self, targets, parts[0]);
                float compareValue = GetValue(self, targets, parts[1]);

                //Debug.Log("Value: " + value + " CompareValue: " + compareValue);

                if (value < compareValue)
                {
                    return false;
                }
            }
            else if (conditionPart.Contains("<="))
            {
                string[] parts = conditionPart.Replace("<=", "|").Split('|');
                float value = GetValue(self, targets, parts[0]);
                float compareValue = GetValue(self, targets, parts[1]);

                //Debug.Log("Value: " + value + " CompareValue: " + compareValue);

                if (value > compareValue)
                {
                    return false;
                }
            }
            else if (conditionPart.Contains("=="))
            {
                string[] parts = conditionPart.Replace("==", "|").Split('|');
                float value = GetValue(self, targets, parts[0]);
                float compareValue = GetValue(self, targets, parts[1]);

                //Debug.Log("Value: " + value + " CompareValue: " + compareValue);

                if (value != compareValue)
                {
                    return false;
                }
            }
            else if (conditionPart.Contains("!="))
            {
                string[] parts = conditionPart.Replace("!=", "|").Split('|');
                float value = GetValue(self, targets, parts[0]);
                float compareValue = GetValue(self, targets, parts[1]);

                //Debug.Log("Value: " + value + " CompareValue: " + compareValue);

                if (value == compareValue)
                {
                    return false;
                }
            }
            else
            {
                Debug.LogError("Condition not recognized compare symbol: " + conditionPart);
                return false;
            }

            return true;
        }

        private float GetValue(IValueContainer self, List<IValueContainer> targets, string valueTarget)
        {
            if (float.TryParse(valueTarget, out float value))
            {
                return value;
            }

            if (valueTarget.Contains("."))
            {
                string[] valueParts = valueTarget.Split('.');
                string container = valueParts[0];
                string valueProperty = valueParts[1];

                if (container == "Self")
                {
                    return self.GetTotal(valueProperty, false);
                }
                else if (container == "Hero")
                {
                    return targets[0].GetTotal(valueProperty, false);
                }
                else
                {
                    Debug.LogError("Container not recognized: " + container);
                    return 0;
                }
            }
            else
            {
                switch (valueTarget)
                {
                    case "StateTime":
                        return StateDefinition.stateTimer;
                    default:
                        Debug.LogError("Value not recognized: " + valueTarget);
                        return 0;
                }
            }
        }
    }
}
