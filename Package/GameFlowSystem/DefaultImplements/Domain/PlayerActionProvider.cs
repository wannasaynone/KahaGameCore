using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.GameData.Implemented;
using KahaGameCore.Package.GameFlowSystem;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Data;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    /// <summary>提供目前地點與條件下可顯示的玩家行動清單。</summary>
    public interface IPlayerActionProvider : IGameFlowActionProvider
    {
        /// <summary>以具體表格型別覆蓋 IGameFlowActionProvider 的版本，供專案內部取得完整欄位。</summary>
        new IReadOnlyList<PlayerActionData> GetVisibleActions(int locationId);
        bool IsEnabled(PlayerActionData action);
    }

    public class PlayerActionProvider : IPlayerActionProvider
    {
        private readonly IConditionEvaluator conditionEvaluator;
        private readonly List<PlayerActionData> actions;

        public PlayerActionProvider(GameStaticDataManager staticDataManager, IConditionEvaluator conditionEvaluator)
        {
            this.conditionEvaluator = conditionEvaluator ?? throw new ArgumentNullException(nameof(conditionEvaluator));

            PlayerActionData[] loadedActions = staticDataManager.GetAllGameData<PlayerActionData>();
            actions = loadedActions == null
                ? new List<PlayerActionData>()
                : loadedActions.OrderBy(action => action.SortOrder).ToList();
        }

        public IReadOnlyList<PlayerActionData> GetVisibleActions(int locationId)
        {
            return actions
                .Where(action => IsAvailableAtLocation(action, locationId))
                .Where(action => conditionEvaluator.Evaluate(action.VisibleCondition))
                .ToList();
        }

        public bool IsEnabled(PlayerActionData action)
        {
            return conditionEvaluator.Evaluate(action.EnableCondition);
        }

        IReadOnlyList<IGameFlowAction> IGameFlowActionProvider.GetVisibleActions(int locationId)
        {
            return GetVisibleActions(locationId);
        }

        bool IGameFlowActionProvider.IsEnabled(IGameFlowAction action)
        {
            return IsEnabled((PlayerActionData)action);
        }

        private static bool IsAvailableAtLocation(PlayerActionData action, int locationId)
        {
            if (string.IsNullOrWhiteSpace(action.Locations))
            {
                return true;
            }

            string[] locationIds = action.Locations.Split(',');
            foreach (string idText in locationIds)
            {
                if (int.TryParse(idText.Trim(), out int id) && id == locationId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
