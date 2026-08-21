using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KahaGameCore.Effects
{
    [Serializable]
    public sealed class EffectCommandModuleReference
    {
        [SerializeField] private string assemblyName;
        [SerializeField] private string factoryTypeName;

        public EffectCommandModuleReference(
            string assemblyName,
            string factoryTypeName)
        {
            this.assemblyName = Require(assemblyName, nameof(assemblyName));
            this.factoryTypeName = Require(factoryTypeName, nameof(factoryTypeName));
        }

        public string AssemblyName => assemblyName;
        public string FactoryTypeName => factoryTypeName;

        private static string Require(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A value is required.", parameterName);
            return value.Trim();
        }
    }

    public sealed class EffectCommandConfiguration
    {
        public static readonly EffectCommandConfiguration Empty =
            new EffectCommandConfiguration(
                Array.Empty<EffectCommandModuleReference>(),
                Array.Empty<string>());

        public EffectCommandConfiguration(
            IEnumerable<EffectCommandModuleReference> modules,
            IEnumerable<string> commandNames)
        {
            if (modules == null) throw new ArgumentNullException(nameof(modules));
            if (commandNames == null) throw new ArgumentNullException(nameof(commandNames));

            Modules = modules
                .Select(module => module ?? throw new ArgumentException(
                    "The module list contains null.", nameof(modules)))
                .ToArray();
            CommandNames = commandNames
                .Select(value => value?.Trim())
                .Where(value => !string.IsNullOrEmpty(value))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        public IReadOnlyList<EffectCommandModuleReference> Modules { get; }
        public IReadOnlyList<string> CommandNames { get; }
    }
}
