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
        private sealed class FactoryRuntime
        {
            public FactoryRuntime(
                EffectCommandModuleReference reference,
                IEffectCommandModuleFactory factory)
            {
                Reference = reference;
                Factory = factory;
            }

            public EffectCommandModuleReference Reference { get; }
            public IEffectCommandModuleFactory Factory { get; }
            public IEffectCommandModule Module { get; set; }
        }

        private sealed class AvailableCommand
        {
            public AvailableCommand(
                FactoryRuntime factory,
                EffectCommandDescriptor descriptor)
            {
                Factory = factory;
                Descriptor = descriptor;
            }

            public FactoryRuntime Factory { get; }
            public EffectCommandDescriptor Descriptor { get; }
        }

        public static EffectRuntime CreateRuntime(
            EffectCommandConfiguration configuration,
            EffectCommandServiceRegistry services)
        {
            EffectCommandRegistry registry = new EffectCommandRegistry();
            Populate(registry, configuration, services);
            return new EffectRuntime(registry);
        }

        public static void Populate(
            EffectCommandRegistry registry,
            EffectCommandConfiguration configuration,
            EffectCommandServiceRegistry services)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (services == null) throw new ArgumentNullException(nameof(services));

            Dictionary<string, AvailableCommand> commands =
                DiscoverCommands(configuration.Modules);
            List<EffectCommandDefinition> definitions =
                CreateDefinitions(configuration.CommandNames, commands, registry, services);

            foreach (EffectCommandDefinition definition in definitions)
                registry.Register(definition);
        }

        private static Dictionary<string, AvailableCommand> DiscoverCommands(
            IReadOnlyList<EffectCommandModuleReference> references)
        {
            HashSet<string> factoryTypes = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, AvailableCommand> commands =
                new Dictionary<string, AvailableCommand>(StringComparer.Ordinal);

            foreach (EffectCommandModuleReference reference in references)
            {
                if (!factoryTypes.Add(reference.FactoryTypeName))
                {
                    throw new EffectCommandCompositionException(
                        $"Effect command factory '{reference.FactoryTypeName}' is selected more than once.");
                }

                FactoryRuntime runtime = new FactoryRuntime(
                    reference,
                    CreateFactory(reference));
                IReadOnlyList<EffectCommandDescriptor> descriptors =
                    runtime.Factory.GetDescriptors() ??
                    Array.Empty<EffectCommandDescriptor>();
                foreach (EffectCommandDescriptor descriptor in descriptors)
                {
                    if (descriptor == null)
                    {
                        throw new EffectCommandCompositionException(
                            $"Command factory '{reference.FactoryTypeName}' contains a null descriptor.");
                    }

                    if (commands.ContainsKey(descriptor.Name))
                    {
                        throw new EffectCommandCompositionException(
                            $"Selected command '{descriptor.Name}' is declared by more than one factory.");
                    }

                    commands.Add(descriptor.Name, new AvailableCommand(runtime, descriptor));
                }
            }

            return commands;
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

            if (!string.Equals(
                    factoryType.Assembly.GetName().Name,
                    reference.AssemblyName,
                    StringComparison.Ordinal))
            {
                throw new EffectCommandCompositionException(
                    $"Command factory '{factoryType.FullName}' does not belong to selected " +
                    $"assembly '{reference.AssemblyName}'.");
            }

            if (factoryType.IsAbstract || factoryType.IsInterface ||
                !typeof(IEffectCommandModuleFactory).IsAssignableFrom(factoryType) ||
                factoryType.GetConstructor(Type.EmptyTypes) == null)
            {
                throw new EffectCommandCompositionException(
                    $"Command factory '{reference.FactoryTypeName}' must be a concrete " +
                    "IEffectCommandModuleFactory with a public parameterless constructor.");
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

        private static List<EffectCommandDefinition> CreateDefinitions(
            IReadOnlyList<string> commandNames,
            IReadOnlyDictionary<string, AvailableCommand> commands,
            EffectCommandRegistry registry,
            EffectCommandServiceRegistry services)
        {
            List<EffectCommandDefinition> result =
                new List<EffectCommandDefinition>(commandNames.Count);
            foreach (string commandName in commandNames)
            {
                if (!commands.TryGetValue(commandName, out AvailableCommand available))
                {
                    throw new EffectCommandCompositionException(
                        $"Enabled command '{commandName}' is not provided by a selected factory.");
                }

                if (registry.TryGetDefinition(commandName, out _))
                {
                    throw new EffectCommandCompositionException(
                        $"Effect command '{commandName}' is already registered.");
                }

                if (available.Factory.Module == null)
                {
                    available.Factory.Module =
                        available.Factory.Factory.Create(services) ??
                        throw new EffectCommandCompositionException(
                            $"Command factory '{available.Factory.Reference.FactoryTypeName}' " +
                            "returned no runtime module.");
                }

                EffectCommandDefinition definition =
                    available.Factory.Module.CreateDefinition(commandName);
                ValidateDefinition(commandName, available, definition);
                result.Add(definition);
            }

            return result;
        }

        private static void ValidateDefinition(
            string commandName,
            AvailableCommand available,
            EffectCommandDefinition definition)
        {
            string factoryName = available.Factory.Reference.FactoryTypeName;
            if (definition == null)
            {
                throw new EffectCommandCompositionException(
                    $"Command factory '{factoryName}' returned no definition for '{commandName}'.");
            }

            if (!string.Equals(definition.Name, commandName, StringComparison.Ordinal))
            {
                throw new EffectCommandCompositionException(
                    $"Command factory '{factoryName}' returned definition '{definition.Name}' " +
                    $"for requested command '{commandName}'.");
            }

            if (!ReferenceEquals(definition.Descriptor, available.Descriptor))
            {
                throw new EffectCommandCompositionException(
                    $"Command factory '{factoryName}' did not use its published descriptor " +
                    $"for '{commandName}'.");
            }
        }
    }
}
