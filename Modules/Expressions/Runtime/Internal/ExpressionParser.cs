using System.Collections.Generic;

namespace KahaGameCore.Expressions.Internal
{
    internal sealed class ExpressionParseResult
    {
        public ExpressionParseResult(ExpressionSyntax root, ExpressionError error) { Root = root; Error = error; }
        public ExpressionSyntax Root { get; }
        public ExpressionError Error { get; }
        public bool IsSuccess => Error == null;
    }

    internal sealed class ExpressionParser
    {
        private readonly IReadOnlyList<ExpressionToken> tokens;
        private int index;
        private ExpressionError error;

        private ExpressionParser(IReadOnlyList<ExpressionToken> tokens) { this.tokens = tokens; }

        public static ExpressionParseResult Parse(string source)
        {
            ExpressionTokenizationResult tokenized = ExpressionTokenizer.Tokenize(source);
            if (!tokenized.IsSuccess) return new ExpressionParseResult(null, tokenized.Error);
            ExpressionParser parser = new ExpressionParser(tokenized.Tokens);
            ExpressionSyntax root = parser.ParseOr();
            if (parser.error != null) return new ExpressionParseResult(null, parser.error);
            if (!parser.Check(ExpressionTokenKind.End))
            {
                ExpressionToken token = parser.Current;
                return new ExpressionParseResult(null, new ExpressionError(ExpressionErrorCode.UnexpectedToken, "Unexpected token.", token.Position, token.Length));
            }
            return new ExpressionParseResult(root, null);
        }

        private ExpressionSyntax ParseOr() => ParseBinary(ParseAnd, ExpressionTokenKind.Or);
        private ExpressionSyntax ParseAnd() => ParseBinary(ParseEquality, ExpressionTokenKind.And);
        private ExpressionSyntax ParseEquality() => ParseBinary(ParseComparison, ExpressionTokenKind.Equal, ExpressionTokenKind.NotEqual);
        private ExpressionSyntax ParseComparison() => ParseBinary(ParseAddition, ExpressionTokenKind.Greater, ExpressionTokenKind.GreaterOrEqual, ExpressionTokenKind.Less, ExpressionTokenKind.LessOrEqual);
        private ExpressionSyntax ParseAddition() => ParseBinary(ParseMultiplication, ExpressionTokenKind.Plus, ExpressionTokenKind.Minus);
        private ExpressionSyntax ParseMultiplication() => ParseBinary(ParseUnary, ExpressionTokenKind.Star, ExpressionTokenKind.Slash);

        private delegate ExpressionSyntax ParseOperand();
        private ExpressionSyntax ParseBinary(ParseOperand parseOperand, params ExpressionTokenKind[] operations)
        {
            ExpressionSyntax value = parseOperand();
            while (error == null && Match(out ExpressionToken operation, operations))
            {
                ExpressionSyntax right = parseOperand();
                if (right == null) return null;
                value = new BinaryExpressionSyntax(value, operation, right);
            }
            return value;
        }

        private ExpressionSyntax ParseUnary()
        {
            if (Match(out ExpressionToken operation, ExpressionTokenKind.Minus, ExpressionTokenKind.Not))
            {
                ExpressionSyntax operand = ParseUnary();
                return operand == null ? null : new UnaryExpressionSyntax(operation.Kind, operand, operation.Position, operation.Length);
            }
            return ParsePrimary();
        }

        private ExpressionSyntax ParsePrimary()
        {
            if (Match(out ExpressionToken number, ExpressionTokenKind.Number)) return new LiteralExpressionSyntax(ExpressionValue.FromNumber(number.Number), number.Position, number.Length);
            if (Match(out ExpressionToken parameter, ExpressionTokenKind.Parameter)) return new SymbolExpressionSyntax(parameter.Text, parameter.Position, parameter.Length);
            if (Match(out ExpressionToken identifier, ExpressionTokenKind.Identifier))
            {
                if (identifier.Text == "true") return new LiteralExpressionSyntax(ExpressionValue.FromBoolean(true), identifier.Position, identifier.Length);
                if (identifier.Text == "false") return new LiteralExpressionSyntax(ExpressionValue.FromBoolean(false), identifier.Position, identifier.Length);
                if (Match(out _, ExpressionTokenKind.LeftParenthesis)) return ParseFunction(identifier);
                return new SymbolExpressionSyntax(identifier.Text, identifier.Position, identifier.Length);
            }
            if (Match(out _, ExpressionTokenKind.LeftParenthesis))
            {
                ExpressionSyntax nested = ParseOr();
                if (!Match(out _, ExpressionTokenKind.RightParenthesis)) SetExpected("Expected ')'.");
                return nested;
            }
            SetExpected("Expected a value.");
            return null;
        }

        private ExpressionSyntax ParseFunction(ExpressionToken name)
        {
            List<ExpressionSyntax> arguments = new List<ExpressionSyntax>();
            if (!Check(ExpressionTokenKind.RightParenthesis))
            {
                do
                {
                    ExpressionSyntax argument = ParseOr();
                    if (argument == null) return null;
                    arguments.Add(argument);
                } while (Match(out _, ExpressionTokenKind.Comma));
            }
            if (!Match(out ExpressionToken closing, ExpressionTokenKind.RightParenthesis))
            {
                SetExpected("Expected ')'.");
                return null;
            }
            return new FunctionExpressionSyntax(name.Text, arguments, name.Position, closing.Position + closing.Length - name.Position);
        }

        private bool Match(out ExpressionToken matched, params ExpressionTokenKind[] kinds)
        {
            foreach (ExpressionTokenKind kind in kinds)
            {
                if (!Check(kind)) continue;
                matched = tokens[index++];
                return true;
            }
            matched = default;
            return false;
        }

        private bool Check(ExpressionTokenKind kind) => Current.Kind == kind;
        private ExpressionToken Current => tokens[index];
        private void SetExpected(string message)
        {
            if (error != null) return;
            error = new ExpressionError(ExpressionErrorCode.UnexpectedToken, message, Current.Position, Current.Length);
        }
    }
}
