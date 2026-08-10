namespace KahaGameCore.Effects
{
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
