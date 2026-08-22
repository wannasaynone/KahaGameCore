using System;
using KahaGameCore.GameEvents.Editor;
using NUnit.Framework;
using UnityEngine;

namespace KahaGameCore.GameEvents.Tests
{
    public sealed class EffectCommandCategoryColorPaletteTests
    {
        [Test]
        public void TryGetColor_UnconfiguredCategory_ReturnsFalse()
        {
            var palette = new EffectCommandCategoryColorPalette();

            bool found = palette.TryGetColor("Presentation", out Color color);

            Assert.That(found, Is.False);
            Assert.That(color, Is.EqualTo(default(Color)));
        }

        [Test]
        public void Replace_ConfiguredCategory_ReturnsItsColor()
        {
            var palette = new EffectCommandCategoryColorPalette();
            var expected = new Color(0.2f, 0.4f, 0.8f, 1f);
            palette.Replace(new[]
            {
                new EffectCommandCategoryColorEntry("  Presentation  ", expected)
            });

            bool found = palette.TryGetColor("Presentation", out Color actual);

            Assert.That(found, Is.True);
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(palette.Entries[0].Category, Is.EqualTo("Presentation"));
        }

        [Test]
        public void Replace_DuplicateNormalizedCategory_Throws()
        {
            var palette = new EffectCommandCategoryColorPalette();

            Assert.Throws<ArgumentException>(() => palette.Replace(new[]
            {
                new EffectCommandCategoryColorEntry("Actor", Color.red),
                new EffectCommandCategoryColorEntry(" Actor ", Color.blue)
            }));
        }
    }
}
