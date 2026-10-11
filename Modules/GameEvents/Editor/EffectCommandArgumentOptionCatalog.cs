using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace KahaGameCore.GameEvents.Editor
{
    internal static class EffectCommandArgumentOptionCatalog
    {
        private static IReadOnlyList<IEffectCommandArgumentOptionProvider> providers;
        private static readonly Dictionary<string, IReadOnlyList<EffectCommandArgumentOption>>
            optionCache =
                new Dictionary<string, IReadOnlyList<EffectCommandArgumentOption>>(
                    StringComparer.Ordinal);

        static EffectCommandArgumentOptionCatalog()
        {
            EditorApplication.hierarchyChanged += InvalidateOptions;
            EditorApplication.projectChanged += InvalidateOptions;
            ObjectChangeEvents.changesPublished += OnObjectChanges;
        }

        private static void OnObjectChanges(ref ObjectChangeEventStream stream)
        {
            for (int index = 0; index < stream.length; index++)
            {
                // Asset edits (typing a dialogue line records one per keystroke) need
                // another window; the editor invalidates when it gets focus back.
                if (stream.GetEventType(index) !=
                    ObjectChangeKind.ChangeAssetObjectProperties)
                {
                    InvalidateOptions();
                    return;
                }
            }
        }

        // Providers scan the scene and the editor asks once per argument per OnGUI,
        // so answers are kept until the scene or project changes.
        public static IReadOnlyList<EffectCommandArgumentOption> GetOptions(
            IEffectCommandArgumentOptionProvider provider,
            EffectCommandArgumentOptionContext context)
        {
            string key = string.Join(
                "\u001f",
                provider.SourceKey,
                context.CommandName,
                context.ArgumentIndex,
                string.Join("\u001f", context.Arguments));
            if (optionCache.TryGetValue(
                    key,
                    out IReadOnlyList<EffectCommandArgumentOption> options))
            {
                return options;
            }

            // Typing in a free-text argument makes a new key per keystroke.
            if (optionCache.Count >= 1024)
            {
                optionCache.Clear();
            }

            options = provider.GetOptions(context) ??
                      Array.Empty<EffectCommandArgumentOption>();
            optionCache[key] = options;
            return options;
        }

        internal static void InvalidateOptions()
        {
            optionCache.Clear();
        }

        public static bool TryGetProvider(
            string sourceKey,
            out IEffectCommandArgumentOptionProvider provider,
            out string error)
        {
            provider = null;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(sourceKey))
            {
                return false;
            }

            IReadOnlyList<IEffectCommandArgumentOptionProvider> matches =
                Providers
                    .Where(item => string.Equals(
                        item.SourceKey,
                        sourceKey,
                        StringComparison.Ordinal))
                    .ToArray();
            if (matches.Count == 1)
            {
                provider = matches[0];
                return true;
            }

            error = matches.Count == 0
                ? $"找不到選項來源「{sourceKey}」。"
                : $"選項來源「{sourceKey}」有 {matches.Count} 個提供者，必須保持唯一。";
            return false;
        }

        public static void StopAllPreviews()
        {
            if (providers == null)
            {
                return;
            }

            foreach (IEffectCommandArgumentOptionProvider provider in providers)
            {
                try
                {
                    provider.StopPreview();
                }
                catch (Exception)
                {
                    // Editor shutdown and assembly reload must not be blocked by preview cleanup.
                }
            }
        }

        internal static void Reset()
        {
            StopAllPreviews();
            providers = null;
            InvalidateOptions();
        }

        private static IReadOnlyList<IEffectCommandArgumentOptionProvider> Providers =>
            providers ??= CreateProviders();

        private static IReadOnlyList<IEffectCommandArgumentOptionProvider> CreateProviders()
        {
            var result = new List<IEffectCommandArgumentOptionProvider>();
            foreach (Type type in TypeCache
                         .GetTypesDerivedFrom<IEffectCommandArgumentOptionProvider>()
                         .Where(IsConcreteProvider)
                         .OrderBy(item => item.FullName, StringComparer.Ordinal))
            {
                try
                {
                    result.Add(
                        (IEffectCommandArgumentOptionProvider)
                        Activator.CreateInstance(type));
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogError(
                        $"無法建立 Game Event 指令參數選項提供者「{type.FullName}」：" +
                        exception.Message);
                }
            }

            return result;
        }

        private static bool IsConcreteProvider(Type type)
        {
            return type != null &&
                   !type.IsAbstract &&
                   !type.IsInterface &&
                   type.GetConstructor(Type.EmptyTypes) != null;
        }
    }
}
