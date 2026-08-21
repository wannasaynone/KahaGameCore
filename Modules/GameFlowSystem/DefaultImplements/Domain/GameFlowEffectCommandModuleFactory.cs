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

        public IEffectCommandModule Create(EffectCommandServiceRegistry services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            return new GameFlowEffectCommandModule(
                services.GetRequired<GameFlowEffectCommandServices>());
        }
    }
}
