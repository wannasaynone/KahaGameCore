namespace KahaGameCore.EffectCommand
{
    public class EffectTimingTriggedSignal
    {
        public EffectProcesser.ProcessData ProcessData { get; private set; }

        public EffectTimingTriggedSignal(EffectProcesser.ProcessData processData)
        {
            ProcessData = processData;
        }
    }
}
