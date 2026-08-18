using System.Collections.Generic;

namespace KahaGameCore.Effects
{
    /// <summary>
    /// Exposes editor metadata for the commands owned by one runtime assembly.
    /// Implementations must have a public parameterless constructor.
    /// </summary>
    public interface IEffectCommandDescriptorProvider
    {
        IReadOnlyList<EffectCommandDescriptor> GetDescriptors();
    }
}
