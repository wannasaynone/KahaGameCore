namespace KahaGameCore.Package.SideScrollerActor.WeaponScripts
{
    public interface IWeaponCapability
    {
        string GetIdleAnimationName();
        string GetWalkAnimationName(bool isReverse = false);
        string GetRunAnimationName();
        string GetReloadAnimationName();

        IAttackInfo PeekNextAttackInfo();
        IAttackInfo GetNextAttackInfo();

        bool IsRangeWeapon();
        void Reload();

        int GetInstanceID();
    }
}
