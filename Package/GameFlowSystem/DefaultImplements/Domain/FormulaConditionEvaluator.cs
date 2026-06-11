using System;
using UnityEngine;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    /// <summary>
    /// 以 KahaGameCore.Calculator 為基礎的條件式求值器。
    /// 比較式左右兩側皆為 Calculator 公式，因此可使用 $Tag、四則運算與 Random()。
    /// </summary>
    public class FormulaConditionEvaluator : IConditionEvaluator
    {
        // 順序重要：先比對雙字元運算子，避免 ">=" 被誤判為 ">"。
        private static readonly string[] comparisonOperators = { ">=", "<=", "==", "!=", ">", "<" };

        private readonly IGameState gameState;

        public FormulaConditionEvaluator(IGameState gameState)
        {
            this.gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
        }

        public bool Evaluate(string condition)
        {
            if (string.IsNullOrWhiteSpace(condition))
            {
                return true;
            }

            string[] orGroups = condition.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string orGroup in orGroups)
            {
                if (EvaluateAndGroup(orGroup))
                {
                    return true;
                }
            }

            return false;
        }

        private bool EvaluateAndGroup(string andGroup)
        {
            string[] comparisons = andGroup.Split(new[] { "&&" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string comparison in comparisons)
            {
                if (!EvaluateComparison(comparison.Trim()))
                {
                    return false;
                }
            }

            return true;
        }

        private bool EvaluateComparison(string comparison)
        {
            foreach (string op in comparisonOperators)
            {
                int operatorIndex = comparison.IndexOf(op, StringComparison.Ordinal);
                if (operatorIndex < 0)
                {
                    continue;
                }

                float left = FormulaPreprocessor.Evaluate(gameState, comparison.Substring(0, operatorIndex));
                float right = FormulaPreprocessor.Evaluate(gameState, comparison.Substring(operatorIndex + op.Length));

                switch (op)
                {
                    case ">=": return left >= right;
                    case "<=": return left <= right;
                    case "==": return Mathf.Approximately(left, right);
                    case "!=": return !Mathf.Approximately(left, right);
                    case ">": return left > right;
                    case "<": return left < right;
                }
            }

            Debug.LogError($"[FormulaConditionEvaluator] 條件式缺少比較運算子：{comparison}");
            return false;
        }
    }
}
