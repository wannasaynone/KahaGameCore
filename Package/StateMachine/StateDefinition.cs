using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Assets.Scripts.StateMachine
{
    [CreateAssetMenu(fileName = "NewState", menuName = "StateMachine/State")]
    public class StateDefinition : ScriptableObject
    {
        public string stateID;
        public string stateName;

        public Vector2 editorPosition;

        public List<TransitionDefinition> transitions = new List<TransitionDefinition>();

        // 新增：轉換排序結構
        [System.Serializable]
        public class TransitionOrder
        {
            public TransitionDefinition transition;
            public int priority = 0; // 數字越小優先級越高
            public bool enabled = true;
        }

        public List<TransitionOrder> orderedTransitions = new List<TransitionOrder>();
        public bool useOrderedEvaluation = true; // 是否使用排序評估
        public bool stopOnFirstSuccess = true; // 第一個成功時是否停止檢查

        public float stateTimer = 0f;

        public StateBehaviourDefinition enterBehaviour;
        public StateBehaviourDefinition exitBehaviour;
        public StateBehaviourDefinition updateBehaviour;

        /// <summary>
        /// 獲取按優先級排序的轉換列表
        /// </summary>
        public List<TransitionDefinition> GetOrderedTransitions()
        {
            if (useOrderedEvaluation && orderedTransitions.Count > 0)
            {
                // 同步排序列表
                SyncOrderedTransitions();

                // 返回啟用的轉換，按優先級排序
                return orderedTransitions
                    .Where(ot => ot.enabled && ot.transition != null)
                    .OrderBy(ot => ot.priority)
                    .Select(ot => ot.transition)
                    .ToList();
            }
            else
            {
                // 使用原始順序
                return transitions;
            }
        }

        /// <summary>
        /// 同步排序轉換列表與主轉換列表
        /// </summary>
        public void SyncOrderedTransitions()
        {
            // 移除不存在於主列表中的轉換
            orderedTransitions.RemoveAll(ot => ot.transition == null || !transitions.Contains(ot.transition));

            // 添加新的轉換到排序列表
            foreach (var transition in transitions)
            {
                if (transition != null && !orderedTransitions.Any(ot => ot.transition == transition))
                {
                    orderedTransitions.Add(new TransitionOrder
                    {
                        transition = transition,
                        priority = orderedTransitions.Count,
                        enabled = true
                    });
                }
            }
        }

        /// <summary>
        /// 向上移動轉換優先級
        /// </summary>
        public void MoveTransitionUp(TransitionDefinition transition)
        {
            var orderedTransition = orderedTransitions.FirstOrDefault(ot => ot.transition == transition);
            if (orderedTransition != null && orderedTransition.priority > 0)
            {
                // 找到優先級更高的轉換並交換
                var higherPriorityTransition = orderedTransitions
                    .Where(ot => ot.priority < orderedTransition.priority)
                    .OrderByDescending(ot => ot.priority)
                    .FirstOrDefault();

                if (higherPriorityTransition != null)
                {
                    int tempPriority = orderedTransition.priority;
                    orderedTransition.priority = higherPriorityTransition.priority;
                    higherPriorityTransition.priority = tempPriority;
                }
            }
        }

        /// <summary>
        /// 向下移動轉換優先級
        /// </summary>
        public void MoveTransitionDown(TransitionDefinition transition)
        {
            var orderedTransition = orderedTransitions.FirstOrDefault(ot => ot.transition == transition);
            if (orderedTransition != null)
            {
                // 找到優先級更低的轉換並交換
                var lowerPriorityTransition = orderedTransitions
                    .Where(ot => ot.priority > orderedTransition.priority)
                    .OrderBy(ot => ot.priority)
                    .FirstOrDefault();

                if (lowerPriorityTransition != null)
                {
                    int tempPriority = orderedTransition.priority;
                    orderedTransition.priority = lowerPriorityTransition.priority;
                    lowerPriorityTransition.priority = tempPriority;
                }
            }
        }

        /// <summary>
        /// 設置轉換的啟用狀態
        /// </summary>
        public void SetTransitionEnabled(TransitionDefinition transition, bool enabled)
        {
            var orderedTransition = orderedTransitions.FirstOrDefault(ot => ot.transition == transition);
            if (orderedTransition != null)
            {
                orderedTransition.enabled = enabled;
            }
        }

        /// <summary>
        /// 獲取轉換的優先級
        /// </summary>
        public int GetTransitionPriority(TransitionDefinition transition)
        {
            var orderedTransition = orderedTransitions.FirstOrDefault(ot => ot.transition == transition);
            return orderedTransition?.priority ?? -1;
        }

        /// <summary>
        /// 檢查轉換是否啟用
        /// </summary>
        public bool IsTransitionEnabled(TransitionDefinition transition)
        {
            var orderedTransition = orderedTransitions.FirstOrDefault(ot => ot.transition == transition);
            return orderedTransition?.enabled ?? true;
        }
    }
}
