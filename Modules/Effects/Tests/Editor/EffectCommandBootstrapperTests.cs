using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace KahaGameCore.Effects.Tests
{
    public sealed class EffectCommandBootstrapperTests
    {
        public sealed class TestFactory : IEffectCommandModuleFactory
        {
            internal static readonly EffectCommandDescriptor Enabled = Describe("Enabled");
            internal static readonly EffectCommandDescriptor Disabled = Describe("Disabled");

            public IReadOnlyList<EffectCommandDescriptor> GetDescriptors()
            {
                return new[] { Enabled, Disabled };
            }

            public IReadOnlyList<EffectCommandDefinition> Create(
                EffectCommandDependencies services)
            {
                return Definitions(Enabled, Disabled);
            }
        }

        public sealed class OtherFactory : IEffectCommandModuleFactory
        {
            internal static readonly EffectCommandDescriptor Other = Describe("Other");

            public IReadOnlyList<EffectCommandDescriptor> GetDescriptors()
            {
                return new[] { Other };
            }

            public IReadOnlyList<EffectCommandDefinition> Create(
                EffectCommandDependencies services)
            {
                return Definitions(Other);
            }
        }

        private sealed class NoOpCommand : IEffectCommand
        {
            public UniTask ExecuteAsync(
                EffectExecutionContext context,
                IReadOnlyList<string> arguments,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }
        }

        private static IReadOnlyList<EffectCommandDefinition> Definitions(
            params EffectCommandDescriptor[] descriptors)
        {
            List<EffectCommandDefinition> definitions =
                new List<EffectCommandDefinition>(descriptors.Length);
            foreach (EffectCommandDescriptor descriptor in descriptors)
                definitions.Add(new EffectCommandDefinition(descriptor, new NoOpCommand()));
            return definitions;
        }

        [Test]
        public void Populate_RegistersOnlyEnabledCommands()
        {
            EffectCommandRegistry registry = new EffectCommandRegistry();
            EffectCommandConfiguration configuration = Configuration<TestFactory>("Enabled");

            registry.PopulateByEffectCommandBootstrapper(
                configuration,
                new EffectCommandDependencies());

            Assert.That(registry.TryGetDefinition("Enabled", out _), Is.True);
            Assert.That(registry.TryGetDefinition("Disabled", out _), Is.False);
        }

        [Test]
        public void Populate_MissingFactoryFailsBeforeRegistration()
        {
            EffectCommandRegistry registry = new EffectCommandRegistry();
            EffectCommandConfiguration configuration = new EffectCommandConfiguration(
                new[]
                {
                    new EffectCommandModuleReference(
                        "Missing.Commands",
                        "Missing.Commands.Factory, Missing.Commands")
                },
                new[] { "Missing" });

            Assert.That(
                () => registry.PopulateByEffectCommandBootstrapper(
                    configuration,
                    new EffectCommandDependencies()),
                Throws.TypeOf<EffectCommandCompositionException>()
                    .With.Message.Contains("has no loadable factory"));
            Assert.That(registry.TryGetDefinition("Missing", out _), Is.False);
        }

        [Test]
        public void Populate_CommandOutsideSelectedFactoriesFails()
        {
            EffectCommandRegistry registry = new EffectCommandRegistry();
            EffectCommandConfiguration configuration = Configuration<TestFactory>("Other");

            Assert.That(
                () => registry.PopulateByEffectCommandBootstrapper(
                    configuration,
                    new EffectCommandDependencies()),
                Throws.TypeOf<EffectCommandCompositionException>()
                    .With.Message.Contains("not provided by a selected factory"));
            Assert.That(registry.TryGetDefinition("Enabled", out _), Is.False);
            Assert.That(registry.TryGetDefinition("Other", out _), Is.False);
        }

        public sealed class LyingFactory : IEffectCommandModuleFactory
        {
            internal static readonly EffectCommandDescriptor Promised = Describe("Promised");

            public IReadOnlyList<EffectCommandDescriptor> GetDescriptors()
            {
                return new[] { Promised };
            }

            public IReadOnlyList<EffectCommandDefinition> Create(
                EffectCommandDependencies services)
            {
                return Array.Empty<EffectCommandDefinition>();
            }
        }

        [Test]
        public void Populate_FactoryThatSkipsAPublishedCommandFails()
        {
            EffectCommandRegistry registry = new EffectCommandRegistry();
            EffectCommandConfiguration configuration =
                Configuration<LyingFactory>("Promised");

            Assert.That(
                () => registry.PopulateByEffectCommandBootstrapper(
                    configuration,
                    new EffectCommandDependencies()),
                Throws.TypeOf<EffectCommandCompositionException>()
                    .With.Message.Contains("but did not create it"));
            Assert.That(registry.TryGetDefinition("Promised", out _), Is.False);
        }

        private static EffectCommandConfiguration Configuration<TFactory>(
            params string[] commands)
        {
            Type type = typeof(TFactory);
            return new EffectCommandConfiguration(
                new[]
                {
                    new EffectCommandModuleReference(
                        type.Assembly.GetName().Name,
                        $"{type.FullName}, {type.Assembly.GetName().Name}")
                },
                commands);
        }

        private static EffectCommandDescriptor Describe(string name)
        {
            return new EffectCommandDescriptor(
                name,
                name,
                "Tests",
                Array.Empty<EffectCommandParameterDefinition>());
        }
    }
}
