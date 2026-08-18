using System;
using System.Collections.Generic;
using System.Text;

namespace KahaGameCore.GameEvents.Editor
{
    internal enum GameEventConditionGroupMode
    {
        All,
        Any
    }

    internal abstract class GameEventConditionDraft
    {
    }

    internal sealed class GameEventConditionClauseDraft : GameEventConditionDraft
    {
        public string ParameterKey;
        public string Operator = "==";
        public string Value = "0";
    }

    internal sealed class GameEventConditionGroupDraft : GameEventConditionDraft
    {
        public GameEventConditionGroupMode Mode = GameEventConditionGroupMode.All;
        public List<GameEventConditionDraft> Children =
            new List<GameEventConditionDraft>();
    }

    internal static class GameEventConditionDraftCodec
    {
        public static GameEventConditionGroupDraft Parse(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return new GameEventConditionGroupDraft();
            }

            Parser parser = new Parser(source);
            GameEventConditionDraft expression = parser.ParseExpression();
            parser.RequireEnd();
            if (expression is GameEventConditionGroupDraft group)
            {
                return group;
            }

            GameEventConditionGroupDraft root = new GameEventConditionGroupDraft();
            root.Children.Add(expression);
            return root;
        }

        public static string Serialize(GameEventConditionGroupDraft root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (root.Children.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            AppendGroup(builder, root, true);
            return builder.ToString();
        }

        private static void AppendGroup(
            StringBuilder builder,
            GameEventConditionGroupDraft group,
            bool isRoot)
        {
            if (group.Children.Count == 0)
            {
                throw new InvalidOperationException(
                    "A nested Condition Group must contain at least one condition.");
            }

            if (!isRoot)
            {
                builder.Append('(');
            }

            string connector = group.Mode == GameEventConditionGroupMode.Any
                ? " || "
                : " && ";
            for (int index = 0; index < group.Children.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(connector);
                }

                AppendNode(builder, group.Children[index]);
            }

            if (!isRoot)
            {
                builder.Append(')');
            }
        }

        private static void AppendNode(
            StringBuilder builder,
            GameEventConditionDraft draft)
        {
            if (draft is GameEventConditionClauseDraft clause)
            {
                AppendClause(builder, clause);
                return;
            }

            if (draft is GameEventConditionGroupDraft group)
            {
                AppendGroup(builder, group, false);
                return;
            }

            throw new InvalidOperationException("Condition contains an unknown row type.");
        }

        private static void AppendClause(
            StringBuilder builder,
            GameEventConditionClauseDraft draft)
        {
            if (string.IsNullOrWhiteSpace(draft.ParameterKey))
            {
                throw new InvalidOperationException(
                    "A Condition row has no Parameter selected.");
            }

            bool isBoolean = string.Equals(
                    draft.Value,
                    "true",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    draft.Value,
                    "false",
                    StringComparison.OrdinalIgnoreCase);
            if (isBoolean && (draft.Operator == "==" || draft.Operator == "!="))
            {
                bool value = string.Equals(
                    draft.Value,
                    "true",
                    StringComparison.OrdinalIgnoreCase);
                bool expected = draft.Operator == "==" ? value : !value;
                if (!expected)
                {
                    builder.Append('!');
                }

                builder.Append('$').Append(draft.ParameterKey);
                return;
            }

            if (!IsComparisonOperator(draft.Operator))
            {
                throw new InvalidOperationException(
                    $"Condition for '{draft.ParameterKey}' has an invalid comparison.");
            }

            if (string.IsNullOrWhiteSpace(draft.Value))
            {
                throw new InvalidOperationException(
                    $"Condition for '{draft.ParameterKey}' requires a value.");
            }

            builder
                .Append('$')
                .Append(draft.ParameterKey)
                .Append(draft.Operator)
                .Append(draft.Value);
        }

        private static bool IsComparisonOperator(string value)
        {
            return value == "==" || value == "!=" || value == ">" ||
                value == ">=" || value == "<" || value == "<=";
        }

        private sealed class Parser
        {
            private readonly string source;
            private int position;

            public Parser(string source)
            {
                this.source = source;
            }

            public GameEventConditionDraft ParseExpression()
            {
                return ParseOr();
            }

            public void RequireEnd()
            {
                SkipWhitespace();
                if (position != source.Length)
                {
                    throw UnsupportedCondition();
                }
            }

            private GameEventConditionDraft ParseOr()
            {
                List<GameEventConditionDraft> terms =
                    new List<GameEventConditionDraft> { ParseAnd() };
                while (TryRead("||"))
                {
                    terms.Add(ParseAnd());
                }

                return Combine(GameEventConditionGroupMode.Any, terms);
            }

