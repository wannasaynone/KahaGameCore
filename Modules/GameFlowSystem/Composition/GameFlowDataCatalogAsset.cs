using System;
using System.Collections.Generic;
using KahaGameCore.GameEvents;
using UnityEngine;

namespace KahaGameCore.GameFlowSystem.Composition
{
    /// <summary>
    /// 預設 GameFlow 組裝與 Game Event authoring 共用的專案資料入口。
    /// 只保存來源資產引用與允許使用的 Command 名稱，不複製或快取表格內容。
    /// 不使用預設 GameFlow／Game Event Editor 的專案不需要此資產。
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameFlowDataCatalog",
        menuName = "Kaha Game Core/GameFlow/Data Catalog")]
    public sealed class GameFlowDataCatalogAsset : ScriptableObject
    {
        [Header("GameFlow Tables")]
        [SerializeField] private TextAsset timePhaseData;
        [SerializeField] private TextAsset playerActionData;
        [SerializeField] private TextAsset locationData;
        [SerializeField] private TextAsset gameTextData;
        [SerializeField] private TextAsset dialogueData;

        [Header("Parameters and Events")]
        [SerializeField] private List<TextAsset> parameterTables = new List<TextAsset>();
        [SerializeField] private GameEventCatalogAsset gameEventCatalog;
        [SerializeField] private List<string> gameEventCommands = new List<string>();

        public TextAsset TimePhaseData => timePhaseData;
        public TextAsset PlayerActionData => playerActionData;
        public TextAsset LocationData => locationData;
        public TextAsset GameTextData => gameTextData;
        public TextAsset DialogueData => dialogueData;
        public IReadOnlyList<TextAsset> ParameterTables => parameterTables;
        public GameEventCatalogAsset GameEventCatalog => gameEventCatalog;
        public IReadOnlyList<string> GameEventCommands =>
            gameEventCommands ?? (IReadOnlyList<string>)Array.Empty<string>();

        public TextAsset[] GetGameDataTables()
        {
            return new[]
            {
                timePhaseData,
                playerActionData,
                locationData,
                gameTextData,
                dialogueData
            };
        }

        public void SetGameDataTables(
            TextAsset timePhases,
            TextAsset playerActions,
            TextAsset locations,
            TextAsset gameTexts,
            TextAsset dialogues)
        {
            timePhaseData = timePhases;
            playerActionData = playerActions;
            locationData = locations;
            gameTextData = gameTexts;
            dialogueData = dialogues;
        }

        public void SetParameterTables(IEnumerable<TextAsset> tables)
        {
            if (tables == null) throw new ArgumentNullException(nameof(tables));
            parameterTables = new List<TextAsset>(tables);
        }

        public void SetGameEventCatalog(GameEventCatalogAsset catalog)
        {
            gameEventCatalog = catalog;
        }

        public void SetGameEventCommands(IEnumerable<string> commandNames)
        {
            if (commandNames == null) throw new ArgumentNullException(nameof(commandNames));
            gameEventCommands = new List<string>(commandNames);
        }

        public void ValidateRequiredReferences()
        {
            ValidateTable(timePhaseData, "TimePhaseData");
            ValidateTable(playerActionData, "PlayerActionData");
            ValidateTable(locationData, "LocationData");
            ValidateTable(gameTextData, "GameTextData");
            ValidateTable(dialogueData, "DialogueData");

            if (parameterTables == null || parameterTables.Count == 0)
            {
                throw new InvalidOperationException(
                    $"[{name}] At least one Parameter Table is required.");
            }

            for (int index = 0; index < parameterTables.Count; index++)
            {
                if (parameterTables[index] == null)
                {
                    throw new InvalidOperationException(
                        $"[{name}] Parameter Tables row {index + 1} is missing.");
                }
            }

            if (gameEventCatalog == null)
            {
                throw new InvalidOperationException(
                    $"[{name}] Game Event Catalog is required.");
            }
        }

        private void ValidateTable(TextAsset table, string expectedName)
        {
            if (table == null)
            {
                throw new InvalidOperationException(
                    $"[{name}] {expectedName} table is required.");
            }

            if (!string.Equals(table.name, expectedName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"[{name}] {expectedName} must reference a TextAsset named " +
                    $"'{expectedName}', but got '{table.name}'.");
            }
        }
    }
}
