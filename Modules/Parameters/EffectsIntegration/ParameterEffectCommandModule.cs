using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;
using KahaGameCore.Expressions;
using UnityEngine.Scripting;

namespace KahaGameCore.Parameters.EffectsIntegration
{
    internal static class ParameterEffectCommandManifest
    {
        public static readonly EffectCommandDescriptor Add = Describe(
            "AddParameter", EffectCommandParameterKind.NumberExpression);
        public static readonly EffectCommandDescriptor Set = Describe(
            "SetParameter", EffectCommandParameterKind.ParameterValue);
        public static readonly IReadOnlyList<EffectCommandDescriptor> Descriptors =
            Array.AsReadOnly(new[] { Add, Set });

        public static float Calculate(ParameterStore parameters, string formula)
        {
            ExpressionResult<float> result = parameters.Calculate(formula);
            if (!result.IsSuccess)
                throw new InvalidOperationException(result.Error.ToString());
            return result.Value;
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
                    new EffectCommandParameterDefinition(
                        "value",
                        valueKind,
                        parameterKeySourceIndex:
                            valueKind == EffectCommandParameterKind.ParameterValue
                                ? 0
                                : -1)
                });
        }
    }

    public sealed class ParameterEffectCommandModule : IEffectCommandModule
    {
        private readonly ParameterStore parameters;

        public ParameterEffectCommandModule(ParameterStore parameters)
        {
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        public EffectCommandDefinition CreateDefinition(string commandName)
        {
            switch (commandName)
            {
                case "AddParameter":
                    return new EffectCommandDefinition(
                        ParameterEffectCommandManifest.Add,
                        new AddParameterCommand(parameters));
                case "SetParameter":
                    return new EffectCommandDefinition(
                        ParameterEffectCommandManifest.Set,
                        new SetParameterCommand(parameters));
                default:
                    throw new ArgumentException(
                        $"Parameters does not own command '{commandName}'.",
                        nameof(commandName));
            }
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
            float value = ParameterEffectCommandManifest.Calculate(parameters, arguments[1]);
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
                    parameters.Set(
                        arguments[0],
                        (int)Math.Round(ParameterEffectCommandManifest.Calculate(
                            parameters, arguments[1])));
                    break;
                case ParameterType.Float:
                    parameters.Set(
                        arguments[0],
                        ParameterEffectCommandManifest.Calculate(
                            parameters, arguments[1]));
                    break;
                case ParameterType.Bool:
                    ExpressionResult<bool> result =
                        parameters.EvaluateCondition(arguments[1]);
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

    [Preserve]
    public sealed class ParameterEffectCommandModuleFactory :
        IEffectCommandModuleFactory
    {
        public IReadOnlyList<EffectCommandDescriptor> GetDescriptors()
        {
            return ParameterEffectCommandManifest.Descriptors;
        }

        public IEffectCommandModule Create(EffectCommandServiceRegistry services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            return new ParameterEffectCommandModule(
                services.GetRequired<ParameterStore>());
        }
    }
}
