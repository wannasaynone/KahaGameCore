namespace KahaGameCore.Combat
{
    public class ProcessData 
    {
        public string timing = "";
        public IActor caster = null;
        public System.Collections.Generic.List<IActor> targets = null;
        public int skipIfCount = 0;
    }
}