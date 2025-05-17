using KahaGameCore.Common;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.WeaponScripts
{
    public abstract class Weapon : MonoBehaviour, IWeaponCapability
    {
        public Sprite Icon => weaponIcon;
        [Tooltip("設定顯示武器 ICON。")]
        [Rename("武器 ICON")][SerializeField] private Sprite weaponIcon;
        public string IdleAnimationName => idleAnimationName;
        [Tooltip("使用這個武器時播放的待機動畫。")]
        [Rename("待機動畫")][SerializeField] private string idleAnimationName = "Idle";
        public string WalkAnimationName => walkAnimationName;
        [Tooltip("使用這個武器時播放的走路動畫。面向與前進方向相同時播放這個。")]
        [Rename("走路動畫")][SerializeField] private string walkAnimationName = "Walk";
        public string WalkAnimationName_Reverse => walkAnimationName_Reverse;
        [Tooltip("使用這個武器時播放的走路動畫。面向與前進方向相反時播放這個。")]
        [Rename("走路動畫(反向)")][SerializeField] private string walkAnimationName_Reverse = "Walk_R";
        public string RunAnimationName => runAnimationName;
        [Tooltip("使用這個武器時播放的跑步動畫。")]
        [Rename("跑步動畫")][SerializeField] private string runAnimationName = "Run";
        public string ReloadAnimationName => reloadAnimationName;
        [Tooltip("使用這個武器時播放的裝填動畫。")]
        [Rename("裝填動畫")][SerializeField] private string reloadAnimationName = "Reload";
        public abstract AttackInfo PeekNextAttackInfo();
        public abstract AttackInfo GetNextAttackInfo();

        IAttackInfo IWeaponCapability.PeekNextAttackInfo()
        {
            AttackInfo attackInfo = PeekNextAttackInfo();
            return attackInfo;
        }

        IAttackInfo IWeaponCapability.GetNextAttackInfo()
        {
            AttackInfo attackInfo = GetNextAttackInfo();
            return attackInfo;
        }

        public virtual bool IsRangeWeapon()
        {
            return this is RangeWeapon;
        }

        public virtual void Reload()
        {
            if (this is RangeWeapon rangeWeapon)
            {
                rangeWeapon.Reload();
            }
        }

        public string GetIdleAnimationName()
        {
            return IdleAnimationName;
        }

        public string GetWalkAnimationName(bool isReverse = false)
        {
            return isReverse ? WalkAnimationName_Reverse : WalkAnimationName;
        }

        public string GetRunAnimationName()
        {
            return RunAnimationName;
        }

        public string GetReloadAnimationName()
        {
            return ReloadAnimationName;
        }
    }
}
