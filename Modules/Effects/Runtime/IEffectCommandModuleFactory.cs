using System.Collections.Generic;

namespace KahaGameCore.Effects
{
    /// <summary>
    /// Declares one command assembly to the Editor and creates its runtime commands from
    /// services already owned by the composition root. GetDescriptors runs without any
    /// runtime services so the Editor can list commands with no game running; Create is
    /// only called when at least one published command is enabled.
    /// Implementations require a public parameterless constructor.
    /// </summary>
    public interface IEffectCommandModuleFactory
    {
        IReadOnlyList<EffectCommandDescriptor> GetDescriptors();
        IReadOnlyList<EffectCommandDefinition> Create(EffectCommandDependencies services);
    }
}
