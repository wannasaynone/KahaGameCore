using System;
using System.Collections.Generic;

namespace KahaGameCore.Effects
{
    public sealed class EffectCommandCall
    {
        public EffectCommandCall(string name, IEnumerable<string> arguments, int position, int length)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Arguments = new List<string>(arguments ?? throw new ArgumentNullException(nameof(arguments))).AsReadOnly();
            Position = position;
            Length = length;
        }

        public string Name { get; }
        public IReadOnlyList<string> Arguments { get; }
        public int Position { get; }
        public int Length { get; }
    }

    public sealed class EffectTimingBlock
    {
        public EffectTimingBlock(string name, IEnumerable<EffectCommandCall> commands)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Commands = new List<EffectCommandCall>(commands ?? throw new ArgumentNullException(nameof(commands))).AsReadOnly();
        }

        public string Name { get; }
        public IReadOnlyList<EffectCommandCall> Commands { get; }
    }

    public sealed class EffectProgram
    {
        public EffectProgram(bool usesExplicitTimings, IEnumerable<EffectTimingBlock> blocks)
        {
            UsesExplicitTimings = usesExplicitTimings;
            Blocks = new List<EffectTimingBlock>(blocks ?? throw new ArgumentNullException(nameof(blocks))).AsReadOnly();
        }

        public bool UsesExplicitTimings { get; }
        public IReadOnlyList<EffectTimingBlock> Blocks { get; }
    }

    public sealed class EffectParseResult
    {
        private EffectParseResult(EffectProgram program, IEnumerable<EffectDiagnostic> diagnostics)
        {
            Program = program;
            Diagnostics = new List<EffectDiagnostic>(diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).AsReadOnly();
        }

        public bool IsSuccess => Program != null && Diagnostics.Count == 0;
        public EffectProgram Program { get; }
        public IReadOnlyList<EffectDiagnostic> Diagnostics { get; }

        public static EffectParseResult Success(EffectProgram program)
        {
            return new EffectParseResult(program ?? throw new ArgumentNullException(nameof(program)), new EffectDiagnostic[0]);
        }

        public static EffectParseResult Failure(params EffectDiagnostic[] diagnostics)
        {
            return new EffectParseResult(null, diagnostics);
        }

        public string FormatDiagnostics()
        {
            return string.Join("\n", Diagnostics);
        }
    }
}
