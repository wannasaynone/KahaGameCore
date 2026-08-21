using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace KahaGameCore.Effects.StandardCommands
{
    internal static class StandardEffectCommandManifest
    {
        public static readonly EffectCommandDescriptor Wait =
            new EffectCommandDescriptor(
                "Wait",
                "Wait",
                "Presentation",
                new[]
                {
                    new EffectCommandParameterDefinition(
                        "seconds",
                        EffectCommandParameterKind.Literal)
                });

        public static readonly IReadOnlyList<EffectCommandDescriptor> Descriptors =
            Array.AsReadOnly(new[] { Wait });
    }

    public sealed class StandardEffectCommandModule : IEffectCommandModule
    {
        public EffectCommandDefinition CreateDefinition(string commandName)
        {
            if (string.Equals(commandName, "Wait", StringComparison.Ordinal))
            {
                return new EffectCommandDefinition(
                    StandardEffectCommandManifest.Wait,
                    new WaitCommand());
            }

            throw new ArgumentException(
                $"Standard Effects does not own command '{commandName}'.",
                nameof(commandName));
        }
    }

    [Preserve]
    public sealed class StandardEffectCommandModuleFactory :
        IEffectCommandModuleFactory
    {
        public IReadOnlyList<EffectCommandDescriptor> GetDescriptors()
        {
            return StandardEffectCommandManifest.Descriptors;
        }

        public IEffectCommandModule Create(EffectCommandServiceRegistry services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            return new StandardEffectCommandModule();
        }
    }
}
