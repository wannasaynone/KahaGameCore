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

            public IEffectCommandModule Create(EffectCommandServiceRegistry services)
            {
                return new TestModule(Enabled, Disabled);
            }
        }

        public sealed class OtherFactory : IEffectCommandModuleFactory
        {
            internal static readonly EffectCommandDescriptor Other = Describe("Other");

            public IReadOnlyList<EffectCommandDescriptor> GetDescriptors()
            {
                return new[] { Other };
            }

            public IEffectCommandModule Create(EffectCommandServiceRegistry services)
            {
                return new TestModule(Other);
            }
        }

        private sealed class TestModule : IEffectCommandModule
        {
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

            private readonly IReadOnlyList<EffectCommandDescriptor> descriptors;

            public TestModule(params EffectCommandDescriptor[] descriptors)
            {
                this.descriptors = descriptors;
            }

            public EffectCommandDefinition CreateDefinition(string commandName)
            {
                foreach (EffectCommandDescriptor descriptor in descriptors)
                {
                    if (descriptor.Name == commandName)
                        return new EffectCommandDefinition(descriptor, new NoOpCommand());
                }

                throw new InvalidOperationException(commandName);
            }
        }

        [Test]
        public void Populate_RegistersOnlyEnabledCommands()
        {
            EffectCommandRegistry registry = new EffectCommandRegistry();
            EffectCommandConfiguration configuration = Configuration<TestFactory>("Enabled");

            EffectCommandBootstrapper.Populate(
                registry,
                configuration,
                new EffectCommandServiceRegistry());

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
                () => EffectCommandBootstrapper.Populate(
                    registry,
                    configuration,
                    new EffectCommandServiceRegistry()),
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
                () => EffectCommandBootstrapper.Populate(
                    registry,
                    configuration,
                    new EffectCommandServiceRegistry()),
                Throws.TypeOf<EffectCommandCompositionException>()
                    .With.Message.Contains("not provided by a selected factory"));
            Assert.That(registry.TryGetDefinition("Enabled", out _), Is.False);
            Assert.That(registry.TryGetDefinition("Other", out _), Is.False);
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
