using System.Collections.Generic;
using KahaGameCore.Effects;

namespace KahaGameCore.GameFlowSystem.DefaultImplements
{
    public sealed class GameFlowEffectCommandDescriptorProvider :
        IEffectCommandDescriptorProvider
    {
        public IReadOnlyList<EffectCommandDescriptor> GetDescriptors()
        {
            return EffectCommandRegistrar.Descriptors;
        }
    }
}
