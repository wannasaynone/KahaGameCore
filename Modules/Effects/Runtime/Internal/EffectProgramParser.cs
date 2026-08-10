using System;
using System.Collections.Generic;
using System.Text;

namespace KahaGameCore.Effects.Internal
{
    internal static class EffectProgramParser
    {
        internal static EffectParseResult Parse(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return EffectParseResult.Success(
                    new EffectProgram(false, new[]
                    {
                        new EffectTimingBlock(
                            EffectRuntime.DefaultTiming,
                            Array.Empty<EffectCommandCall>())
                    }));
            }

            if (ContainsUnquoted(source, '{'))
            {
                return ParseExplicitTimings(source);
            }

            EffectParseResult flatResult = ParseFlatSequence(source, 0);
            if (!flatResult.IsSuccess)
            {
                return flatResult;
            }

            return EffectParseResult.Success(
                new EffectProgram(false, new[]
                {
                    new EffectTimingBlock(
                        EffectRuntime.DefaultTiming,
                        flatResult.Program.Blocks[0].Commands)
                }));
        }

        private static EffectParseResult ParseExplicitTimings(string source)
        {
            List<EffectTimingBlock> blocks = new List<EffectTimingBlock>();
            int index = 0;
            while (index < source.Length)
            {
                SkipWhitespace(source, ref index);
                if (index >= source.Length)
                {
                    break;
                }

                int nameStart = index;
                while (index < source.Length && source[index] != '{')
                {
                    index++;
                }

                if (index >= source.Length)
                {
                    return Failure(
                        "ExpectedTimingBlock",
                        "Expected timing block opening brace.",
                        nameStart,
                        Math.Max(1, source.Length - nameStart));
                }

                string name = source.Substring(nameStart, index - nameStart).Trim();
                if (name.Length == 0)
                {
                    return Failure("ExpectedTimingName", "Timing name is required.", nameStart, 1);
                }

                int bodyStart = ++index;
                int closeIndex = FindUnquoted(source, '}', bodyStart);
                if (closeIndex < 0)
                {
                    return Failure(
                        "UnterminatedTimingBlock",
                        $"Timing block '{name}' is not terminated.",
                        bodyStart - 1,
                        1);
                }

                string body = source.Substring(bodyStart, closeIndex - bodyStart);
                EffectParseResult bodyResult = ParseFlatSequence(body, bodyStart);
                if (!bodyResult.IsSuccess)
                {
                    return bodyResult;
                }

                blocks.Add(new EffectTimingBlock(name, bodyResult.Program.Blocks[0].Commands));
                index = closeIndex + 1;
            }

            return EffectParseResult.Success(new EffectProgram(true, blocks));
        }

        private static EffectParseResult ParseFlatSequence(string source, int offset)
        {
            List<EffectCommandCall> commands = new List<EffectCommandCall>();
            int commandStart = 0;
            int parenthesisDepth = 0;
            bool inQuote = false;
            bool escaping = false;

            for (int index = 0; index <= source.Length; index++)
            {
                char current = index < source.Length ? source[index] : ';';
                if (inQuote)
                {
                    if (escaping) escaping = false;
                    else if (current == '\\') escaping = true;
                    else if (current == '"') inQuote = false;
                    continue;
                }

                if (current == '"')
                {
                    inQuote = true;
                    continue;
                }

                if (current == '(')
                {
                    parenthesisDepth++;
                    continue;
                }

                if (current == ')')
                {
                    parenthesisDepth--;
                    if (parenthesisDepth < 0)
                    {
                        return Failure(
                            "UnexpectedCloseParenthesis",
                            "Unexpected closing parenthesis.",
                            offset + index,
                            1);
                    }

                    continue;
                }

                if (current != ';' || parenthesisDepth != 0)
                {
                    continue;
                }

                string commandSource = source.Substring(commandStart, index - commandStart);
                if (!string.IsNullOrWhiteSpace(commandSource))
                {
                    EffectParseResult commandResult = ParseCommand(
                        commandSource,
                        offset + commandStart);
                    if (!commandResult.IsSuccess)
                    {
                        return commandResult;
                    }

                    commands.Add(commandResult.Program.Blocks[0].Commands[0]);
                }

                commandStart = index + 1;
            }

            if (inQuote)
            {
                return Failure(
                    "UnterminatedQuote",
                    "Quoted argument is not terminated.",
                    offset + source.Length - 1,
                    1);
            }

            if (parenthesisDepth != 0)
            {
                return Failure(
                    "UnterminatedParenthesis",
                    "Command argument list is not terminated.",
                    offset + source.Length,
                    0);
            }

            return EffectParseResult.Success(
                new EffectProgram(false, new[]
                {
                    new EffectTimingBlock(EffectRuntime.DefaultTiming, commands)
                }));
        }

