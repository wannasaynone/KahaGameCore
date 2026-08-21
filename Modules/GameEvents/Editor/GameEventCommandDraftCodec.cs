using System;
using System.Collections.Generic;
using System.Text;
using KahaGameCore.Effects;

namespace KahaGameCore.GameEvents.Editor
{
    [Serializable]
    internal sealed class GameEventCommandDraft
    {
        public string Name;
        public List<string> Arguments = new List<string>();
    }

    internal static class GameEventCommandDraftOperations
    {
        public static void InsertBlank(
            List<GameEventCommandDraft> drafts,
            int index)
        {
            if (drafts == null) throw new ArgumentNullException(nameof(drafts));
            if (index < 0 || index > drafts.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            drafts.Insert(index, new GameEventCommandDraft());
        }

        public static void Duplicate(
            List<GameEventCommandDraft> drafts,
            int sourceIndex)
        {
            if (drafts == null) throw new ArgumentNullException(nameof(drafts));
            if (sourceIndex < 0 || sourceIndex >= drafts.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex));
            }

            GameEventCommandDraft source = drafts[sourceIndex] ??
                throw new InvalidOperationException(
                    $"Command row {sourceIndex + 1} is missing.");
            drafts.Insert(
                sourceIndex + 1,
                new GameEventCommandDraft
                {
                    Name = source.Name,
                    Arguments = new List<string>(source.Arguments)
                });
        }
    }

    internal static class GameEventCommandDraftCodec
    {
        public static List<GameEventCommandDraft> Parse(string source)
        {
            EffectParseResult result = new EffectRuntime(new EffectCommandRegistry()).Parse(source);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(result.FormatDiagnostics());
            }

            if (result.Program.UsesExplicitTimings)
            {
                throw new InvalidOperationException(
                    "Game Event Commands must be a flat command sequence; TriggerTiming belongs to the document field.");
            }

            List<GameEventCommandDraft> drafts = new List<GameEventCommandDraft>();
            foreach (EffectCommandCall call in result.Program.Blocks[0].Commands)
            {
                drafts.Add(new GameEventCommandDraft
                {
                    Name = call.Name,
                    Arguments = new List<string>(call.Arguments)
                });
            }

            return drafts;
        }

        public static string Serialize(IReadOnlyList<GameEventCommandDraft> drafts)
        {
            if (drafts == null) throw new ArgumentNullException(nameof(drafts));

            StringBuilder builder = new StringBuilder();
            for (int index = 0; index < drafts.Count; index++)
            {
                GameEventCommandDraft draft = drafts[index] ??
                    throw new InvalidOperationException($"Command row {index + 1} is missing.");
                if (string.IsNullOrWhiteSpace(draft.Name))
                {
                    throw new InvalidOperationException($"Command row {index + 1} has no command selected.");
                }

                builder.Append(draft.Name).Append('(');
                for (int argumentIndex = 0;
                     argumentIndex < draft.Arguments.Count;
                     argumentIndex++)
                {
                    if (argumentIndex > 0)
                    {
                        builder.Append(',');
                    }

                    AppendArgument(builder, draft.Arguments[argumentIndex] ?? string.Empty);
                }

                builder.Append(");");
            }

            return builder.ToString();
        }

        private static void AppendArgument(StringBuilder builder, string argument)
        {
            if (!NeedsQuoting(argument))
            {
                builder.Append(argument);
                return;
            }

            builder.Append('"');
            for (int index = 0; index < argument.Length; index++)
            {
                switch (argument[index])
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default: builder.Append(argument[index]); break;
                }
            }

            builder.Append('"');
        }

        private static bool NeedsQuoting(string argument)
        {
            if (argument.Length == 0 ||
                !string.Equals(argument, argument.Trim(), StringComparison.Ordinal))
            {
                return true;
            }

            int depth = 0;
            for (int index = 0; index < argument.Length; index++)
            {
                char current = argument[index];
                if (current == '(') depth++;
                else if (current == ')') depth--;
                else if ((current == ',' || current == ';') && depth == 0) return true;
                else if (current == '{' || current == '}') return true;
                else if (current == '"' || current == '\\' || current == '\n' ||
                         current == '\r' || current == '\t') return true;
            }

            return false;
        }
    }
}
