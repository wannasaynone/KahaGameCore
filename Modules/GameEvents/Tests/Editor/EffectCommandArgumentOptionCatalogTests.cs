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

            public IReadOnlyList<EffectCommandArgumentOption> GetOptions()
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

            public IReadOnlyList<EffectCommandArgumentOption> GetOptions()
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

            public IReadOnlyList<EffectCommandArgumentOption> GetOptions()
            {
                return Array.Empty<EffectCommandArgumentOption>();
            }

            public void StopPreview()
            {
            }
        }

        [SetUp]
        public void SetUp()
        {
            EffectCommandArgumentOptionCatalog.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            EffectCommandArgumentOptionCatalog.Reset();
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
    }
}
