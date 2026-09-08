using System;
using System.Collections.Generic;
using KahaGameCore.Effects;
using UnityEngine.Scripting;

namespace KahaGameCore.GameFlowSystem.DefaultImplements
{
    [Preserve]
    public sealed class GameFlowEffectCommandModuleFactory :
        IEffectCommandModuleFactory
    {
        public IReadOnlyList<EffectCommandDescriptor> GetDescriptors()
        {
            return GameFlowEffectCommandManifest.Descriptors;
        }

        public IReadOnlyList<EffectCommandDefinition> Create(
            EffectCommandDependencies services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            return GameFlowEffectCommandModule.CreateDefinitions(
                services.GetRequired<GameFlowEffectCommandServices>());
        }
    }
}
