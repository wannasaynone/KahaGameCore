using System.Collections.Generic;
using KahaGameCore.StaticData;
using KahaGameCore.GameEvent;
using KahaGameCore.ValueContainer;
using KahaGameCore.GameFlowSystem.DefaultImplements.Data;
using KahaGameCore.GameFlowSystem.DefaultImplements.Events;
using UnityEngine;

namespace KahaGameCore.GameFlowSystem.DefaultImplements
{
    public class GameState : IGameState
    {
        private IValueContainer container = new GameValueContainer();
        private readonly Dictionary<string, GameValueData> tagToDefinition = new Dictionary<string, GameValueData>();
        private readonly HashSet<string> knownTags = new HashSet<string>();

        public GameState(GameStaticDataManager staticDataManager)
        {
            GameValueData[] definitions = staticDataManager.GetAllGameData<GameValueData>();
            if (definitions == null)
            {
                Debug.LogError("[GameState] GameValueData 表未載入，數值將不會被鉗制。");
                return;
            }

            foreach (GameValueData definition in definitions)
            {
                tagToDefinition[definition.Tag] = definition;
                knownTags.Add(definition.Tag);
            }
        }

        public int Get(string tag)
        {
            return container.GetTotal(tag, baseOnly: false);
        }

        public bool TryGet(string tag, out int value)
        {
            value = Get(tag);
            return knownTags.Contains(tag);
        }

        public void Add(string tag, int amount)
        {
            Set(tag, Get(tag) + amount);
        }

        public void Set(string tag, int value)
        {
            knownTags.Add(tag);
            int clamped = Clamp(tag, value);
            if (clamped == Get(tag))
            {
                return;
            }

            container.SetBase(tag, clamped);
            EventBus.Publish(new GameValueChangedEvent(tag, clamped));
        }

        public void ResetToInitial()
        {
            // 重建容器以一併清除動態旗標（EventTriggerCount_x、LocationUnlocked_x 等），
            // 確保返回標題後重新開始是乾淨的狀態。
            container = new GameValueContainer();
            knownTags.Clear();

            foreach (GameValueData definition in tagToDefinition.Values)
            {
                container.SetBase(definition.Tag, definition.InitialValue);
                knownTags.Add(definition.Tag);
                EventBus.Publish(new GameValueChangedEvent(definition.Tag, definition.InitialValue));
            }
        }

        private int Clamp(string tag, int value)
        {
            if (tagToDefinition.TryGetValue(tag, out GameValueData definition))
            {
                return Mathf.Clamp(value, definition.MinValue, definition.MaxValue);
            }

            // 表上未定義的標籤（旗標、事件完成記錄等）不做鉗制。
            return value;
        }
    }
}