        private static EffectParseResult ParseCommand(string source, int offset)
        {
            int openIndex = FindFirstUnquoted(source, '(');
            if (openIndex <= 0)
            {
                return Failure(
                    "ExpectedCommand",
                    "Expected CommandName(...).",
                    offset,
                    Math.Max(1, source.Length));
            }

            string name = source.Substring(0, openIndex).Trim();
            if (name.Length == 0)
            {
                return Failure(
                    "ExpectedCommandName",
                    "Command name is required.",
                    offset,
                    Math.Max(1, openIndex));
            }

            int closeIndex = FindMatchingCloseParenthesis(source, openIndex);
            if (closeIndex < 0)
            {
                return Failure(
                    "UnterminatedParenthesis",
                    "Command argument list is not terminated.",
                    offset + openIndex,
                    1);
            }

            if (!string.IsNullOrWhiteSpace(source.Substring(closeIndex + 1)))
            {
                return Failure(
                    "UnexpectedTrailingText",
                    "Unexpected text after command argument list.",
                    offset + closeIndex + 1,
                    source.Length - closeIndex - 1);
            }

            string argumentsSource = source.Substring(openIndex + 1, closeIndex - openIndex - 1);
            List<string> arguments = new List<string>();
            if (argumentsSource.Length > 0)
            {
                EffectDiagnostic diagnostic = SplitArguments(
                    argumentsSource,
                    offset + openIndex + 1,
                    arguments);
                if (diagnostic != null)
                {
                    return EffectParseResult.Failure(diagnostic);
                }
            }

            EffectCommandCall call = new EffectCommandCall(name, arguments, offset, source.Length);
            return EffectParseResult.Success(
                new EffectProgram(false, new[]
                {
                    new EffectTimingBlock(EffectRuntime.DefaultTiming, new[] { call })
                }));
        }

        private static EffectDiagnostic SplitArguments(
            string source,
            int offset,
            List<string> arguments)
        {
            int start = 0;
            int depth = 0;
            bool inQuote = false;
            bool escaping = false;
            for (int index = 0; index <= source.Length; index++)
            {
                char current = index < source.Length ? source[index] : ',';
                if (inQuote)
                {
                    if (escaping) escaping = false;
                    else if (current == '\\') escaping = true;
                    else if (current == '"') inQuote = false;
                    continue;
                }

                if (current == '"')
                {
                    inQuote = true;
                    continue;
                }

                if (current == '(')
                {
                    depth++;
                    continue;
                }

                if (current == ')')
                {
                    depth--;
                    if (depth < 0)
                    {
                        return new EffectDiagnostic(
                            "UnexpectedCloseParenthesis",
                            "Unexpected closing parenthesis in argument.",
                            offset + index,
                            1);
                    }

                    continue;
                }

                if (current != ',' || depth != 0)
                {
                    continue;
                }

                string rawArgument = source.Substring(start, index - start).Trim();
                arguments.Add(Unquote(rawArgument));
                start = index + 1;
            }

            if (inQuote)
            {
                return new EffectDiagnostic(
                    "UnterminatedQuote",
                    "Quoted argument is not terminated.",
                    offset + source.Length - 1,
                    1);
            }

            return null;
        }

        private static string Unquote(string value)
        {
            if (value.Length < 2 || value[0] != '"' || value[value.Length - 1] != '"')
            {
                return value;
            }

            StringBuilder builder = new StringBuilder();
            bool escaping = false;
            for (int index = 1; index < value.Length - 1; index++)
            {
                char current = value[index];
                if (!escaping)
                {
                    if (current == '\\') escaping = true;
                    else builder.Append(current);
                    continue;
                }

                switch (current)
                {
                    case 'n': builder.Append('\n'); break;
                    case 'r': builder.Append('\r'); break;
                    case 't': builder.Append('\t'); break;
                    default: builder.Append(current); break;
                }

                escaping = false;
            }

            if (escaping)
            {
                builder.Append('\\');
            }

            return builder.ToString();
        }

        private static int FindFirstUnquoted(string source, char target)
        {
            bool inQuote = false;
            bool escaping = false;
            for (int index = 0; index < source.Length; index++)
            {
                char current = source[index];
                if (inQuote)
                {
                    if (escaping) escaping = false;
                    else if (current == '\\') escaping = true;
                    else if (current == '"') inQuote = false;
                    continue;
                }

                if (current == '"') inQuote = true;
                else if (current == target) return index;
            }

            return -1;
        }

        private static bool ContainsUnquoted(string source, char target)
        {
            return FindUnquoted(source, target, 0) >= 0;
        }

        private static int FindUnquoted(string source, char target, int startIndex)
        {
            bool inQuote = false;
            bool escaping = false;
            for (int index = startIndex; index < source.Length; index++)
            {
                char current = source[index];
                if (inQuote)
                {
                    if (escaping) escaping = false;
                    else if (current == '\\') escaping = true;
                    else if (current == '"') inQuote = false;
                    continue;
                }

                if (current == '"') inQuote = true;
                else if (current == target) return index;
            }

            return -1;
        }

        private static void SkipWhitespace(string source, ref int index)
        {
            while (index < source.Length && char.IsWhiteSpace(source[index]))
            {
                index++;
            }
        }

        private static int FindMatchingCloseParenthesis(string source, int openIndex)
        {
            int depth = 0;
            bool inQuote = false;
            bool escaping = false;
            for (int index = openIndex; index < source.Length; index++)
            {
                char current = source[index];
                if (inQuote)
                {
                    if (escaping) escaping = false;
                    else if (current == '\\') escaping = true;
                    else if (current == '"') inQuote = false;
                    continue;
                }

                if (current == '"') inQuote = true;
                else if (current == '(') depth++;
                else if (current == ')' && --depth == 0) return index;
            }

            return -1;
        }

        private static EffectParseResult Failure(
            string code,
            string message,
            int position,
            int length)
        {
            return EffectParseResult.Failure(
                new EffectDiagnostic(code, message, position, length));
        }
    }
}
