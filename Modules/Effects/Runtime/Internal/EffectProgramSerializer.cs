using System;
using System.Text;

namespace KahaGameCore.Effects.Internal
{
    internal static class EffectProgramSerializer
    {
        internal static string Serialize(EffectProgram program)
        {
            if (program == null) throw new ArgumentNullException(nameof(program));

            StringBuilder builder = new StringBuilder();
            for (int blockIndex = 0; blockIndex < program.Blocks.Count; blockIndex++)
            {
                EffectTimingBlock block = program.Blocks[blockIndex];
                if (program.UsesExplicitTimings)
                {
                    builder.Append(block.Name).Append('{');
                }

                for (int commandIndex = 0; commandIndex < block.Commands.Count; commandIndex++)
                {
                    EffectCommandCall command = block.Commands[commandIndex];
                    builder.Append(command.Name).Append('(');
                    for (int argumentIndex = 0;
                         argumentIndex < command.Arguments.Count;
                         argumentIndex++)
                    {
                        if (argumentIndex > 0)
                        {
                            builder.Append(',');
                        }

                        AppendArgument(builder, command.Arguments[argumentIndex]);
                    }

                    builder.Append(");");
                }

                if (program.UsesExplicitTimings)
                {
                    builder.Append('}');
                }
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
