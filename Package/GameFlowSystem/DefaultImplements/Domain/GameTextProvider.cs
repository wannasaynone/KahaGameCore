using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.GameData.Implemented;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Data;
using UnityEngine;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    public class GameTextProvider : IGameTextProvider
    {
        private readonly IConditionEvaluator conditionEvaluator;
        private readonly List<GameTextData> texts;

        public GameTextProvider(GameStaticDataManager staticDataManager, IConditionEvaluator conditionEvaluator)
        {
            this.conditionEvaluator = conditionEvaluator ?? throw new ArgumentNullException(nameof(conditionEvaluator));

            GameTextData[] loadedTexts = staticDataManager.GetAllGameData<GameTextData>();
            texts = loadedTexts == null ? new List<GameTextData>() : loadedTexts.ToList();
        }

        public string GetText(int textId)
        {
            GameTextData text = texts.Find(data => data.ID == textId);
            if (text == null)
            {
                Debug.LogError($"[GameTextProvider] 找不到文字 ID={textId}。");
                return $"<missing text {textId}>";
            }

            return text.Text;
        }

        public GameTextData PickRandom(string group)
        {
            List<GameTextData> candidates = texts
                .Where(data => data.Group == group)
                .Where(data => conditionEvaluator.Evaluate(data.Condition))
                .ToList();

            if (candidates.Count == 0)
            {
                return null;
            }

            int totalWeight = candidates.Sum(data => Mathf.Max(1, data.Weight));
            int roll = UnityEngine.Random.Range(0, totalWeight);
            foreach (GameTextData candidate in candidates)
            {
                roll -= Mathf.Max(1, candidate.Weight);
                if (roll < 0)
                {
                    return candidate;
                }
            }

            return candidates[candidates.Count - 1];
        }
    }
}
