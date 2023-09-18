using System;

namespace KahaGameCore.Combat
{
    public interface ISkillTrigger
    {
        void Trigger(string timing, Action onEnded);
    }
}