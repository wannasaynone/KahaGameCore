using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.WeaponScripts
{
    public interface IAttackInfo
    {
        string GetAnimationName();
        string GetPrepareAnimationName();
        string GetPrepareAndMoveAnimationName(bool isReverse = false);
        RuntimeAnimatorController GetAnimatorController();

        float GetStaminaCost();
        int GetHealthCost();
        float GetDuration();
        float GetAllowNextAttackTime();
        float GetCreateBulletTime();

        GameObject GetBulletPrefab();
        Transform GetBulletSpawnPoint();
        GameObject GetMuzzlePrefab();

        float GetMutiply();
        float GetHitForceDuration();
        float GetHitForcePower();
        bool IsHeavyHit();

        bool ShouldPauseWhenCreateBullet();
        float GetPauseDurationWhenCreateBullet();

        bool ShouldPauseWhenHit();
        float GetPauseDurationWhenHit();
        bool ShouldPauseWhenCritical();
        float GetPauseDurationWhenCritical();

        GameObject GetEnableWhenPrepare();
        GameObject GetEnableWhenAttacking();

        float GetCameraOffsetPrepareAttack();
        float GetAddMoveSpeedWhenPrepare();

        bool IsAreaDamageEnabled();
        float GetExplosionRadius();
        bool DoesAreaAffectWeakPoints();

        bool AllowCritical();
        float GetCriticalMultiply();
        float GetShakeWhenHit();
        float GetShakeWhenCritical();
    }
}
