namespace KahaGameCore.Effects
{
    /// <summary>
    /// Runtime adapter for one command-owning assembly. Implementations capture their
    /// dependencies through construction and create only definitions requested by name.
    /// </summary>
    public interface IEffectCommandModule
    {
        EffectCommandDefinition CreateDefinition(string commandName);
    }
}
