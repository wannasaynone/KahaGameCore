using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.Effects;
using KahaGameCore.Parameters;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.GameEvents.Editor
{
    internal sealed class ParameterAuthoringEntry
    {
        public ParameterAuthoringEntry(
            string tableGuid,
            string tableDisplayName,
            string assetPath,
            ParameterDefinition definition)
        {
            TableGuid = tableGuid ?? throw new ArgumentNullException(nameof(tableGuid));
            TableDisplayName = tableDisplayName ??
                throw new ArgumentNullException(nameof(tableDisplayName));
            AssetPath = assetPath ?? throw new ArgumentNullException(nameof(assetPath));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public string TableGuid { get; }
        public string TableDisplayName { get; }
        public string AssetPath { get; }
        public ParameterDefinition Definition { get; }
    }

    internal sealed class GameEventProjectAuthoringCatalog
    {
        private readonly Dictionary<string, ParameterDefinition> parametersByKey;
        private readonly Dictionary<string, ParameterAuthoringEntry> parameterEntriesByKey;
        private readonly Dictionary<string, EffectCommandDescriptor> commandsByName;

        private GameEventProjectAuthoringCatalog(
            IReadOnlyList<ParameterAuthoringEntry> parameterEntries,
            IReadOnlyList<EffectCommandDescriptor> commands,
            IReadOnlyList<string> triggerTimings,
            IReadOnlyList<string> warnings,
            IReadOnlyList<string> errors)
        {
            ParameterEntries = parameterEntries;
            Parameters = parameterEntries
                .Select(entry => entry.Definition)
                .ToArray();
            Commands = commands;
            TriggerTimings = triggerTimings;
            Warnings = warnings;
            Errors = errors;
            parametersByKey = Parameters.ToDictionary(item => item.Key, StringComparer.Ordinal);
            parameterEntriesByKey = parameterEntries.ToDictionary(
                item => item.Definition.Key,
                StringComparer.Ordinal);
            commandsByName = commands.ToDictionary(item => item.Name, StringComparer.Ordinal);
        }

        public IReadOnlyList<ParameterAuthoringEntry> ParameterEntries { get; }
        public IReadOnlyList<ParameterDefinition> Parameters { get; }
        public IReadOnlyList<EffectCommandDescriptor> Commands { get; }
        public IReadOnlyList<string> TriggerTimings { get; }
        public IReadOnlyList<string> Warnings { get; }
        public IReadOnlyList<string> Errors { get; }

        public static GameEventProjectAuthoringCatalog Load(GameEventCatalogAsset eventCatalog)
        {
            return Load(eventCatalog, null, null);
        }

        internal static GameEventProjectAuthoringCatalog Load(
            GameEventCatalogAsset eventCatalog,
            IReadOnlyList<ParameterAuthoringEntry> parameterEntryOverride,
            IReadOnlyList<string> parameterErrors)
        {
            ParameterTableJsonCodec parameterCodec = new ParameterTableJsonCodec();
            GameEventDocumentJsonCodec eventCodec = new GameEventDocumentJsonCodec();
            EffectRuntime effectRuntime = new EffectRuntime(new EffectCommandRegistry());
            Dictionary<string, ParameterAuthoringEntry> parameterMap =
                new Dictionary<string, ParameterAuthoringEntry>(StringComparer.Ordinal);
            List<string> warnings = new List<string>();
            List<string> errors = new List<string>();

            if (parameterEntryOverride == null)
            {
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
                        warnings,
                        errors);
                }
            }
            else
            {
                foreach (ParameterAuthoringEntry entry in parameterEntryOverride)
                {
                    parameterMap[entry.Definition.Key] = entry;
                }

                if (parameterErrors != null)
                {
                    errors.AddRange(parameterErrors);
                }
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

            IReadOnlyList<ParameterAuthoringEntry> parameterEntries = parameterMap.Values
                .OrderBy(item => item.TableDisplayName, StringComparer.Ordinal)
                .ThenBy(item => item.Definition.Key, StringComparer.Ordinal)
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
                parameterEntries,
                commands,
                eventCatalog?.TriggerTimings ?? Array.Empty<string>(),
                warnings,
                errors);
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

        public bool TryGetParameterEntry(
            string key,
            out ParameterAuthoringEntry entry)
        {
            return parameterEntriesByKey.TryGetValue(key ?? string.Empty, out entry);
        }

        public bool TryGetCommand(string name, out EffectCommandDescriptor descriptor)
        {
            return commandsByName.TryGetValue(name ?? string.Empty, out descriptor);
        }

        private static void TryCollectParameters(
            string path,
            string json,
            ParameterTableJsonCodec codec,
            Dictionary<string, ParameterAuthoringEntry> parameters,
            List<string> warnings,
            List<string> errors)
        {
            try
            {
                ParameterTable table = codec.Read(json);
                foreach (ParameterDefinition definition in table.Definitions)
                {
                    if (parameters.TryGetValue(
                            definition.Key,
                            out ParameterAuthoringEntry existing))
                    {
                        errors.Add(
                            $"參數鍵「{definition.Key}」同時存在於「" +
                            $"{existing.AssetPath}」與「{path}」。參數鍵必須跨表唯一。");
                        continue;
                    }

                    parameters.Add(
                        definition.Key,
                        new ParameterAuthoringEntry(
                            table.TableGuid,
                            table.DisplayName,
                            path,
                            definition));
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
