namespace KahaGameCore.Combat
{
    public interface IActor 
    {
        IValueContainer Stats { get; }
        ISkillTrigger SkillTrigger { get; }
    }
}