using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.Effects;
using UnityEditor;

namespace KahaGameCore.GameEvents.Editor
{
    internal static class EffectCommandAssemblyCatalog
    {
        public static IReadOnlyList<string> GetFactoryAssemblyNames()
        {
            return GetFactoryTypes()
                .Select(type => type.Assembly.GetName().Name)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
        }

        public static IReadOnlyList<EffectCommandModuleReference> GetModuleReferences(
            IReadOnlyList<string> assemblyNames,
            List<string> warnings)
        {
            HashSet<string> selected = new HashSet<string>(
                assemblyNames ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            Type[] factories = GetFactoryTypes()
                .Where(type => selected.Contains(type.Assembly.GetName().Name))
                .OrderBy(type => type.Assembly.GetName().Name, StringComparer.Ordinal)
                .ThenBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();

            AddMissingAssemblyWarnings(selected, factories, warnings);
            return factories
                .Select(type => new EffectCommandModuleReference(
                    type.Assembly.GetName().Name,
                    StableTypeName(type)))
                .ToArray();
        }

        public static IReadOnlyList<EffectCommandDescriptor> GetDescriptors(
            IReadOnlyList<string> assemblyNames,
            List<string> warnings)
        {
            HashSet<string> selected = new HashSet<string>(
                assemblyNames ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            Type[] factoryTypes = GetFactoryTypes()
                .Where(type => selected.Contains(type.Assembly.GetName().Name))
                .ToArray();
            Dictionary<string, EffectCommandDescriptor> byName =
                new Dictionary<string, EffectCommandDescriptor>(StringComparer.Ordinal);

            foreach (Type type in factoryTypes)
            {
                try
                {
                    IEffectCommandModuleFactory factory =
                        (IEffectCommandModuleFactory)Activator.CreateInstance(type);
                    foreach (EffectCommandDescriptor descriptor in
                             factory.GetDescriptors() ??
                             Array.Empty<EffectCommandDescriptor>())
                    {
                        if (descriptor == null)
                        {
                            warnings?.Add(
                                $"指令工廠「{type.FullName}」回傳空的指令描述。");
                            continue;
                        }

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

            AddMissingAssemblyWarnings(selected, factoryTypes, warnings);
            return byName.Values
                .OrderBy(item => item.Category, StringComparer.Ordinal)
                .ThenBy(item => item.DisplayName, StringComparer.Ordinal)
                .ToArray();
        }

        private static Type[] GetFactoryTypes()
        {
            return TypeCache.GetTypesDerivedFrom<IEffectCommandModuleFactory>()
                .Where(IsConcreteFactory)
                .ToArray();
        }

        private static bool IsConcreteFactory(Type type)
        {
            return type != null && !type.IsAbstract && !type.IsInterface &&
                type.GetConstructor(Type.EmptyTypes) != null;
        }

        private static string StableTypeName(Type type)
        {
            return $"{type.FullName}, {type.Assembly.GetName().Name}";
        }

        private static void AddMissingAssemblyWarnings(
            IEnumerable<string> selectedAssemblies,
            IEnumerable<Type> factoryTypes,
            List<string> warnings)
        {
            HashSet<string> available = factoryTypes
                .Select(type => type.Assembly.GetName().Name)
                .ToHashSet(StringComparer.Ordinal);
            foreach (string missing in selectedAssemblies.Except(
                         available,
                         StringComparer.Ordinal))
            {
                warnings?.Add($"指令組件「{missing}」沒有 runtime module factory。");
            }
        }
    }
}
