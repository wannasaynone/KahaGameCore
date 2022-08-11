namespace KahaGameCore.Combat.Processor.EffectProcessor
{
    public class EffectTimingTriggedSignal : InGameEvent.InGameEventCenter.InGameEvent
    {
        public CombatUnit Caster { get; private set; }
        public CombatUnit Target { get; private set; }
        public string Timing { get; private set; }

        public EffectTimingTriggedSignal(CombatUnit caster, CombatUnit target, string timing)
        {
            Caster = caster;
            Target = target;
            Timing = timing;
        }
    }
}
