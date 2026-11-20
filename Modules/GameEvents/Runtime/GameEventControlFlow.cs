using System;
using System.Collections.Generic;
using KahaGameCore.Effects;

namespace KahaGameCore.GameEvents
{
    /// <summary>
    /// Built-in commands that steer a Game Event's flat command list instead of
    /// doing work: <c>If(condition)</c> … <c>Else()</c> … <c>EndIf()</c> and
    /// <c>Quit()</c>. GameEventRunner interprets them; they are never registered
    /// in EffectRuntime, and a Catalog does not need to enable them.
    /// </summary>
    public static class GameEventControlFlow
    {
        public const string Category = "Flow";
        public const string IfCommandName = "If";
        public const string ElseCommandName = "Else";
        public const string EndIfCommandName = "EndIf";
        public const string QuitCommandName = "Quit";

        public static readonly EffectCommandDescriptor If = new EffectCommandDescriptor(
            IfCommandName,
            "If",
            Category,
            new[]
            {
                new EffectCommandParameterDefinition(
                    "condition",
                    EffectCommandParameterKind.ConditionExpression)
            });

        public static readonly EffectCommandDescriptor Else = new EffectCommandDescriptor(
            ElseCommandName,
            "Else",
            Category,
            Array.Empty<EffectCommandParameterDefinition>());

        public static readonly EffectCommandDescriptor EndIf = new EffectCommandDescriptor(
            EndIfCommandName,
            "End If",
            Category,
            Array.Empty<EffectCommandParameterDefinition>());

        public static readonly EffectCommandDescriptor Quit = new EffectCommandDescriptor(
            QuitCommandName,
            "Quit Event",
            Category,
            Array.Empty<EffectCommandParameterDefinition>());

        public static readonly IReadOnlyList<EffectCommandDescriptor> Descriptors =
            Array.AsReadOnly(new[] { If, Else, EndIf, Quit });

        public static bool IsControlFlowCommand(string commandName)
        {
            return TryGetDescriptor(commandName, out EffectCommandDescriptor _);
        }

        public static bool TryGetDescriptor(
            string commandName,
            out EffectCommandDescriptor descriptor)
        {
            for (int index = 0; index < Descriptors.Count; index++)
            {
                if (string.Equals(Descriptors[index].Name, commandName, StringComparison.Ordinal))
                {
                    descriptor = Descriptors[index];
                    return true;
                }
            }

            descriptor = null;
            return false;
        }

        /// <summary>
        /// Pairs every If with its Else and EndIf. Always returns a result, even
        /// for a broken list, so an editor can still indent rows and point at
        /// the row that is wrong.
        /// </summary>
        public static GameEventCommandStructure Analyze(IReadOnlyList<string> commandNames)
        {
            if (commandNames == null) throw new ArgumentNullException(nameof(commandNames));

            int count = commandNames.Count;
            int[] depths = new int[count];
            int[] jumps = new int[count];
            GameEventControlFlowError[] errors = new GameEventControlFlowError[count];
            Stack<int> openIfs = new Stack<int>();
            Dictionary<int, int> elseByIf = new Dictionary<int, int>();

            for (int index = 0; index < count; index++)
            {
                jumps[index] = -1;
                string name = commandNames[index];
                if (string.Equals(name, IfCommandName, StringComparison.Ordinal))
                {
                    depths[index] = openIfs.Count;
                    openIfs.Push(index);
                }
                else if (string.Equals(name, ElseCommandName, StringComparison.Ordinal))
                {
                    if (openIfs.Count == 0)
                    {
                        depths[index] = 0;
                        errors[index] = GameEventControlFlowError.ElseWithoutIf;
                        continue;
                    }

                    int ifIndex = openIfs.Peek();
                    depths[index] = openIfs.Count - 1;
                    if (elseByIf.ContainsKey(ifIndex))
                    {
                        errors[index] = GameEventControlFlowError.DuplicateElse;
                        continue;
                    }

                    elseByIf.Add(ifIndex, index);
                    jumps[ifIndex] = index;
                }
                else if (string.Equals(name, EndIfCommandName, StringComparison.Ordinal))
                {
                    if (openIfs.Count == 0)
                    {
                        depths[index] = 0;
                        errors[index] = GameEventControlFlowError.EndIfWithoutIf;
                        continue;
                    }

                    int ifIndex = openIfs.Pop();
                    depths[index] = openIfs.Count;
                    if (elseByIf.TryGetValue(ifIndex, out int elseIndex))
                    {
                        jumps[elseIndex] = index;
                    }
                    else
                    {
                        jumps[ifIndex] = index;
                    }
                }
                else
                {
                    depths[index] = openIfs.Count;
                }
            }

            foreach (int unclosedIf in openIfs)
            {
                errors[unclosedIf] = GameEventControlFlowError.IfWithoutEndIf;
            }

            return new GameEventCommandStructure(depths, jumps, errors);
        }
    }

    public enum GameEventControlFlowError
    {
        None,
        ElseWithoutIf,
        DuplicateElse,
        EndIfWithoutIf,
        IfWithoutEndIf
    }

    public sealed class GameEventCommandStructure
    {
        private readonly int[] depths;
        private readonly int[] jumps;
        private readonly GameEventControlFlowError[] errors;

        internal GameEventCommandStructure(
            int[] depths,
            int[] jumps,
            GameEventControlFlowError[] errors)
        {
            this.depths = depths;
            this.jumps = jumps;
            this.errors = errors;
            FirstErrorIndex = Array.FindIndex(
                errors,
                error => error != GameEventControlFlowError.None);
        }

        public int Count => depths.Length;
        public bool IsValid => FirstErrorIndex < 0;
        public int FirstErrorIndex { get; }

        /// <summary>
        /// How many If blocks enclose the row. If, Else and EndIf rows sit at
        /// the depth of the block they open or close.
        /// </summary>
        public int GetDepth(int index)
        {
            return depths[index];
        }

        public GameEventControlFlowError GetErrorCode(int index)
        {
            return errors[index];
        }

        /// <summary>The row's structural error, or null.</summary>
        public string GetError(int index)
        {
            switch (errors[index])
            {
                case GameEventControlFlowError.ElseWithoutIf: return "Else has no matching If.";
                case GameEventControlFlowError.DuplicateElse: return "This If already has an Else.";
                case GameEventControlFlowError.EndIfWithoutIf: return "EndIf has no matching If.";
                case GameEventControlFlowError.IfWithoutEndIf: return "If has no matching EndIf.";
                default: return null;
            }
        }

        /// <summary>
        /// For an If, its Else (or EndIf when there is no Else); for an Else,
        /// its EndIf. Execution resumes after the returned row. -1 otherwise.
        /// </summary>
        public int GetJumpTarget(int index)
        {
            return jumps[index];
        }
    }
}
