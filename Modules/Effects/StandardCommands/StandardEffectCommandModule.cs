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

    [Preserve]
    public sealed class StandardEffectCommandModuleFactory :
        IEffectCommandModuleFactory
    {
        public IReadOnlyList<EffectCommandDescriptor> GetDescriptors()
        {
            return StandardEffectCommandManifest.Descriptors;
        }

        public IReadOnlyList<EffectCommandDefinition> Create(
            EffectCommandDependencies services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            return new[]
            {
                new EffectCommandDefinition(
                    StandardEffectCommandManifest.Wait, new WaitCommand())
            };
        }
    }
}
