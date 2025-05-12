using KahaGameCore.Package.EffectProcessor.ValueContainer;

namespace KahaGameCore.Package.EffectProcessor.Data
{
    public class ProcessData
    {
        public string timing = "";
        public IValueContainer caster = null;
        public System.Collections.Generic.List<IValueContainer> targets = new System.Collections.Generic.List<IValueContainer>();
        public int skipIfCount = 0;
    }
}