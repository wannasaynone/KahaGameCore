namespace KahaGameCore.Effects
{
    /// <summary>
    /// The commands an effect program can call, keyed by the name used in its source.
    /// Filled once by <see cref="EffectCommandBootstrapper"/> and then read by
    /// <see cref="EffectRuntime"/> on every execution. The services used to build these
    /// commands live in <see cref="EffectCommandDependencies"/> instead.
    /// </summary>
    public sealed class EffectCommandRegistry
    {
        private readonly System.Collections.Generic.Dictionary<string, EffectCommandDefinition> definitions =
            new System.Collections.Generic.Dictionary<string, EffectCommandDefinition>(System.StringComparer.Ordinal);

        public void Register(EffectCommandDefinition definition)
        {
            if (definition == null) throw new System.ArgumentNullException(nameof(definition));
            if (definitions.ContainsKey(definition.Name))
            {
                throw new System.InvalidOperationException(
                    $"Effect command '{definition.Name}' is already registered.");
            }

            definitions.Add(definition.Name, definition);
        }

        public bool TryGetDefinition(string name, out EffectCommandDefinition definition)
        {
            return definitions.TryGetValue(name, out definition);
        }
    }
}
