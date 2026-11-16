using System;
using System.Collections.Generic;
using KahaGameCore.Effects;
using KahaGameCore.GameEvents.Editor;
using NUnit.Framework;

namespace KahaGameCore.GameEvents.Tests
{
    public sealed class GameEventParameterRenamerTests
    {
        private static readonly IReadOnlyDictionary<string, EffectCommandDescriptor> Commands =
            new Dictionary<string, EffectCommandDescriptor>(StringComparer.Ordinal)
            {
                ["SetParameter"] = Describe(
                    "SetParameter",
                    new EffectCommandParameterDefinition("key", EffectCommandParameterKind.ParameterKey),
                    new EffectCommandParameterDefinition("value", EffectCommandParameterKind.Literal)),
                ["AddParameter"] = Describe(
                    "AddParameter",
                    new EffectCommandParameterDefinition("key", EffectCommandParameterKind.ParameterKey),
                    new EffectCommandParameterDefinition("value", EffectCommandParameterKind.NumberExpression)),
                ["Say"] = Describe(
                    "Say",
                    new EffectCommandParameterDefinition("speaker", EffectCommandParameterKind.Literal),
                    new EffectCommandParameterDefinition("text", EffectCommandParameterKind.TextKey))
            };

        [Test]
        public void RenameInExpression_OnlyMatchesWholeKey()
        {
            string renamed = GameEventParameterRenamer.RenameInExpression(
                source: "$A1BAC > 3 && $A1B == 1 || $XA1B > 0",
                oldKey: "A1B",
                newKey: "C1B");

            Assert.AreEqual("$A1BAC > 3 && $C1B == 1 || $XA1B > 0", renamed);
        }

        [Test]
        public void RenameInExpression_KeyWithDotIsReadWhole()
        {
            string renamed = GameEventParameterRenamer.RenameInExpression(
                source: "$A1B.x + $A1B",
                oldKey: "A1B",
                newKey: "C1B");

            Assert.AreEqual("$A1B.x + $C1B", renamed);
        }

        [Test]
        public void RenameInExpression_WithoutMatch_ReturnsSameInstance()
        {
            const string source = "$A1BAC > 3";

            Assert.AreSame(
                source,
                GameEventParameterRenamer.RenameInExpression(
                    source: source,
                    oldKey: "A1B",
                    newKey: "C1B"));
        }

        [Test]
        public void RenameInCommands_OnlyChangesParameterKeyArgumentsAndFormulas()
        {
            string renamed = GameEventParameterRenamer.RenameInCommands(
                source: "SetParameter(A1B,true);SetParameter(A1BAC,true);" +
                    "AddParameter(A1BAC,$A1B * 2);Say(A1B,A1B);",
                commandsByName: Commands,
                oldKey: "A1B",
                newKey: "C1B");

            Assert.AreEqual(
                "SetParameter(C1B,true);SetParameter(A1BAC,true);" +
                "AddParameter(A1BAC,$C1B * 2);Say(A1B,A1B);",
                renamed);
        }

        [Test]
        public void RenameInCommands_WithoutMatch_KeepsOriginalText()
        {
            const string source = "SetParameter( A1BAC , true );";

            Assert.AreSame(
                source,
                GameEventParameterRenamer.RenameInCommands(
                    source: source,
                    commandsByName: Commands,
                    oldKey: "A1B",
                    newKey: "C1B"));
        }

        [TestCase("C1B", true)]
        [TestCase("_Quest.Stage", true)]
        [TestCase("1AB", false)]
        [TestCase("A B", false)]
        [TestCase("", false)]
        public void IsValidKey(string key, bool expected)
        {
            Assert.AreEqual(expected, GameEventParameterRenamer.IsValidKey(key));
        }

        private static EffectCommandDescriptor Describe(
            string name,
            params EffectCommandParameterDefinition[] parameters)
        {
            return new EffectCommandDescriptor(
                name: name,
                displayName: name,
                category: "Test",
                parameters: parameters);
        }
    }
}
