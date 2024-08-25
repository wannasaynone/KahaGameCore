using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.Package.PlayerControlable
{
    public class InteractManager
    {
        public static InteractManager Instance
        {
            get
            {
                if (instance == null)
                {
                    Debug.LogError("InteractManager is not initialized. Please call InteractManager.Initialize() first.");
                    return null;
                }
                return instance;
            }
        }
        private static InteractManager instance;

        private InteractManager() { }

        private static InteractData[] sourceDatas;

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

        public static void Initialize(InteractData[] interactDatas)
        {
            if (instance != null)
            {
                Debug.LogError("InteractManager is already initialized.");
                return;
            }

            instance = new InteractManager();

            sourceDatas = interactDatas;

            for (int i = 0; i < sourceDatas.Length; i++)
            {
                InteractData interactData = sourceDatas[i];

                TargetTagToInteractData targetTagToInteractData = instance.targetTagToInteractDatas.Find(x => x.targetTag == interactData.InteractTargetTag);
                if (targetTagToInteractData == null)
                {
                    targetTagToInteractData = new TargetTagToInteractData
                    {
                        targetTag = interactData.InteractTargetTag
                    };
                    instance.targetTagToInteractDatas.Add(targetTagToInteractData);
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

        private bool IsTriggerableInteractData(InteractData interactData, Actor.IActor actor, int day, int hour, int minute = 0)
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

                    if (timeRange.Length != 2)
                    {
                        Debug.LogError("Invalid time range format: " + timeArray[i]);
                        return false;
                    }

                    string[] startTime = timeRange[0].Split(':');
                    string[] endTime = timeRange[1].Split(':');

                    if (startTime.Length != 2)
                    {
                        startTime = new string[] { timeRange[0], "0" };
                    }
                    if (endTime.Length != 2)
                    {
                        endTime = new string[] { timeRange[1], "0" };
                    }

                    int startHour = int.Parse(startTime[0]);
                    int startMinute = int.Parse(startTime[1]);
                    int endHour = int.Parse(endTime[0]);
                    int endMinute = int.Parse(endTime[1]);

                    if (endHour < startHour)
                    {
                        isAnyTimeMatched = IsMatchTime(startHour, startMinute, 24, 0, hour, minute) || IsMatchTime(0, 0, endHour, endMinute, hour, minute);
                    }
                    else
                    {
                        isAnyTimeMatched = IsMatchTime(startHour, startMinute, endHour, endMinute, hour, minute);
                    }

                    if (isAnyTimeMatched)
                    {
                        break;
                    }
                }
                else
                {
                    int time = int.Parse(timeArray[i]);
                    isAnyTimeMatched = time == hour;
                    if (isAnyTimeMatched)
                    {
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

        private bool IsMatchTime(int startHour, int startMinute, int endHour, int endMinute, int hour, int minute)
        {
            if (hour >= startHour && hour <= endHour)
            {
                return true;
            }

            else if (hour == startHour && minute >= startMinute)
            {
                return true;
            }
            else if (hour == endHour && minute <= endMinute)
            {
                return true;
            }

            return false;
        }
    }
}