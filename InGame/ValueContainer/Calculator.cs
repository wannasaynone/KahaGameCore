using System.Collections.Generic;

namespace KahaGameCore.ValueContainer
{
    public static class Calculator
    {
        private static Dictionary<string, float> m_tagToValue = new Dictionary<string, float>();

        public class CalculateData
        {
            public IValueContainer caster = null;
            public IValueContainer target = null;
            public string formula = "";
            public bool useBaseValue = false;
        }
        public static float Calculate(CalculateData data)
        {
            // Handle empty formula
            if (string.IsNullOrEmpty(data.formula))
            {
                UnityEngine.Debug.LogError("[Error] Formula is empty");
                return 0;
            }

            // Trim the formula
            data.formula = data.formula.Trim();
            UnityEngine.Debug.Log($"Calculating formula: {data.formula}");

            try
            {
                // Check if the formula contains arithmetic operators outside of parentheses
                bool containsOperatorsOutsideParentheses = ContainsOperatorsOutsideParentheses(data.formula);

                // First, handle function calls like Random() and Read()
                if (data.formula.Contains("(") && data.formula.Contains(")"))
                {
                    // Check if parentheses are balanced
                    if (!AreParenthesesBalanced(data.formula))
                    {
                        UnityEngine.Debug.LogError($"Mismatched parentheses in formula: {data.formula}");
                        return 0;
                    }

                    // Only treat as a single function call if there are no operators outside parentheses
                    // and the formula starts with a function name and ends with a closing parenthesis
                    if (!containsOperatorsOutsideParentheses &&
                        (data.formula.StartsWith("Random") || data.formula.StartsWith("Read")) &&
                        data.formula.LastIndexOf(')') == data.formula.Length - 1)
                    {
                        int openParenIndex = data.formula.IndexOf('(');
                        int closeParenIndex = data.formula.LastIndexOf(')');

                        if (openParenIndex > 0 && closeParenIndex > openParenIndex)
                        {
                            string command = data.formula.Substring(0, openParenIndex).Trim();
                            string parameters = data.formula.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1).Trim();

                            return GetValueByCommand(data, command, parameters);
                        }
                    }

                    // For formulas with multiple function calls or nested parentheses,
                    // use the arithmetic processing which handles them through tokenization
                    if (containsOperatorsOutsideParentheses)
                    {
                        string result = Arithmetic(data, data.formula);
                        if (float.TryParse(result, out float value))
                        {
                            return value;
                        }
                    }
                    else
                    {
                        // Use parentheses evaluation logic for nested parentheses without operators
                        return EvaluateExpressionWithParentheses(data);
                    }
                }

                // Handle arithmetic expressions without parentheses
                if (data.formula.Contains("+") || data.formula.Contains("-") ||
                    data.formula.Contains("*") || data.formula.Contains("/"))
                {
                    string result = Arithmetic(data, data.formula);
                    if (float.TryParse(result, out float value))
                    {
                        return value;
                    }
                }

                // Handle direct values (numbers or property references)
                if (float.TryParse(data.formula, out float directValue))
                {
                    return directValue;
                }
                else
                {
                    return GetValueByParaString(data, data.formula);
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error calculating formula '{data.formula}': {ex.Message}");
                return 0;
            }
        }

