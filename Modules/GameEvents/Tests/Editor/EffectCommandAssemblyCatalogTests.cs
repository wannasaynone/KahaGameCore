using System.Collections.Generic;
using System.Linq;
using KahaGameCore.Effects;
using NUnit.Framework;

namespace KahaGameCore.GameEvents.Tests
{
    public sealed class EffectCommandAssemblyCatalogTests
    {
        [Test]
        public void GetDescriptors_WaitIsOwnedByStandardCommandsAssembly()
        {
            const string standardAssembly =
                "KahaGameCore.Modules.Effects.StandardCommands";
            const string gameFlowAssembly =
                "KahaGameCore.Modules.GameFlowSystem.DefaultImplements";
            List<string> warnings = new List<string>();

            IReadOnlyList<EffectCommandDescriptor> standardDescriptors =
                KahaGameCore.GameEvents.Editor.EffectCommandAssemblyCatalog.GetDescriptors(
                    new[] { standardAssembly },
                    warnings);
            IReadOnlyList<EffectCommandDescriptor> gameFlowDescriptors =
                KahaGameCore.GameEvents.Editor.EffectCommandAssemblyCatalog.GetDescriptors(
                    new[] { gameFlowAssembly },
                    warnings);
            IReadOnlyList<EffectCommandModuleReference> references =
                KahaGameCore.GameEvents.Editor.EffectCommandAssemblyCatalog
                    .GetModuleReferences(new[] { standardAssembly }, warnings);

            Assert.That(
                KahaGameCore.GameEvents.Editor.EffectCommandAssemblyCatalog
                    .GetFactoryAssemblyNames(),
                Does.Contain(standardAssembly));
            Assert.That(
                standardDescriptors.Select(descriptor => descriptor.Name),
                Is.EqualTo(new[] { "Wait" }));
            Assert.That(
                gameFlowDescriptors.Select(descriptor => descriptor.Name),
                Does.Not.Contain("Wait"));
            Assert.That(references.Count, Is.EqualTo(1));
            Assert.That(references[0].AssemblyName, Is.EqualTo(standardAssembly));
            Assert.That(
                references[0].FactoryTypeName,
                Does.Contain("StandardEffectCommandModuleFactory"));
            Assert.That(warnings, Is.Empty);
        }

        [Test]
        public void GetDescriptors_GameEventsOwnsTriggerEvent()
        {
            const string gameEventsAssembly =
                "KahaGameCore.Modules.GameEvents";
            var warnings = new List<string>();

            EffectCommandDescriptor descriptor =
                KahaGameCore.GameEvents.Editor.EffectCommandAssemblyCatalog
                    .GetDescriptors(new[] { gameEventsAssembly }, warnings)
                    .Single(item => item.Name == "TriggerEvent");

            Assert.That(descriptor.DisplayName, Is.EqualTo("Trigger Event"));
            Assert.That(descriptor.Parameters.Count, Is.EqualTo(1));
            Assert.That(
                descriptor.Parameters[0].OptionSourceKey,
                Is.EqualTo(GameEventEffectCommandModule.EventOptionSourceKey));
            Assert.That(warnings, Is.Empty);
        }
    }
}
