namespace KahaGameCore.Combat
{
    public class ProcessData
    {
        public string timing = "";
        public Actor.IActor caster = null;
        public System.Collections.Generic.List<Actor.IActor> targets = new System.Collections.Generic.List<Actor.IActor>();
        public int skipIfCount = 0;
    }
}