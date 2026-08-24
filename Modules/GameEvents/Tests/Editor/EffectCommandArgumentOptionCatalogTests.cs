using System;
using System.Collections.Generic;
using KahaGameCore.Effects;
using KahaGameCore.GameEvents.Editor;
using NUnit.Framework;

namespace KahaGameCore.GameEvents.Tests
{
    public sealed class EffectCommandArgumentOptionCatalogTests
    {
        public sealed class UniqueProvider :
            IEffectCommandArgumentOptionProvider
        {
            public string SourceKey => "TestUniqueOptions";

            public IReadOnlyList<EffectCommandArgumentOption> GetOptions(
                EffectCommandArgumentOptionContext context)
            {
                return Array.Empty<EffectCommandArgumentOption>();
            }

            public void StopPreview()
            {
            }
        }

        public sealed class DuplicateProviderA :
            IEffectCommandArgumentOptionProvider
        {
            public string SourceKey => "TestDuplicateOptions";

            public IReadOnlyList<EffectCommandArgumentOption> GetOptions(
                EffectCommandArgumentOptionContext context)
            {
                return Array.Empty<EffectCommandArgumentOption>();
            }

            public void StopPreview()
            {
            }
        }

        public sealed class DuplicateProviderB :
            IEffectCommandArgumentOptionProvider
        {
            public string SourceKey => "TestDuplicateOptions";

            public IReadOnlyList<EffectCommandArgumentOption> GetOptions(
                EffectCommandArgumentOptionContext context)
            {
                return Array.Empty<EffectCommandArgumentOption>();
            }

            public void StopPreview()
            {
            }
        }

        public sealed class UniqueEditorProvider :
            IEffectCommandArgumentEditorProvider
        {
            public string SourceKey => "TestUniqueEditor";

            public void Draw(EffectCommandArgumentEditorContext context)
            {
            }
        }

        [SetUp]
        public void SetUp()
        {
            EffectCommandArgumentOptionCatalog.Reset();
            EffectCommandArgumentEditorCatalog.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            EffectCommandArgumentOptionCatalog.Reset();
            EffectCommandArgumentEditorCatalog.Reset();
        }

        [Test]
        public void ParameterDefinition_PreservesTrimmedOptionSourceKey()
        {
            var parameter = new EffectCommandParameterDefinition(
                "camera",
                EffectCommandParameterKind.Literal,
                "  CameraCue  ");

            Assert.That(parameter.OptionSourceKey, Is.EqualTo("CameraCue"));
        }

        [Test]
        public void ArgumentOptionContext_CopiesCurrentCommandArguments()
        {
            var arguments = new List<string> { "actor-guid", "Idle" };

            var context = new EffectCommandArgumentOptionContext(
                " PlayActorAnimation ",
                1,
                arguments);
            arguments[0] = "changed";

            Assert.That(context.CommandName, Is.EqualTo("PlayActorAnimation"));
            Assert.That(context.ArgumentIndex, Is.EqualTo(1));
            Assert.That(context.Arguments[0], Is.EqualTo("actor-guid"));
        }

        [Test]
        public void ArgumentOption_AllowsLabeledEmptyLiteral()
        {
            var option = new EffectCommandArgumentOption(
                string.Empty,
                "No animation");

            Assert.That(option.Value, Is.Empty);
            Assert.That(option.Label, Is.EqualTo("No animation"));
        }

        [Test]
        public void Catalog_ResolvesOneProviderForSourceKey()
        {
            bool found = EffectCommandArgumentOptionCatalog.TryGetProvider(
                "TestUniqueOptions",
                out IEffectCommandArgumentOptionProvider provider,
                out string error);

            Assert.That(found, Is.True, error);
            Assert.That(provider, Is.TypeOf<UniqueProvider>());
        }

        [Test]
        public void Catalog_RejectsDuplicatedSourceKeys()
        {
            bool found = EffectCommandArgumentOptionCatalog.TryGetProvider(
                "TestDuplicateOptions",
                out _,
                out string error);

            Assert.That(found, Is.False);
            Assert.That(error, Does.Contain("2 個提供者"));
        }

        [Test]
        public void CustomEditorContext_CopiesArgumentsAndCanSetValue()
        {
            var arguments = new List<string> { "speaker", "line" };
            string assigned = string.Empty;
            var context = new EffectCommandArgumentEditorContext(
                "event-guid",
                "Say",
                1,
                arguments,
                value => assigned = value);
            arguments[1] = "changed";

            context.SetValue("replacement");

            Assert.That(context.DocumentGuid, Is.EqualTo("event-guid"));
            Assert.That(context.Value, Is.EqualTo("line"));
            Assert.That(assigned, Is.EqualTo("replacement"));
        }

        [Test]
        public void CustomEditorCatalog_ResolvesOneProviderForSourceKey()
        {
            bool found = EffectCommandArgumentEditorCatalog.TryGetProvider(
                "TestUniqueEditor",
                out IEffectCommandArgumentEditorProvider provider,
                out string error);

            Assert.That(found, Is.True, error);
            Assert.That(provider, Is.TypeOf<UniqueEditorProvider>());
        }
    }
}
