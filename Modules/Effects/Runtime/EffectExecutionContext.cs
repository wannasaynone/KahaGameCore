using System;
using System.Collections.Generic;
using KahaGameCore.ValueContainer;

namespace KahaGameCore.Effects
{
    public sealed class EffectExecutionContext
    {
        public EffectExecutionContext(
            IValueContainer caster = null,
            IEnumerable<IValueContainer> targets = null,
            IDictionary<string, object> customData = null)
        {
            Caster = caster;
            Targets = new List<IValueContainer>(targets ?? new IValueContainer[0]).AsReadOnly();
            CustomData = new Dictionary<string, object>(
                customData ?? new Dictionary<string, object>());
        }

        public IValueContainer Caster { get; }
        public IReadOnlyList<IValueContainer> Targets { get; }
        public IReadOnlyDictionary<string, object> CustomData { get; }
    }
}
