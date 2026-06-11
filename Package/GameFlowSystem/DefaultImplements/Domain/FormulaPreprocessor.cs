using System.Text.RegularExpressions;
using KahaGameCore.ValueContainer;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    /// <summary>
    /// 表格公式前處理：將企劃友善的 $Tag 寫法展開為 KahaGameCore.Calculator 的 Caster.Tag 寫法，
    /// 並提供以遊戲狀態為 Caster 的求值捷徑。
    /// </summary>
    public static class FormulaPreprocessor
    {
        private static readonly Regex tagPattern = new Regex(@"\$([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);

        public static string Expand(string formula)
        {
            if (string.IsNullOrEmpty(formula))
            {
                return formula;
            }

            return tagPattern.Replace(formula, "Caster.$1");
        }

        public static float Evaluate(IGameState gameState, string formula)
        {
            return Calculator.Calculate(new Calculator.CalculateData
            {
                caster = gameState.Container,
                formula = Expand(formula)
            });
        }

        public static int EvaluateInt(IGameState gameState, string formula)
        {
            return UnityEngine.Mathf.RoundToInt(Evaluate(gameState, formula));
        }
    }
}
