using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;
using KahaGameCore.Expressions;

namespace KahaGameCore.Parameters.EffectsIntegration
{
    public static class ParameterEffectCommandRegistrar
    {
        private static readonly EffectCommandDescriptor AddDescriptor = Describe(
            "AddParameter", EffectCommandParameterKind.NumberExpression);
        private static readonly EffectCommandDescriptor SetDescriptor = Describe(
            "SetParameter", EffectCommandParameterKind.Literal);
        private static readonly IReadOnlyList<EffectCommandDescriptor> descriptors =
            Array.AsReadOnly(new[] { AddDescriptor, SetDescriptor });

        public static IReadOnlyList<EffectCommandDescriptor> Descriptors => descriptors;

        public static void RegisterAll(EffectCommandRegistry registry, ParameterStore parameters)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            registry.Register(new EffectCommandDefinition(AddDescriptor, new AddParameterCommand(parameters)));
            registry.Register(new EffectCommandDefinition(SetDescriptor, new SetParameterCommand(parameters)));
        }

        private static EffectCommandDescriptor Describe(
            string name,
            EffectCommandParameterKind valueKind)
        {
            return new EffectCommandDescriptor(
                name,
                name,
                "Parameters",
                new[]
                {
                    new EffectCommandParameterDefinition(
                        "key", EffectCommandParameterKind.ParameterKey),
                    new EffectCommandParameterDefinition("value", valueKind)
                });
        }

        internal static float Calculate(ParameterStore parameters, string formula)
        {
            ExpressionResult<float> result = parameters.Calculate(formula);
            if (!result.IsSuccess)
                throw new InvalidOperationException(result.Error.ToString());
            return result.Value;
        }

    }

    public sealed class AddParameterCommand : IEffectCommand
    {
            private readonly ParameterStore parameters;
            public AddParameterCommand(ParameterStore parameters)
            {
                this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            }

            public UniTask ExecuteAsync(
                EffectExecutionContext context,
                IReadOnlyList<string> arguments,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!parameters.TryGetValue(arguments[0], out ParameterValue current))
                    throw new UnknownParameterException(arguments[0]);
                float value = ParameterEffectCommandRegistrar.Calculate(parameters, arguments[1]);
                if (current.Type == ParameterType.Int)
                    parameters.Add(arguments[0], (int)Math.Round(value));
                else if (current.Type == ParameterType.Float)
                    parameters.Add(arguments[0], value);
                else
                    throw new InvalidOperationException(
                        $"AddParameter does not support {current.Type} parameter '{arguments[0]}'.");
                return UniTask.CompletedTask;
            }
    }

    public sealed class SetParameterCommand : IEffectCommand
    {
            private readonly ParameterStore parameters;
            public SetParameterCommand(ParameterStore parameters)
            {
                this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            }

            public UniTask ExecuteAsync(
                EffectExecutionContext context,
                IReadOnlyList<string> arguments,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!parameters.TryGetValue(arguments[0], out ParameterValue current))
                    throw new UnknownParameterException(arguments[0]);
                switch (current.Type)
                {
                    case ParameterType.Int:
                        parameters.Set(arguments[0], (int)Math.Round(ParameterEffectCommandRegistrar.Calculate(parameters, arguments[1])));
                        break;
                    case ParameterType.Float:
                        parameters.Set(arguments[0], ParameterEffectCommandRegistrar.Calculate(parameters, arguments[1]));
                        break;
                    case ParameterType.Bool:
                        ExpressionResult<bool> result = parameters.EvaluateCondition(arguments[1]);
                        if (!result.IsSuccess)
                            throw new InvalidOperationException(result.Error.ToString());
                        parameters.Set(arguments[0], result.Value);
                        break;
                    case ParameterType.String:
                        parameters.Set(arguments[0], arguments[1]);
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"SetParameter does not support {current.Type} parameter '{arguments[0]}'.");
                }
                return UniTask.CompletedTask;
            }
    }

    /* Command implementations above are public runtime pieces; the provider below is editor metadata. */
    public sealed class ParameterEffectCommandDescriptorProvider :
        IEffectCommandDescriptorProvider
    {
        public IReadOnlyList<EffectCommandDescriptor> GetDescriptors()
        {
            return ParameterEffectCommandRegistrar.Descriptors;
        }
    }
}