            private GameEventConditionDraft ParseAnd()
            {
                List<GameEventConditionDraft> terms =
                    new List<GameEventConditionDraft> { ParsePrimary() };
                while (TryRead("&&"))
                {
                    terms.Add(ParsePrimary());
                }

                return Combine(GameEventConditionGroupMode.All, terms);
            }

            private GameEventConditionDraft ParsePrimary()
            {
                SkipWhitespace();
                if (TryRead("("))
                {
                    GameEventConditionDraft nested = ParseExpression();
                    if (!TryRead(")"))
                    {
                        throw UnsupportedCondition();
                    }

                    return nested;
                }

                return ParseClause();
            }

            private GameEventConditionClauseDraft ParseClause()
            {
                SkipWhitespace();
                bool negate = TryRead("!");
                if (!TryRead("$"))
                {
                    throw UnsupportedCondition();
                }

                string parameterKey = ReadIdentifier();
                if (string.IsNullOrEmpty(parameterKey))
                {
                    throw UnsupportedCondition();
                }

                string comparison = ReadComparisonOperator();
                string value;
                if (string.IsNullOrEmpty(comparison))
                {
                    comparison = "==";
                    value = negate ? "false" : "true";
                }
                else
                {
                    if (negate)
                    {
                        throw UnsupportedCondition();
                    }

                    value = ReadLiteral();
                    if (string.IsNullOrEmpty(value))
                    {
                        throw UnsupportedCondition();
                    }
                }

                return new GameEventConditionClauseDraft
                {
                    ParameterKey = parameterKey,
                    Operator = comparison,
                    Value = value
                };
            }

            private string ReadIdentifier()
            {
                SkipWhitespace();
                int start = position;
                if (position >= source.Length ||
                    (!char.IsLetter(source[position]) && source[position] != '_'))
                {
                    return string.Empty;
                }

                position++;
                while (position < source.Length &&
                       (char.IsLetterOrDigit(source[position]) ||
                        source[position] == '_' || source[position] == '.'))
                {
                    position++;
                }

                return source.Substring(start, position - start);
            }

            private string ReadComparisonOperator()
            {
                SkipWhitespace();
                string[] operators = { "==", "!=", ">=", "<=", ">", "<" };
                for (int index = 0; index < operators.Length; index++)
                {
                    if (TryRead(operators[index]))
                    {
                        return operators[index];
                    }
                }

                return string.Empty;
            }

            private string ReadLiteral()
            {
                SkipWhitespace();
                int start = position;
                if (StartsWith("true"))
                {
                    position += "true".Length;
                }
                else if (StartsWith("false"))
                {
                    position += "false".Length;
                }
                else
                {
                    if (position < source.Length && source[position] == '-')
                    {
                        position++;
                    }

                    bool hasDigit = false;
                    bool hasDecimalPoint = false;
                    while (position < source.Length)
                    {
                        char current = source[position];
                        if (char.IsDigit(current))
                        {
                            hasDigit = true;
                            position++;
                            continue;
                        }

                        if (current == '.' && !hasDecimalPoint)
                        {
                            hasDecimalPoint = true;
                            position++;
                            continue;
                        }

                        break;
                    }

                    if (!hasDigit)
                    {
                        position = start;
                        return string.Empty;
                    }
                }

                return source.Substring(start, position - start);
            }

            private bool TryRead(string value)
            {
                SkipWhitespace();
                if (!StartsWith(value))
                {
                    return false;
                }

                position += value.Length;
                return true;
            }

            private bool StartsWith(string value)
            {
                return position + value.Length <= source.Length &&
                    string.CompareOrdinal(
                        source,
                        position,
                        value,
                        0,
                        value.Length) == 0;
            }

            private void SkipWhitespace()
            {
                while (position < source.Length && char.IsWhiteSpace(source[position]))
                {
                    position++;
                }
            }

            private static GameEventConditionDraft Combine(
                GameEventConditionGroupMode mode,
                List<GameEventConditionDraft> terms)
            {
                if (terms.Count == 1)
                {
                    return terms[0];
                }

                GameEventConditionGroupDraft group = new GameEventConditionGroupDraft
                {
                    Mode = mode
                };
                for (int index = 0; index < terms.Count; index++)
                {
                    if (terms[index] is GameEventConditionGroupDraft nested &&
                        nested.Mode == mode)
                    {
                        group.Children.AddRange(nested.Children);
                    }
                    else
                    {
                        group.Children.Add(terms[index]);
                    }
                }

                return group;
            }

            private static InvalidOperationException UnsupportedCondition()
            {
                return new InvalidOperationException(
                    "This condition uses expression syntax that the structured editor does not support.");
            }
        }
    }
}
