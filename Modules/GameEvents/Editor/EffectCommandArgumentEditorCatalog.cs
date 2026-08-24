using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace KahaGameCore.GameEvents.Editor
{
    internal static class EffectCommandArgumentEditorCatalog
    {
        private static IReadOnlyList<IEffectCommandArgumentEditorProvider> providers;

        public static bool TryGetProvider(
            string sourceKey,
            out IEffectCommandArgumentEditorProvider provider,
            out string error)
        {
            provider = null;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(sourceKey))
            {
                return false;
            }

            IReadOnlyList<IEffectCommandArgumentEditorProvider> matches =
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

            if (matches.Count > 1)
            {
                error = $"參數編輯器來源「{sourceKey}」有 {matches.Count} 個提供者，必須保持唯一。";
            }

            return false;
        }

        internal static void Reset()
        {
            providers = null;
        }

        private static IReadOnlyList<IEffectCommandArgumentEditorProvider> Providers =>
            providers ??= CreateProviders();

        private static IReadOnlyList<IEffectCommandArgumentEditorProvider> CreateProviders()
        {
            var result = new List<IEffectCommandArgumentEditorProvider>();
            foreach (Type type in TypeCache
                         .GetTypesDerivedFrom<IEffectCommandArgumentEditorProvider>()
                         .Where(IsConcreteProvider)
                         .OrderBy(item => item.FullName, StringComparer.Ordinal))
            {
                try
                {
                    result.Add(
                        (IEffectCommandArgumentEditorProvider)
                        Activator.CreateInstance(type));
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogError(
                        $"無法建立 Game Event 指令參數編輯器「{type.FullName}」：" +
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
