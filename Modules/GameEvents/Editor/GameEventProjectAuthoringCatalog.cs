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
        private readonly Dictionary<string, IReadOnlyList<string>> argumentOptions;

        private GameEventProjectAuthoringCatalog(
            IReadOnlyList<ParameterDefinition> parameters,
            IReadOnlyList<EffectCommandDescriptor> commands,
            IReadOnlyList<string> triggerTimings,
            Dictionary<string, IReadOnlyList<string>> argumentOptions,
            IReadOnlyList<string> warnings)
        {
            Parameters = parameters;
            Commands = commands;
            TriggerTimings = triggerTimings;
            this.argumentOptions = argumentOptions;
            Warnings = warnings;
            parametersByKey = parameters.ToDictionary(item => item.Key, StringComparer.Ordinal);
            commandsByName = commands.ToDictionary(item => item.Name, StringComparer.Ordinal);
        }

        public IReadOnlyList<ParameterDefinition> Parameters { get; }
        public IReadOnlyList<EffectCommandDescriptor> Commands { get; }
        public IReadOnlyList<string> TriggerTimings { get; }
        public IReadOnlyList<string> Warnings { get; }

        public static GameEventProjectAuthoringCatalog Load(
            IReadOnlyList<TextAsset> selectedParameterTables,
            GameEventCatalogAsset eventCatalog,
            UnityEngine.Object dataCatalog)
        {
            if (selectedParameterTables == null)
            {
                throw new ArgumentNullException(nameof(selectedParameterTables));
            }

            ParameterTableJsonCodec parameterCodec = new ParameterTableJsonCodec();
            GameEventDocumentJsonCodec eventCodec = new GameEventDocumentJsonCodec();
            EffectRuntime effectRuntime = new EffectRuntime(new EffectCommandRegistry());
            Dictionary<string, ParameterDefinition> parameterMap =
                new Dictionary<string, ParameterDefinition>(StringComparer.Ordinal);
            SortedSet<string> timings = new SortedSet<string>(StringComparer.Ordinal);
            Dictionary<string, SortedSet<string>> argumentSets =
                new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
            List<string> warnings = new List<string>();

            for (int index = 0; index < selectedParameterTables.Count; index++)
            {
                TextAsset selectedTable = selectedParameterTables[index];
                if (selectedTable == null)
                {
                    warnings.Add($"Selected Parameter Table row {index + 1} is missing.");
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
                        warnings.Add($"Event Catalog row {index + 1} is missing.");
                        continue;
                    }

                    TryCollectGameEvent(
                        AssetDatabase.GetAssetPath(asset),
                        asset.text,
                        eventCodec,
                        effectRuntime,
                        timings,
                        argumentSets,
                        warnings);
                }
            }

            GameEventEditorDataSource source =
                GameEventEditorCommandCatalog.GetDataSource();
            if (source == null)
            {
                warnings.Add("No Game Event Editor data source is registered.");
            }
            else if (dataCatalog == null)
            {
                warnings.Add($"No {source.DisplayName} is selected.");
            }
            else
            {
                foreach (string timing in source.GetTriggerTimings(dataCatalog))
                {
                    if (!string.IsNullOrWhiteSpace(timing))
                    {
                        timings.Add(timing);
                    }
                }
            }

            IReadOnlyList<ParameterDefinition> parameters = parameterMap.Values
                .OrderBy(item => item.Key, StringComparer.Ordinal)
                .ToArray();
            IReadOnlyList<EffectCommandDescriptor> commands = source != null && dataCatalog != null
                ? SelectCommands(
                    GameEventEditorCommandCatalog.GetDescriptors(),
                    source.GetCommandNames(dataCatalog),
                    warnings)
                : Array.Empty<EffectCommandDescriptor>();
            Dictionary<string, IReadOnlyList<string>> options = argumentSets
                .ToDictionary(
                    item => item.Key,
                    item => (IReadOnlyList<string>)item.Value.ToArray(),
                    StringComparer.Ordinal);

            return new GameEventProjectAuthoringCatalog(
                parameters,
                commands,
                timings.ToArray(),
                options,
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
                    warnings.Add($"Data Catalog Command row {index + 1} is empty.");
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
                        $"Data Catalog Command '{commandName}' is not registered by the project.");
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

        public IReadOnlyList<string> GetArgumentOptions(string commandName, int argumentIndex)
        {
            return argumentOptions.TryGetValue(
                MakeArgumentKey(commandName, argumentIndex),
                out IReadOnlyList<string> values)
                ? values
                : Array.Empty<string>();
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
                            $"Parameter '{definition.Key}' has conflicting types in '{path}'.");
                        continue;
                    }

                    parameters[definition.Key] = definition;
                }
            }
            catch (Exception exception)
            {
                warnings.Add($"Cannot read Parameter Table '{path}': {exception.Message}");
            }
        }

        private static void TryCollectGameEvent(
            string path,
            string json,
            GameEventDocumentJsonCodec codec,
            EffectRuntime effectRuntime,
            SortedSet<string> timings,
            Dictionary<string, SortedSet<string>> argumentSets,
            List<string> warnings)
        {
            try
            {
                GameEventDocument document = codec.Read(json);
                if (!string.IsNullOrWhiteSpace(document.TriggerTiming))
                {
                    timings.Add(document.TriggerTiming);
                }

                EffectParseResult parsed = effectRuntime.Parse(document.Commands);
                if (!parsed.IsSuccess)
                {
                    warnings.Add(
                        $"Cannot index Commands in '{path}': {parsed.FormatDiagnostics()}");
                    return;
                }

                foreach (EffectTimingBlock block in parsed.Program.Blocks)
                {
                    foreach (EffectCommandCall command in block.Commands)
                    {
                        for (int argumentIndex = 0;
                             argumentIndex < command.Arguments.Count;
                             argumentIndex++)
                        {
                            string key = MakeArgumentKey(command.Name, argumentIndex);
                            if (!argumentSets.TryGetValue(key, out SortedSet<string> values))
                            {
                                values = new SortedSet<string>(StringComparer.Ordinal);
                                argumentSets.Add(key, values);
                            }

                            values.Add(command.Arguments[argumentIndex]);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                warnings.Add($"Cannot index Game Event '{path}': {exception.Message}");
            }
        }

        private static string MakeArgumentKey(string commandName, int argumentIndex)
        {
            return (commandName ?? string.Empty) + "\n" + argumentIndex;
        }
    }
}
