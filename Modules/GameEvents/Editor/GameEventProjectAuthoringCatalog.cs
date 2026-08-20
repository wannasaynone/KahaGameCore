using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.Effects;
using KahaGameCore.Parameters;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.GameEvents.Editor
{
    internal sealed class GameEventProjectAuthoringCatalog
    {
        private readonly Dictionary<string, ParameterDefinition> parametersByKey;
        private readonly Dictionary<string, EffectCommandDescriptor> commandsByName;

        private GameEventProjectAuthoringCatalog(
            IReadOnlyList<ParameterDefinition> parameters,
            IReadOnlyList<EffectCommandDescriptor> commands,
            IReadOnlyList<string> triggerTimings,
            IReadOnlyList<string> warnings)
        {
            Parameters = parameters;
            Commands = commands;
            TriggerTimings = triggerTimings;
            Warnings = warnings;
            parametersByKey = parameters.ToDictionary(item => item.Key, StringComparer.Ordinal);
            commandsByName = commands.ToDictionary(item => item.Name, StringComparer.Ordinal);
        }

        public IReadOnlyList<ParameterDefinition> Parameters { get; }
        public IReadOnlyList<EffectCommandDescriptor> Commands { get; }
        public IReadOnlyList<string> TriggerTimings { get; }
        public IReadOnlyList<string> Warnings { get; }

        public static GameEventProjectAuthoringCatalog Load(GameEventCatalogAsset eventCatalog)
        {
            ParameterTableJsonCodec parameterCodec = new ParameterTableJsonCodec();
            GameEventDocumentJsonCodec eventCodec = new GameEventDocumentJsonCodec();
            EffectRuntime effectRuntime = new EffectRuntime(new EffectCommandRegistry());
            Dictionary<string, ParameterDefinition> parameterMap =
                new Dictionary<string, ParameterDefinition>(StringComparer.Ordinal);
            List<string> warnings = new List<string>();

            IReadOnlyList<TextAsset> selectedParameterTables =
                eventCatalog?.ParameterTables ?? Array.Empty<TextAsset>();
            for (int index = 0; index < selectedParameterTables.Count; index++)
            {
                TextAsset selectedTable = selectedParameterTables[index];
                if (selectedTable == null)
                {
                    warnings.Add($"選取的第 {index + 1} 筆參數表已遺失。");
                    continue;
                }

                string selectedPath = AssetDatabase.GetAssetPath(selectedTable);
                TryCollectParameters(
                    selectedPath,
                    selectedTable.text,
                    parameterCodec,
                    parameterMap,
                    warnings);
            }

            if (eventCatalog != null)
            {
                for (int index = 0; index < eventCatalog.Files.Count; index++)
                {
                    TextAsset asset = eventCatalog.Files[index];
                    if (asset == null)
                    {
                        warnings.Add($"事件目錄中的第 {index + 1} 筆事件已遺失。");
                        continue;
                    }

                    TryValidateGameEvent(
                        AssetDatabase.GetAssetPath(asset),
                        asset.text,
                        eventCodec,
                        effectRuntime,
                        warnings);
                }
            }

            IReadOnlyList<ParameterDefinition> parameters = parameterMap.Values
                .OrderBy(item => item.Key, StringComparer.Ordinal)
                .ToArray();
            IReadOnlyList<EffectCommandDescriptor> commands = eventCatalog == null
                ? Array.Empty<EffectCommandDescriptor>()
                : SelectCommands(
                    EffectCommandAssemblyCatalog.GetDescriptors(
                        eventCatalog.CommandAssemblyNames,
                        warnings),
                    eventCatalog.EnabledCommandNames,
                    warnings);
            return new GameEventProjectAuthoringCatalog(
                parameters,
                commands,
                eventCatalog?.TriggerTimings ?? Array.Empty<string>(),
                warnings);
        }

        internal static IReadOnlyList<EffectCommandDescriptor> SelectCommands(
            IReadOnlyList<EffectCommandDescriptor> registeredCommands,
            IReadOnlyList<string> selectedNames,
            List<string> warnings)
        {
            Dictionary<string, EffectCommandDescriptor> registeredByName =
                registeredCommands.ToDictionary(command => command.Name, StringComparer.Ordinal);
            HashSet<string> selected = new HashSet<string>(StringComparer.Ordinal);
            List<EffectCommandDescriptor> commands = new List<EffectCommandDescriptor>();
            for (int index = 0; index < selectedNames.Count; index++)
            {
                string commandName = selectedNames[index];
                if (string.IsNullOrWhiteSpace(commandName))
                {
                    warnings.Add($"事件目錄中的第 {index + 1} 筆指令名稱為空白。");
                    continue;
                }

                if (!selected.Add(commandName))
                {
                    continue;
                }

                if (!registeredByName.TryGetValue(
                        commandName,
                        out EffectCommandDescriptor descriptor))
                {
                    warnings.Add(
                        $"事件目錄指令「{commandName}」不在所選 asmdef 範圍內。");
                    continue;
                }

                commands.Add(descriptor);
            }

            return commands
                .OrderBy(item => item.Category, StringComparer.Ordinal)
                .ThenBy(item => item.DisplayName, StringComparer.Ordinal)
                .ToArray();
        }

        public bool TryGetParameter(string key, out ParameterDefinition definition)
        {
            return parametersByKey.TryGetValue(key ?? string.Empty, out definition);
        }

        public bool TryGetCommand(string name, out EffectCommandDescriptor descriptor)
        {
            return commandsByName.TryGetValue(name ?? string.Empty, out descriptor);
        }

        private static void TryCollectParameters(
            string path,
            string json,
            ParameterTableJsonCodec codec,
            Dictionary<string, ParameterDefinition> parameters,
            List<string> warnings)
        {
            try
            {
                ParameterTable table = codec.Read(json);
                foreach (ParameterDefinition definition in table.Definitions)
                {
                    if (parameters.TryGetValue(definition.Key, out ParameterDefinition existing) &&
                        existing.Type != definition.Type)
                    {
                        warnings.Add(
                            $"參數「{definition.Key}」在「{path}」中存在類型衝突。");
                        continue;
                    }

                    parameters[definition.Key] = definition;
                }
            }
            catch (Exception exception)
            {
                warnings.Add($"無法讀取參數表「{path}」：{exception.Message}");
            }
        }

        private static void TryValidateGameEvent(
            string path,
            string json,
            GameEventDocumentJsonCodec codec,
            EffectRuntime effectRuntime,
            List<string> warnings)
        {
            try
            {
                GameEventDocument document = codec.Read(json);
                EffectParseResult parsed = effectRuntime.Parse(document.Commands);
                if (!parsed.IsSuccess)
                {
                    warnings.Add(
                        $"無法解析「{path}」中的指令：{parsed.FormatDiagnostics()}");
                    return;
                }
            }
            catch (Exception exception)
            {
                warnings.Add($"無法讀取遊戲事件「{path}」：{exception.Message}");
            }
        }

    }
}
