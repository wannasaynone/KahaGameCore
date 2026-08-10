using NUnit.Framework;

namespace KahaGameCore.Effects.Tests
{
    public sealed class EffectRuntimeCodecTests
    {
        [Test]
        public void ParseAndSerialize_PreservesQuotedDelimitersAndNestedArguments()
        {
            EffectRuntime runtime = new EffectRuntime(new EffectCommandRegistry());

            EffectParseResult parsed = runtime.Parse(
                "ShowHint(\"Hello, world; again\");SetParameter(Value,Random(1,2));");

            Assert.That(parsed.IsSuccess, Is.True, parsed.FormatDiagnostics());
            Assert.That(parsed.Program.UsesExplicitTimings, Is.False);
            Assert.That(parsed.Program.Blocks, Has.Count.EqualTo(1));
            Assert.That(parsed.Program.Blocks[0].Name, Is.EqualTo(EffectRuntime.DefaultTiming));
            Assert.That(parsed.Program.Blocks[0].Commands, Has.Count.EqualTo(2));
            Assert.That(parsed.Program.Blocks[0].Commands[0].Name, Is.EqualTo("ShowHint"));
            Assert.That(parsed.Program.Blocks[0].Commands[0].Arguments[0], Is.EqualTo("Hello, world; again"));
            Assert.That(parsed.Program.Blocks[0].Commands[1].Arguments[1], Is.EqualTo("Random(1,2)"));

            string serialized = runtime.Serialize(parsed.Program);
            EffectParseResult reparsed = runtime.Parse(serialized);

            Assert.That(serialized, Is.EqualTo(
                "ShowHint(\"Hello, world; again\");SetParameter(Value,Random(1,2));"));
            Assert.That(reparsed.IsSuccess, Is.True, reparsed.FormatDiagnostics());
            Assert.That(reparsed.Program.Blocks[0].Commands[0].Arguments[0],
                Is.EqualTo("Hello, world; again"));
        }

        [Test]
        public void Parse_ExplicitTimingBlocksRemainDistinctAndOrdered()
        {
            EffectRuntime runtime = new EffectRuntime(new EffectCommandRegistry());

            EffectParseResult parsed = runtime.Parse("Start{Wait(1);}Finish{ReturnToTitle();}");

            Assert.That(parsed.IsSuccess, Is.True, parsed.FormatDiagnostics());
            Assert.That(parsed.Program.UsesExplicitTimings, Is.True);
            Assert.That(parsed.Program.Blocks, Has.Count.EqualTo(2));
            Assert.That(parsed.Program.Blocks[0].Name, Is.EqualTo("Start"));
            Assert.That(parsed.Program.Blocks[0].Commands[0].Name, Is.EqualTo("Wait"));
            Assert.That(parsed.Program.Blocks[1].Name, Is.EqualTo("Finish"));
            Assert.That(runtime.Serialize(parsed.Program),
                Is.EqualTo("Start{Wait(1);}Finish{ReturnToTitle();}"));
        }

        [Test]
        public void ParseAndSerialize_PreservesBracesInsideQuotedArgument()
        {
            EffectRuntime runtime = new EffectRuntime(new EffectCommandRegistry());
            EffectParseResult parsed = runtime.Parse("ShowHint(\"Use { and } literally\");");

            Assert.That(parsed.IsSuccess, Is.True, parsed.FormatDiagnostics());

            string serialized = runtime.Serialize(parsed.Program);
            EffectParseResult reparsed = runtime.Parse(serialized);

            Assert.That(serialized, Is.EqualTo("ShowHint(\"Use { and } literally\");"));
            Assert.That(reparsed.IsSuccess, Is.True, reparsed.FormatDiagnostics());
            Assert.That(reparsed.Program.Blocks[0].Commands[0].Arguments[0],
                Is.EqualTo("Use { and } literally"));
        }
    }
}
