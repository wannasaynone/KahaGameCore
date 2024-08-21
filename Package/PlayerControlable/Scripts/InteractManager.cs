using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.Package.PlayerControlable
{
    public class InteractManager
    {
        private readonly InteractData[] sourceDatas;

        private class TargetTagToInteractData
        {
            public string targetTag;
            public List<ActionTypeToInteractData> actionTypeToInteractDatas = new List<ActionTypeToInteractData>();
        }

        private class ActionTypeToInteractData
        {
            public string actionType;
            public List<InteractData> interactDatas = new List<InteractData>();
        }

        private List<TargetTagToInteractData> targetTagToInteractDatas = new List<TargetTagToInteractData>();

        public InteractManager(InteractData[] interactDatas)
        {
            sourceDatas = interactDatas;

            for (int i = 0; i < sourceDatas.Length; i++)
            {
                InteractData interactData = sourceDatas[i];

                TargetTagToInteractData targetTagToInteractData = targetTagToInteractDatas.Find(x => x.targetTag == interactData.InteractTargetTag);
                if (targetTagToInteractData == null)
                {
                    targetTagToInteractData = new TargetTagToInteractData
                    {
                        targetTag = interactData.InteractTargetTag
                    };
                    targetTagToInteractDatas.Add(targetTagToInteractData);
                }

                ActionTypeToInteractData actionTypeToInteractData = targetTagToInteractData.actionTypeToInteractDatas.Find(x => x.actionType == interactData.ActionType);
                if (actionTypeToInteractData == null)
                {
                    actionTypeToInteractData = new ActionTypeToInteractData
                    {
                        actionType = interactData.ActionType
                    };
                    targetTagToInteractData.actionTypeToInteractDatas.Add(actionTypeToInteractData);
                }

                actionTypeToInteractData.interactDatas.Add(interactData);
            }
        }

        public string[] GetAllActionType(string interactTargetTag)
        {
            List<string> actionTypes = new List<string>();
            for (int i = 0; i < targetTagToInteractDatas.Count; i++)
            {
                TargetTagToInteractData targetTagToInteractData = targetTagToInteractDatas[i];
                if (targetTagToInteractData.targetTag == interactTargetTag)
                {
                    for (int j = 0; j < targetTagToInteractData.actionTypeToInteractDatas.Count; j++)
                    {
                        actionTypes.Add(targetTagToInteractData.actionTypeToInteractDatas[j].actionType);
                    }
                    break;
                }
            }
            return actionTypes.ToArray();
        }

        public string Interact(string interactTargetTag, string actionType, Actor.IActor actor, int day, int time)
        {
            if (sourceDatas == null)
            {
                Debug.LogError("InteractDatas is not initialized.");
                return "";
            }

            TargetTagToInteractData targetTagToInteractData = targetTagToInteractDatas.Find(x => x.targetTag == interactTargetTag);
            if (targetTagToInteractData == null)
            {
                return "";
            }

            List<InteractData> matchedInteractDatas = new List<InteractData>();
            ActionTypeToInteractData actionTypeToInteractData = targetTagToInteractData.actionTypeToInteractDatas.Find(x => x.actionType == actionType);
            for (int i = 0; i < actionTypeToInteractData.interactDatas.Count; i++)
            {
                InteractData interactData = actionTypeToInteractData.interactDatas[i];
                if (IsTriggerableInteractData(interactData, actor, day, time))
                {
                    matchedInteractDatas.Add(interactData);
                }
            }

            if (matchedInteractDatas.Count == 0)
            {
                return "";
            }

            matchedInteractDatas.Sort((x, y) => x.ID.CompareTo(y.ID));
            return matchedInteractDatas[0].ReturnValueString;
        }

        private class ValueComparer
        {
            public string valueName;
            public CompareType compareType;
            public int compareValue;

            public enum CompareType
            {
                Equal,
                Greater,
                Less,
            }
        }

        private bool IsTriggerableInteractData(InteractData interactData, Actor.IActor actor, int day, int time)
        {
            string[] dayArray = string.IsNullOrEmpty(interactData.RequireDayArrayString) ? new string[0] : interactData.RequireDayArrayString.Split(';');
            string[] timeArray = string.IsNullOrEmpty(interactData.RequireTimeArrayString) ? new string[0] : interactData.RequireTimeArrayString.Split(';');
            string[] valueArray = string.IsNullOrEmpty(interactData.RquireValueArrayString) ? new string[0] : interactData.RquireValueArrayString.Split(';');

            bool isAnyDayMatched = dayArray.Length == 0;
            for (int i = 0; i < dayArray.Length; i++)
            {
                if (dayArray[i] == day.ToString())
                {
                    isAnyDayMatched = true;
                    break;
                }
            }

            if (!isAnyDayMatched)
            {
                return false;
            }

            bool isAnyTimeMatched = timeArray.Length == 0;
            for (int i = 0; i < timeArray.Length; i++)
            {
                if (timeArray[i].Contains("-"))
                {
                    string[] timeRange = timeArray[i].Split('-');
                    int start = int.Parse(timeRange[0]);
                    int end = int.Parse(timeRange[1]);
                    if (time >= start && time <= end)
                    {
                        isAnyTimeMatched = true;
                        break;
                    }
                }
                else
                {
                    if (timeArray[i] == time.ToString())
                    {
                        isAnyTimeMatched = true;
                        break;
                    }
                }
            }

            if (!isAnyTimeMatched)
            {
                return false;
            }

            if (valueArray.Length == 0)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(interactData.RquireValueArrayString) && actor == null)
            {
                return false;
            }

            List<ValueComparer> valueComparers = new List<ValueComparer>();
            for (int i = 0; i < valueArray.Length; i++)
            {
                ValueComparer valueComparer = new ValueComparer();

                if (valueArray[i].Contains(">"))
                {
                    valueComparer.compareType = ValueComparer.CompareType.Greater;
                    string[] split = valueArray[i].Split('>');
                    valueComparer.valueName = split[0];
                    valueComparer.compareValue = int.Parse(split[1]);
                }
                else if (valueArray[i].Contains("<"))
                {
                    valueComparer.compareType = ValueComparer.CompareType.Less;
                    string[] split = valueArray[i].Split('<');
                    valueComparer.valueName = split[0];
                    valueComparer.compareValue = int.Parse(split[1]);
                }
                else
                {
                    valueComparer.compareType = ValueComparer.CompareType.Equal;
                    string[] split = valueArray[i].Split('=');
                    valueComparer.valueName = split[0];
                    valueComparer.compareValue = int.Parse(split[1]);
                }

                valueComparers.Add(valueComparer);
            }

            for (int i = 0; i < valueComparers.Count; i++)
            {
                ValueComparer valueComparer = valueComparers[i];
                int value = actor.Stats.GetTotal(valueComparer.valueName, false);
                switch (valueComparer.compareType)
                {
                    case ValueComparer.CompareType.Equal:
                        if (value != valueComparer.compareValue)
                        {
                            return false;
                        }
                        break;
                    case ValueComparer.CompareType.Greater:
                        if (value <= valueComparer.compareValue)
                        {
                            return false;
                        }
                        break;
                    case ValueComparer.CompareType.Less:
                        if (value >= valueComparer.compareValue)
                        {
                            return false;
                        }
                        break;
                }
            }

            return true;
        }
    }
}