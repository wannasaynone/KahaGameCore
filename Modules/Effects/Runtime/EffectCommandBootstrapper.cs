using System;
using System.Collections.Generic;

namespace KahaGameCore.Effects
{
    public sealed class EffectCommandCompositionException : Exception
    {
        public EffectCommandCompositionException(string message) : base(message)
        {
        }

        public EffectCommandCompositionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public static class EffectCommandBootstrapper
    {
        public static EffectRuntime CreateRuntime(
            EffectCommandConfiguration configuration,
            EffectCommandDependencies services)
        {
            EffectCommandRegistry registry = new EffectCommandRegistry();
            registry.PopulateByEffectCommandBootstrapper(configuration, services);
            return new EffectRuntime(registry);
        }

        /// <summary>
        /// Fills <paramref name="registry"/> with every command the configuration enables.
        /// Use this when the registry has to exist before its commands do — a caller whose
        /// dependencies are themselves built on top of the registry's EffectRuntime.
        /// Otherwise prefer <see cref="CreateRuntime"/>.
        /// </summary>
        public static void PopulateByEffectCommandBootstrapper(
            this EffectCommandRegistry registry,
            EffectCommandConfiguration configuration,
            EffectCommandDependencies services)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (services == null) throw new ArgumentNullException(nameof(services));

            Dictionary<string, IEffectCommandModuleFactory> owners =
                DiscoverOwners(configuration.Modules);
            List<EffectCommandDefinition> definitions =
                CreateDefinitions(configuration.CommandNames, owners, services);

            foreach (EffectCommandDefinition definition in definitions)
                registry.Register(definition);
        }

        /// <summary>
        /// Maps every command name published by the selected factories to its owner.
        /// </summary>
        private static Dictionary<string, IEffectCommandModuleFactory> DiscoverOwners(
            IReadOnlyList<EffectCommandModuleReference> references)
        {
            HashSet<string> factoryTypes = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, IEffectCommandModuleFactory> owners =
                new Dictionary<string, IEffectCommandModuleFactory>(StringComparer.Ordinal);

            foreach (EffectCommandModuleReference reference in references)
            {
                if (!factoryTypes.Add(reference.FactoryTypeName))
                {
                    throw new EffectCommandCompositionException(
                        $"Effect command factory '{reference.FactoryTypeName}' is selected more than once.");
                }

                IEffectCommandModuleFactory factory = CreateFactory(reference);
                foreach (EffectCommandDescriptor descriptor in
                         factory.GetDescriptors() ??
                         Array.Empty<EffectCommandDescriptor>())
                {
                    if (descriptor == null)
                    {
                        throw new EffectCommandCompositionException(
                            $"Command factory '{reference.FactoryTypeName}' contains a null descriptor.");
                    }

                    if (owners.ContainsKey(descriptor.Name))
                    {
                        throw new EffectCommandCompositionException(
                            $"Selected command '{descriptor.Name}' is declared by more than one factory.");
                    }

                    owners.Add(descriptor.Name, factory);
                }
            }

            return owners;
        }

        private static IEffectCommandModuleFactory CreateFactory(
            EffectCommandModuleReference reference)
        {
            Type factoryType = Type.GetType(reference.FactoryTypeName, throwOnError: false);
            if (factoryType == null)
            {
                throw new EffectCommandCompositionException(
                    $"Selected command assembly '{reference.AssemblyName}' has no loadable " +
                    $"factory '{reference.FactoryTypeName}'.");
            }

            if (!typeof(IEffectCommandModuleFactory).IsAssignableFrom(factoryType))
            {
                throw new EffectCommandCompositionException(
                    $"Command factory '{reference.FactoryTypeName}' is not an " +
                    "IEffectCommandModuleFactory.");
            }

            try
            {
                return (IEffectCommandModuleFactory)Activator.CreateInstance(factoryType);
            }
            catch (Exception exception)
            {
                throw new EffectCommandCompositionException(
                    $"Could not create command factory '{reference.FactoryTypeName}'.",
                    exception);
            }
        }

        /// <summary>
        /// Builds every enabled command. A factory is only asked to create its commands
        /// when at least one of them is enabled, so a selected but unused assembly never
        /// needs its services registered.
        /// </summary>
        private static List<EffectCommandDefinition> CreateDefinitions(
            IReadOnlyList<string> commandNames,
            IReadOnlyDictionary<string, IEffectCommandModuleFactory> owners,
            EffectCommandDependencies services)
        {
            List<IEffectCommandModuleFactory> factoryOrder =
                new List<IEffectCommandModuleFactory>();
            Dictionary<IEffectCommandModuleFactory, List<string>> wanted =
                new Dictionary<IEffectCommandModuleFactory, List<string>>();

            foreach (string commandName in commandNames)
            {
                if (!owners.TryGetValue(commandName, out IEffectCommandModuleFactory owner))
                {
                    throw new EffectCommandCompositionException(
                        $"Enabled command '{commandName}' is not provided by a selected factory.");
                }

                if (!wanted.TryGetValue(owner, out List<string> names))
                {
                    wanted.Add(owner, names = new List<string>());
                    factoryOrder.Add(owner);
                }

                names.Add(commandName);
            }

            List<EffectCommandDefinition> result =
                new List<EffectCommandDefinition>(commandNames.Count);
            foreach (IEffectCommandModuleFactory factory in factoryOrder)
                Collect(factory, wanted[factory], services, result);

            return result;
        }

        private static void Collect(
            IEffectCommandModuleFactory factory,
            List<string> wanted,
            EffectCommandDependencies services,
            List<EffectCommandDefinition> result)
        {
            string factoryName = factory.GetType().FullName;
            IReadOnlyList<EffectCommandDefinition> created = factory.Create(services);
            if (created == null)
            {
                throw new EffectCommandCompositionException(
                    $"Command factory '{factoryName}' returned no runtime commands.");
            }

            Dictionary<string, EffectCommandDefinition> byName =
                new Dictionary<string, EffectCommandDefinition>(StringComparer.Ordinal);
            foreach (EffectCommandDefinition definition in created)
            {
                if (definition == null)
                {
                    throw new EffectCommandCompositionException(
                        $"Command factory '{factoryName}' returned a null command.");
                }

                byName[definition.Name] = definition;
            }

            foreach (string commandName in wanted)
            {
                if (!byName.TryGetValue(commandName, out EffectCommandDefinition definition))
                {
                    throw new EffectCommandCompositionException(
                        $"Command factory '{factoryName}' publishes '{commandName}' " +
                        "but did not create it.");
                }

                result.Add(definition);
            }
        }
    }
}
