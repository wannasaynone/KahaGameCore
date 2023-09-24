namespace KahaGameCore.Combat
{
    public class ProcessData 
    {
        public string timing = "";
        public IActor caster = null;
        public System.Collections.Generic.List<IActor> targets = new System.Collections.Generic.List<IActor>();
        public int skipIfCount = 0;
    }
}