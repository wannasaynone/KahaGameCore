using KahaGameCore.ValueContainer;

namespace KahaGameCore.Package.EffectProcessor.Data
{
    public class ProcessData
    {
        public string timing = "";
        public IValueContainer caster = null;
        public System.Collections.Generic.List<IValueContainer> targets = new System.Collections.Generic.List<IValueContainer>();
        public int skipIfCount = 0;
        public System.Collections.Generic.Dictionary<string, object> customData = new System.Collections.Generic.Dictionary<string, object>();
    }
}