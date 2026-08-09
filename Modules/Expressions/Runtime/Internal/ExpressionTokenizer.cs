using System.Collections.Generic;
using System.Globalization;

namespace KahaGameCore.Expressions.Internal
{
    internal sealed class ExpressionTokenizationResult
    {
        public ExpressionTokenizationResult(IReadOnlyList<ExpressionToken> tokens, ExpressionError error)
        {
            Tokens = tokens;
            Error = error;
        }

        public IReadOnlyList<ExpressionToken> Tokens { get; }
        public ExpressionError Error { get; }
        public bool IsSuccess => Error == null;
    }

    internal static class ExpressionTokenizer
    {
        public static ExpressionTokenizationResult Tokenize(string source)
        {
            List<ExpressionToken> tokens = new List<ExpressionToken>();
            int position = 0;
            while (position < source.Length)
            {
                if (char.IsWhiteSpace(source[position])) { position++; continue; }
                int start = position;
                char current = source[position++];
                switch (current)
                {
                    case '+': tokens.Add(Token(ExpressionTokenKind.Plus, source, start, position)); break;
                    case '-': tokens.Add(Token(ExpressionTokenKind.Minus, source, start, position)); break;
                    case '*': tokens.Add(Token(ExpressionTokenKind.Star, source, start, position)); break;
                    case '/': tokens.Add(Token(ExpressionTokenKind.Slash, source, start, position)); break;
                    case '(': tokens.Add(Token(ExpressionTokenKind.LeftParenthesis, source, start, position)); break;
                    case ')': tokens.Add(Token(ExpressionTokenKind.RightParenthesis, source, start, position)); break;
                    case ',': tokens.Add(Token(ExpressionTokenKind.Comma, source, start, position)); break;
                    case '>': tokens.Add(Token(Match(source, ref position, '=') ? ExpressionTokenKind.GreaterOrEqual : ExpressionTokenKind.Greater, source, start, position)); break;
                    case '<': tokens.Add(Token(Match(source, ref position, '=') ? ExpressionTokenKind.LessOrEqual : ExpressionTokenKind.Less, source, start, position)); break;
                    case '!': tokens.Add(Token(Match(source, ref position, '=') ? ExpressionTokenKind.NotEqual : ExpressionTokenKind.Not, source, start, position)); break;
                    case '=':
                        if (!Match(source, ref position, '=')) return Error("Expected '=='.", start, 1);
                        tokens.Add(Token(ExpressionTokenKind.Equal, source, start, position));
                        break;
                    case '&':
                        if (!Match(source, ref position, '&')) return Error("Expected '&&'.", start, 1);
                        tokens.Add(Token(ExpressionTokenKind.And, source, start, position));
                        break;
                    case '|':
                        if (!Match(source, ref position, '|')) return Error("Expected '||'.", start, 1);
                        tokens.Add(Token(ExpressionTokenKind.Or, source, start, position));
                        break;
                    case '$':
                        if (!ReadIdentifier(source, ref position, out string parameter)) return Error("Expected a parameter name after '$'.", start, 1);
                        tokens.Add(new ExpressionToken(ExpressionTokenKind.Parameter, parameter, start, position - start));
                        break;
                    default:
                        if (char.IsDigit(current) || current == '.')
                        {
                            while (position < source.Length && (char.IsDigit(source[position]) || source[position] == '.')) position++;
                            string text = source.Substring(start, position - start);
                            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float number))
                            {
                                return new ExpressionTokenizationResult(null, new ExpressionError(ExpressionErrorCode.InvalidNumber, $"Invalid number '{text}'.", start, text.Length));
                            }
                            tokens.Add(new ExpressionToken(ExpressionTokenKind.Number, text, start, text.Length, number));
                        }
                        else if (char.IsLetter(current) || current == '_')
                        {
                            position = start;
                            ReadIdentifier(source, ref position, out string identifier);
                            tokens.Add(new ExpressionToken(ExpressionTokenKind.Identifier, identifier, start, position - start));
                        }
                        else return Error($"Unexpected character '{current}'.", start, 1);
                        break;
                }
            }
            tokens.Add(new ExpressionToken(ExpressionTokenKind.End, string.Empty, source.Length, 0));
            return new ExpressionTokenizationResult(tokens, null);
        }

        private static bool ReadIdentifier(string source, ref int position, out string identifier)
        {
            int start = position;
            if (position >= source.Length || (!char.IsLetter(source[position]) && source[position] != '_'))
            {
                identifier = null;
                return false;
            }
            position++;
            while (position < source.Length && (char.IsLetterOrDigit(source[position]) || source[position] == '_' || source[position] == '.')) position++;
            identifier = source.Substring(start, position - start);
            return true;
        }

        private static bool Match(string source, ref int position, char expected)
        {
            if (position >= source.Length || source[position] != expected) return false;
            position++;
            return true;
        }

        private static ExpressionToken Token(ExpressionTokenKind kind, string source, int start, int end)
        {
            return new ExpressionToken(kind, source.Substring(start, end - start), start, end - start);
        }

        private static ExpressionTokenizationResult Error(string message, int position, int length)
        {
            return new ExpressionTokenizationResult(null, new ExpressionError(ExpressionErrorCode.UnexpectedToken, message, position, length));
        }
    }
}