        private static float EvaluateExpressionWithParentheses(CalculateData data)
        {
            string formula = data.formula;
            UnityEngine.Debug.Log($"Evaluating expression with parentheses: {formula}");

            // Check if parentheses are balanced
            if (!AreParenthesesBalanced(formula))
            {
                UnityEngine.Debug.LogError($"Mismatched parentheses in formula: {formula}");
                return 0;
            }

            // Find the innermost parentheses
            int openIndex = formula.LastIndexOf('(');
            if (openIndex < 0)
            {
                UnityEngine.Debug.LogError($"Mismatched parentheses in formula: {formula}");
                return 0;
            }

            int closeIndex = formula.IndexOf(')', openIndex);
            if (closeIndex < 0)
            {
                UnityEngine.Debug.LogError($"Mismatched parentheses in formula: {formula}");
                return 0;
            }

            // Extract the content inside the parentheses
            string innerExpression = formula.Substring(openIndex + 1, closeIndex - openIndex - 1);

            // Check if this is a function call
            string prefix = "";
            if (openIndex > 0)
            {
                // Look for a function name before the opening parenthesis
                for (int i = openIndex - 1; i >= 0; i--)
                {
                    char c = formula[i];
                    if (char.IsLetterOrDigit(c) || c == '_')
                    {
                        prefix = c + prefix;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            float innerResult;
            if (prefix == "Random" || prefix == "Read")
            {
                // This is a function call
                innerResult = GetValueByCommand(data, prefix, innerExpression);

                // Replace the entire function call with the result
                string functionCall = prefix + "(" + innerExpression + ")";
                formula = formula.Replace(functionCall, innerResult.ToString());
            }
            else
            {
                // Evaluate the inner expression
                CalculateData innerData = new CalculateData
                {
                    caster = data.caster,
                    target = data.target,
                    formula = innerExpression,
                    useBaseValue = data.useBaseValue
                };

                innerResult = Calculate(innerData);

                // Replace the parentheses expression with its result
                formula = formula.Substring(0, openIndex) + innerResult.ToString() + formula.Substring(closeIndex + 1);
            }

            // Recursively evaluate the updated formula
            CalculateData updatedData = new CalculateData
            {
                caster = data.caster,
                target = data.target,
                formula = formula,
                useBaseValue = data.useBaseValue
            };

            return Calculate(updatedData);
        }

        public static void Remember(string tag, float value)
        {
            if (m_tagToValue.ContainsKey(tag))
            {
                m_tagToValue[tag] = value;
            }
            else
            {
                m_tagToValue.Add(tag, value);
            }
        }

        private static float GetStatsValue(IValueContainer unit, string statsName, bool useBaseValue)
        {
            if (unit == null)
            {
                UnityEngine.Debug.LogError("[CombatUtility][GetStatusValue] unit == null");
                return 0;
            }

            return unit.GetTotal(statsName, useBaseValue);
        }

        private static float GetValueByCommand(CalculateData data, string command, string paraString)
        {
            UnityEngine.Debug.Log($"GetValueByCommand: command={command}, paraString={paraString}");
            bool _minus = false;
            if (command.StartsWith("-"))
            {
                command = command.Remove(0, 1);
                _minus = true;
            }
            command = command.Trim();
            switch (command)
            {
                case "Random":
                    {
                        string[] _varParts = paraString.Split(',');

                        // Create new CalculateData instances for each parameter
                        CalculateData minData = new CalculateData
                        {
                            caster = data.caster,
                            target = data.target,
                            formula = _varParts[0].Trim(),
                            useBaseValue = data.useBaseValue
                        };

                        CalculateData maxData = new CalculateData
                        {
                            caster = data.caster,
                            target = data.target,
                            formula = _varParts[1].Trim(),
                            useBaseValue = data.useBaseValue
                        };

                        // Calculate each parameter value
                        float minValue = Calculate(minData);
                        float maxValue = Calculate(maxData);

                        int _min = System.Convert.ToInt32(minValue);
                        int _max = System.Convert.ToInt32(maxValue);

                        if (_minus)
                            return -UnityEngine.Random.Range(_min, _max);
                        else
                            return UnityEngine.Random.Range(_min, _max);
                    }
                case "Read":
                    {
                        // If the parameter contains nested function calls or expressions,
                        // we need to evaluate it first to get the actual key name
                        string key;

                        if (paraString.Contains("(") ||
                            paraString.Contains("+") ||
                            paraString.Contains("-") ||
                            paraString.Contains("*") ||
                            paraString.Contains("/"))
                        {
                            // Create a new CalculateData for evaluating the parameter
                            CalculateData paramData = new CalculateData
                            {
                                caster = data.caster,
                                target = data.target,
                                formula = paraString.Trim(),
                                useBaseValue = data.useBaseValue
                            };

                            // Calculate the parameter to get the key name
                            key = Calculate(paramData).ToString();
                        }
                        else
                        {
                            // Simple case - the parameter is directly the key
                            key = paraString.Trim();
                        }

                        // Check if the key exists in the dictionary
                        if (!m_tagToValue.ContainsKey(key))
                        {
                            UnityEngine.Debug.LogError($"The given key '{key}' was not present in the dictionary.");
                            return 0; // Return 0 for non-existent keys
                        }

                        return m_tagToValue[key]; // Return the value associated with the key
                    }
                default:
                    {
                        UnityEngine.Debug.LogError("[CombatUtility][GetValueByCommand] Invaild command=" + command);
                        return 0;
                    }
            }
        }

        private static bool AreParenthesesBalanced(string expression)
        {
            int count = 0;
            foreach (char c in expression)
            {
                if (c == '(')
                    count++;
                else if (c == ')')
                    count--;

                // If at any point we have more closing than opening parentheses, it's unbalanced
                if (count < 0)
                    return false;
            }
            // If count is 0, all parentheses are balanced
            return count == 0;
        }

        private static float GetValueByParaString(CalculateData data, string paraString)
        {
            UnityEngine.Debug.Log($"GetValueByParaString: paraString={paraString}");
            paraString = paraString.Trim();

            bool _minus = false;
            if (paraString.StartsWith("-"))
            {
                paraString = paraString.Remove(0, 1);
                _minus = true;
            }

            // Check if it's a remembered value first
            if (!paraString.Contains(".") && m_tagToValue.ContainsKey(paraString))
            {
                float rememberedValue = m_tagToValue[paraString];
                if (_minus)
                    rememberedValue = -rememberedValue;
                UnityEngine.Debug.Log($"GetValueByParaString result (remembered value): {rememberedValue}");
                return rememberedValue;
            }

            // Check if the string contains any parentheses
            if (paraString.Contains("("))
            {
                // Check if parentheses are balanced
                if (!AreParenthesesBalanced(paraString))
                {
                    UnityEngine.Debug.LogError($"Failed to parse left operand: {paraString}");
                    return 0;
                }
            }

            // Check if it's a function call (contains both opening and closing parentheses)
            if (paraString.Contains("(") && paraString.Contains(")"))
            {

                int openParenIndex = paraString.IndexOf('(');
                string command = paraString.Substring(0, openParenIndex).Trim();
                string parameters = paraString.Substring(openParenIndex + 1, paraString.Length - openParenIndex - 2).Trim();

                try
                {
                    float functionResult = GetValueByCommand(data, command, parameters);
                    if (_minus)
                        functionResult = -functionResult;
                    return functionResult;
                }
                catch (System.Exception)
                {
                    // If GetValueByCommand fails (e.g., for unknown commands), log error and return 0
                    UnityEngine.Debug.LogError($"Failed to parse left operand: {paraString}");
                    return 0;
                }
            }

            string[] _getValueData = paraString.Split('.');
            if (_getValueData.Length != 2)
            {
                UnityEngine.Debug.LogError($"Failed to parse left operand: {paraString}");
                return 0;
            }

            IValueContainer _getValueTarget = data.caster;

            switch (_getValueData[0].Trim())
            {
                case "Caster":
                    {
                        _getValueTarget = data.caster;
                        break;
                    }
                case "Target":
                    {
                        _getValueTarget = data.target;
                        break;
                    }
                default:
                    {
                        UnityEngine.Debug.LogError("[CombatUtility][GetValueByParaString] Invalid target=" + _getValueData[0]);
                        return 0;
                    }
            }

            float result;
            if (_minus)
                result = -GetStatsValue(_getValueTarget, _getValueData[1].Trim(), data.useBaseValue);
            else
                result = GetStatsValue(_getValueTarget, _getValueData[1].Trim(), data.useBaseValue);

            UnityEngine.Debug.Log($"GetValueByParaString result: {result}");
            return result;
        }

        private static string Arithmetic(CalculateData data, string arithmeticString)
        {
            UnityEngine.Debug.Log($"Arithmetic: arithmeticString={arithmeticString}");

            // First, handle all multiplications and divisions
            string result = HandleMultiplicationAndDivision(data, arithmeticString);

            // Then, handle all additions and subtractions
            result = HandleAdditionAndSubtraction(data, result);

            return result;
        }

        private static string HandleMultiplicationAndDivision(CalculateData data, string arithmeticString)
        {
            // Convert the string to a list of tokens (numbers and operators)
            List<string> tokens = TokenizeExpression(arithmeticString);

            // Process all multiplications and divisions first (left to right)
            for (int i = 1; i < tokens.Count - 1; i += 2)
            {
                if (tokens[i] == "*" || tokens[i] == "/")
                {
                    float leftValue;
                    float rightValue;

                    // Parse left operand
                    if (!float.TryParse(tokens[i - 1], out leftValue))
                    {
                        try
                        {
                            // Check if it's a function call
                            if (tokens[i - 1].Contains("(") && tokens[i - 1].Contains(")"))
                            {
                                // Create a new CalculateData for this token
                                CalculateData tokenData = new CalculateData
                                {
                                    caster = data.caster,
                                    target = data.target,
                                    formula = tokens[i - 1],
                                    useBaseValue = data.useBaseValue
                                };

                                // Calculate the value of this function call
                                leftValue = Calculate(tokenData);
                            }
                            else
                            {
                                leftValue = GetValueByParaString(data, tokens[i - 1]);
                            }
                        }
                        catch (System.Exception ex)
                        {
                            UnityEngine.Debug.LogError($"Failed to parse left operand: {tokens[i - 1]}, Error: {ex.Message}");
                            return "0";
                        }
                    }

                    // Parse right operand
                    if (!float.TryParse(tokens[i + 1], out rightValue))
                    {
                        try
                        {
                            // Check if it's a function call
                            if (tokens[i + 1].Contains("(") && tokens[i + 1].Contains(")"))
                            {
                                // Create a new CalculateData for this token
                                CalculateData tokenData = new CalculateData
                                {
                                    caster = data.caster,
                                    target = data.target,
                                    formula = tokens[i + 1],
                                    useBaseValue = data.useBaseValue
                                };

                                // Calculate the value of this function call
                                rightValue = Calculate(tokenData);
                            }
                            else
                            {
                                rightValue = GetValueByParaString(data, tokens[i + 1]);
                            }
                        }
                        catch (System.Exception ex)
                        {
                            UnityEngine.Debug.LogError($"Failed to parse right operand: {tokens[i + 1]}, Error: {ex.Message}");
                            return "0";
                        }
                    }

                    // Perform the operation
                    float result;
                    if (tokens[i] == "*")
                    {
                        result = leftValue * rightValue;
                        UnityEngine.Debug.Log($"Multiplication: {leftValue} * {rightValue} = {result}");
                    }
                    else // Division
                    {
                        if (rightValue == 0)
                        {
                            UnityEngine.Debug.LogError("Error evaluating expression: Division by zero");
                            return "0";
                        }
                        result = leftValue / rightValue;
                        UnityEngine.Debug.Log($"Division: {leftValue} / {rightValue} = {result}");
                    }

                    // Replace the operation and its operands with the result
                    tokens[i - 1] = result.ToString();
                    tokens.RemoveRange(i, 2);
                    i -= 2; // Adjust index after removal
                }
            }

            // Combine the remaining tokens back into a string
            return string.Join("", tokens);
        }

        private static string HandleAdditionAndSubtraction(CalculateData data, string arithmeticString)
        {
            // Convert the string to a list of tokens (numbers and operators)
            List<string> tokens = TokenizeExpression(arithmeticString);

            // Process all additions and subtractions (left to right)
            for (int i = 1; i < tokens.Count - 1; i += 2)
            {
                if (tokens[i] == "+" || tokens[i] == "-")
                {
                    float leftValue;
                    float rightValue;

                    // Parse left operand
                    if (!float.TryParse(tokens[i - 1], out leftValue))
                    {
                        try
                        {
                            // Check if it's a function call
                            if (tokens[i - 1].Contains("(") && tokens[i - 1].Contains(")"))
                            {
                                // Create a new CalculateData for this token
                                CalculateData tokenData = new CalculateData
                                {
                                    caster = data.caster,
                                    target = data.target,
                                    formula = tokens[i - 1],
                                    useBaseValue = data.useBaseValue
                                };

                                // Calculate the value of this function call
                                leftValue = Calculate(tokenData);
                            }
                            else
                            {
                                leftValue = GetValueByParaString(data, tokens[i - 1]);
                            }
                        }
                        catch (System.Exception ex)
                        {
                            UnityEngine.Debug.LogError($"Failed to parse left operand: {tokens[i - 1]}, Error: {ex.Message}");
                            return "0";
                        }
                    }

                    // Parse right operand
                    if (!float.TryParse(tokens[i + 1], out rightValue))
                    {
                        try
                        {
                            // Check if it's a function call
                            if (tokens[i + 1].Contains("(") && tokens[i + 1].Contains(")"))
                            {
                                // Create a new CalculateData for this token
                                CalculateData tokenData = new CalculateData
                                {
                                    caster = data.caster,
                                    target = data.target,
                                    formula = tokens[i + 1],
                                    useBaseValue = data.useBaseValue
                                };

                                // Calculate the value of this function call
                                rightValue = Calculate(tokenData);
                            }
                            else
                            {
                                rightValue = GetValueByParaString(data, tokens[i + 1]);
                            }
                        }
                        catch (System.Exception ex)
                        {
                            UnityEngine.Debug.LogError($"Failed to parse right operand: {tokens[i + 1]}, Error: {ex.Message}");
                            return "0";
                        }
                    }

                    // Perform the operation
                    float result;
                    if (tokens[i] == "+")
                    {
                        result = leftValue + rightValue;
                        UnityEngine.Debug.Log($"Addition: {leftValue} + {rightValue} = {result}");
                    }
                    else // Subtraction
                    {
                        result = leftValue - rightValue;
                        UnityEngine.Debug.Log($"Subtraction: {leftValue} - {rightValue} = {result}");
                    }

                    // Replace the operation and its operands with the result
                    tokens[i - 1] = result.ToString();
                    tokens.RemoveRange(i, 2);
                    i -= 2; // Adjust index after removal
                }
            }

            // Combine the remaining tokens back into a string
            return string.Join("", tokens);
        }

        private static bool ContainsOperatorsOutsideParentheses(string formula)
        {
            int parenthesesDepth = 0;

            for (int i = 0; i < formula.Length; i++)
            {
                char c = formula[i];

                if (c == '(')
                {
                    parenthesesDepth++;
                }
                else if (c == ')')
                {
                    parenthesesDepth--;
                }
                else if ((c == '+' || c == '-' || c == '*' || c == '/') && parenthesesDepth == 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<string> TokenizeExpression(string expression)
        {
            List<string> tokens = new List<string>();
            string currentToken = "";
            int parenthesesDepth = 0;

            for (int i = 0; i < expression.Length; i++)
            {
                char c = expression[i];

                // Track parentheses depth to avoid splitting inside function calls
                if (c == '(')
                {
                    parenthesesDepth++;
                    currentToken += c;
                }
                else if (c == ')')
                {
                    parenthesesDepth--;
                    currentToken += c;
                }
                // Only split by operators when not inside parentheses
                else if ((c == '+' || c == '-' || c == '*' || c == '/') && parenthesesDepth == 0)
                {
                    // Special case for unary minus (negative sign)
                    // A minus is a unary operator if:
                    // 1. It's at the start of the expression, or
                    // 2. It follows another operator, or
                    // 3. It follows an opening parenthesis
                    bool isUnaryMinus = false;
                    if (c == '-')
                    {
                        if (i == 0) // At the start of the expression
                        {
                            isUnaryMinus = true;
                        }
                        else
                        {
                            char prevChar = expression[i - 1];
                            // After another operator or opening parenthesis
                            if (prevChar == '+' || prevChar == '-' || prevChar == '*' || prevChar == '/' || prevChar == '(')
                            {
                                isUnaryMinus = true;
                            }
                        }
                    }

                    if (isUnaryMinus)
                    {
                        // For unary minus, add it to the current token
                        currentToken += c;
                    }
                    else
                    {
                        // For binary operators, add the current token to the list and start a new one
                        if (!string.IsNullOrEmpty(currentToken))
                        {
                            tokens.Add(currentToken.Trim());
                            currentToken = "";
                        }

                        // Add the operator as a separate token
                        tokens.Add(c.ToString());
                    }
                }
                else
                {
                    // Build up the current token
                    currentToken += c;
                }
            }

            // Add the last token if there is one
            if (!string.IsNullOrEmpty(currentToken))
            {
                tokens.Add(currentToken.Trim());
            }

            return tokens;
        }
    }
}
