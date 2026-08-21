using System.Collections.Generic;

namespace KahaGameCore.Effects
{
    /// <summary>
    /// Declares one command assembly to the Editor and creates its runtime module
    /// from services already owned by the composition root.
    /// Implementations require a public parameterless constructor.
    /// </summary>
    public interface IEffectCommandModuleFactory
    {
        IReadOnlyList<EffectCommandDescriptor> GetDescriptors();
        IEffectCommandModule Create(EffectCommandServiceRegistry services);
    }
}
