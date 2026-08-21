using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace KahaGameCore.GameEvents.Editor
{
    internal static class EffectCommandArgumentOptionCatalog
    {
        private static IReadOnlyList<IEffectCommandArgumentOptionProvider> providers;

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
