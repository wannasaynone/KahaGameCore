using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.Effects;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.GameEvents.Editor
{
    internal sealed class GameEventParameterReference
    {
        public GameEventParameterReference(
            TextAsset eventAsset,
            string assetPath,
            string eventDisplayName,
            bool usedInCondition,
            IReadOnlyList<string> commandNames)
        {
            EventAsset = eventAsset;
            AssetPath = assetPath ?? string.Empty;
            EventDisplayName = eventDisplayName ?? string.Empty;
            UsedInCondition = usedInCondition;
            CommandNames = commandNames ?? Array.Empty<string>();
        }

        public TextAsset EventAsset { get; }
        public string AssetPath { get; }
        public string EventDisplayName { get; }
        public bool UsedInCondition { get; }
        public IReadOnlyList<string> CommandNames { get; }

        public string FormatUsage()
        {
            List<string> usages = new List<string>();
            if (UsedInCondition)
            {
                usages.Add("條件");
            }

            usages.AddRange(CommandNames.Select(name => "指令：" + name));
            return string.Join("、", usages);
        }
    }

    internal sealed class OpenGameEventUsageDocument
    {
        public OpenGameEventUsageDocument(
            TextAsset eventAsset,
            string assetPath,
            string displayName,
            string condition,
            string commands)
        {
            EventAsset = eventAsset;
            AssetPath = assetPath ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Condition = condition ?? string.Empty;
            Commands = commands ?? string.Empty;
        }

        public TextAsset EventAsset { get; }
        public string AssetPath { get; }
        public string DisplayName { get; }
        public string Condition { get; }
        public string Commands { get; }
    }

    internal sealed class GameEventParameterUsageIndex
    {
        private sealed class ReferenceBuilder
        {
            public TextAsset EventAsset;
            public string AssetPath;
            public string EventDisplayName;
            public bool UsedInCondition;
            public readonly HashSet<string> CommandNames =
                new HashSet<string>(StringComparer.Ordinal);

            public GameEventParameterReference Build()
            {
                return new GameEventParameterReference(
                    EventAsset,
                    AssetPath,
                    EventDisplayName,
                    UsedInCondition,
                    CommandNames.OrderBy(name => name, StringComparer.Ordinal).ToArray());
            }
        }

        private readonly Dictionary<string, IReadOnlyList<GameEventParameterReference>>
            referencesByKey;

        private GameEventParameterUsageIndex(
            Dictionary<string, IReadOnlyList<GameEventParameterReference>> referencesByKey,
            IReadOnlyList<string> warnings)
        {
            this.referencesByKey = referencesByKey;
            Warnings = warnings;
        }

        public IReadOnlyList<string> Warnings { get; }

        public IReadOnlyList<GameEventParameterReference> Find(string parameterKey)
        {
            return referencesByKey.TryGetValue(
                    parameterKey ?? string.Empty,
                    out IReadOnlyList<GameEventParameterReference> references)
                ? references
                : Array.Empty<GameEventParameterReference>();
        }

        public static GameEventParameterUsageIndex Build(
            GameEventCatalogAsset eventCatalog,
            IReadOnlyList<EffectCommandDescriptor> commandDescriptors,
            OpenGameEventUsageDocument openDocument = null)
        {
            Dictionary<string, EffectCommandDescriptor> commandsByName =
                (commandDescriptors ?? Array.Empty<EffectCommandDescriptor>())
                .ToDictionary(descriptor => descriptor.Name, StringComparer.Ordinal);
            Dictionary<string, List<ReferenceBuilder>> buildersByKey =
                new Dictionary<string, List<ReferenceBuilder>>(StringComparer.Ordinal);
            List<string> warnings = new List<string>();

            IReadOnlyList<TextAsset> eventAssets =
                eventCatalog?.Files ?? Array.Empty<TextAsset>();
            foreach (TextAsset eventAsset in eventAssets)
            {
                if (eventAsset == null)
                {
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath(eventAsset);
                if (openDocument != null && SamePath(assetPath, openDocument.AssetPath))
                {
                    continue;
                }

                try
                {
                    GameEventDocument document =
                        new GameEventDocumentJsonCodec().Read(eventAsset.text);
                    CollectDocument(
                        eventAsset,
                        assetPath,
                        document.DisplayName,
                        document.Condition,
                        document.Commands,
                        commandsByName,
                        buildersByKey,
                        warnings);
                }
                catch (Exception exception)
                {
                    warnings.Add($"無法分析事件引用「{assetPath}」：{exception.Message}");
                }
            }

            if (openDocument != null)
            {
                CollectDocument(
                    openDocument.EventAsset,
                    openDocument.AssetPath,
                    openDocument.DisplayName,
                    openDocument.Condition,
                    openDocument.Commands,
                    commandsByName,
                    buildersByKey,
                    warnings);
            }

            Dictionary<string, IReadOnlyList<GameEventParameterReference>> references =
                buildersByKey.ToDictionary(
                    pair => pair.Key,
                    pair => (IReadOnlyList<GameEventParameterReference>)pair.Value
                        .Select(builder => builder.Build())
                        .ToArray(),
                    StringComparer.Ordinal);
            return new GameEventParameterUsageIndex(references, warnings);
        }

        private static void CollectDocument(
            TextAsset eventAsset,
            string assetPath,
            string displayName,
            string condition,
            string commandsSource,
            IReadOnlyDictionary<string, EffectCommandDescriptor> commandsByName,
            Dictionary<string, List<ReferenceBuilder>> buildersByKey,
            List<string> warnings)
        {
            Dictionary<string, ReferenceBuilder> builders =
                new Dictionary<string, ReferenceBuilder>(StringComparer.Ordinal);
            foreach (string key in ExtractExpressionKeys(condition))
            {
                GetBuilder(builders, eventAsset, assetPath, displayName, key)
                    .UsedInCondition = true;
            }

            try
            {
                foreach (GameEventCommandDraft command in
                         GameEventCommandDraftCodec.Parse(commandsSource))
                {
                    if (!commandsByName.TryGetValue(
                            command.Name,
                            out EffectCommandDescriptor descriptor))
                    {
                        continue;
                    }

                    int argumentCount = Math.Min(
                        command.Arguments.Count,
                        descriptor.Parameters.Count);
                    for (int index = 0; index < argumentCount; index++)
                    {
                        EffectCommandParameterKind kind = descriptor.Parameters[index].Kind;
                        IEnumerable<string> keys;
                        if (kind == EffectCommandParameterKind.ParameterKey)
                        {
                            keys = new[] { command.Arguments[index] };
                        }
                        else if (kind == EffectCommandParameterKind.NumberExpression ||
                                 kind == EffectCommandParameterKind.ConditionExpression)
                        {
                            keys = ExtractExpressionKeys(command.Arguments[index]);
                        }
                        else
                        {
                            continue;
                        }

                        foreach (string key in keys.Where(value =>
                                     !string.IsNullOrWhiteSpace(value)))
                        {
                            GetBuilder(builders, eventAsset, assetPath, displayName, key)
                                .CommandNames.Add(command.Name);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                warnings.Add(
                    $"無法分析事件「{displayName}」的指令引用：{exception.Message}");
            }

            foreach (KeyValuePair<string, ReferenceBuilder> pair in builders)
            {
                if (!buildersByKey.TryGetValue(
                        pair.Key,
                        out List<ReferenceBuilder> references))
                {
                    references = new List<ReferenceBuilder>();
                    buildersByKey.Add(pair.Key, references);
                }

                references.Add(pair.Value);
            }
        }

        private static ReferenceBuilder GetBuilder(
            Dictionary<string, ReferenceBuilder> builders,
            TextAsset eventAsset,
            string assetPath,
            string displayName,
            string parameterKey)
        {
            if (!builders.TryGetValue(parameterKey, out ReferenceBuilder builder))
            {
                builder = new ReferenceBuilder
                {
                    EventAsset = eventAsset,
                    AssetPath = assetPath ?? string.Empty,
                    EventDisplayName = string.IsNullOrWhiteSpace(displayName)
                        ? "未命名事件"
                        : displayName
                };
                builders.Add(parameterKey, builder);
            }

            return builder;
        }

        internal static IReadOnlyList<string> ExtractExpressionKeys(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return Array.Empty<string>();
            }

            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < source.Length; index++)
            {
                if (source[index] != '$' || index + 1 >= source.Length ||
                    (!char.IsLetter(source[index + 1]) && source[index + 1] != '_'))
                {
                    continue;
                }

                int start = ++index;
                while (index + 1 < source.Length &&
                       (char.IsLetterOrDigit(source[index + 1]) ||
                        source[index + 1] == '_' || source[index + 1] == '.'))
                {
                    index++;
                }

                keys.Add(source.Substring(start, index - start + 1));
            }

            return keys.ToArray();
        }

        private static bool SamePath(string left, string right)
        {
            return string.Equals(
                (left ?? string.Empty).Replace('\\', '/'),
                (right ?? string.Empty).Replace('\\', '/'),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
