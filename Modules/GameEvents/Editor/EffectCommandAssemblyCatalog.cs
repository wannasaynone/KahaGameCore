using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.Effects;
using UnityEditor;

namespace KahaGameCore.GameEvents.Editor
{
    internal static class EffectCommandAssemblyCatalog
    {
        public static IReadOnlyList<string> GetProviderAssemblyNames()
        {
            return TypeCache.GetTypesDerivedFrom<IEffectCommandDescriptorProvider>()
                .Where(IsConcreteProvider)
                .Select(type => type.Assembly.GetName().Name)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
        }

        public static IReadOnlyList<EffectCommandDescriptor> GetDescriptors(
            IReadOnlyList<string> assemblyNames,
            List<string> warnings)
        {
            HashSet<string> allowed = new HashSet<string>(
                assemblyNames ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            Dictionary<string, EffectCommandDescriptor> byName =
                new Dictionary<string, EffectCommandDescriptor>(StringComparer.Ordinal);

            foreach (Type type in TypeCache
                         .GetTypesDerivedFrom<IEffectCommandDescriptorProvider>()
                         .Where(IsConcreteProvider)
                         .Where(type => allowed.Contains(type.Assembly.GetName().Name)))
            {
                try
                {
                    IEffectCommandDescriptorProvider provider =
                        (IEffectCommandDescriptorProvider)Activator.CreateInstance(type);
                    foreach (EffectCommandDescriptor descriptor in
                             provider.GetDescriptors() ?? Array.Empty<EffectCommandDescriptor>())
                    {
                        if (byName.ContainsKey(descriptor.Name))
                        {
                            warnings?.Add(
                                $"指令「{descriptor.Name}」在所選組件中重複宣告。");
                            continue;
                        }

                        byName.Add(descriptor.Name, descriptor);
                    }
                }
                catch (Exception exception)
                {
                    warnings?.Add(
                        $"無法從「{type.FullName}」載入指令描述：{exception.Message}");
                }
            }

            foreach (string missing in allowed.Except(
                         GetProviderAssemblyNames(), StringComparer.Ordinal))
            {
                warnings?.Add(
                    $"指令組件「{missing}」沒有描述提供者。");
            }

            return byName.Values
                .OrderBy(item => item.Category, StringComparer.Ordinal)
                .ThenBy(item => item.DisplayName, StringComparer.Ordinal)
                .ToArray();
        }

        private static bool IsConcreteProvider(Type type)
        {
            return type != null && !type.IsAbstract && !type.IsInterface &&
                type.GetConstructor(Type.EmptyTypes) != null;
        }
    }
}
