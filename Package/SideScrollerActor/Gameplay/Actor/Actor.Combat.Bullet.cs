using System;
using UnityEngine;
using KahaGameCore.GameEvent;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay
{
    public partial class Actor
    {
        private void CreateBulletWithCurrentAttackInfo()
        {
            if (currentAttackInfo.GetBulletPrefab() == null)
            {
                return;
            }

            Transform bulletSpawnPoint = currentAttackInfo.GetBulletSpawnPoint();

            if (bulletSpawnPoint == null)
            {
                bulletSpawnPoint = defaultFirePoint;
            }

            Vector3 firePoint = bulletSpawnPoint.position;
            Bullet bullet = Instantiate(currentAttackInfo.GetBulletPrefab(), firePoint, bulletSpawnPoint.rotation).GetComponent<Bullet>();

            if (currentAttackInfo.GetMuzzlePrefab() != null)
            {
                GameObject cloneMuzzle = Instantiate(currentAttackInfo.GetMuzzlePrefab());
                cloneMuzzle.gameObject.SetActive(true);
                cloneMuzzle.transform.SetParent(null);
                cloneMuzzle.transform.position = firePoint;
                cloneMuzzle.transform.rotation = bulletSpawnPoint.rotation;
                Destroy(cloneMuzzle, 1f);
            }

            if (currentAttackInfo.ShouldPauseWhenCreateBullet())
            {
                EventBus.Publish(new Game_CallHitPause() { duration = currentAttackInfo.GetPauseDurationWhenCreateBullet() });
            }

            if (bullet == null)
            {
                Debug.LogError("No bullet found in BulletPrefab: " + currentAttackInfo.GetBulletPrefab().name);
                return;
            }

            bullet.owner = this;
            bullet.damage = Convert.ToInt32(currentAttackInfo.GetMutiply() * actorSetting.attack);
            bullet.deductStamina = Convert.ToInt32(currentAttackInfo.GetMutiply() * actorSetting.deductTargetStamina);
            bullet.hitForce_power = currentAttackInfo.GetHitForcePower();
            bullet.hitForce_duration = currentAttackInfo.GetHitForceDuration();
            bullet.isHeavyHit = currentAttackInfo.IsHeavyHit();
            bullet.pauseWhenHit = currentAttackInfo.ShouldPauseWhenHit();
            bullet.pauseDurationWhenHit = currentAttackInfo.GetPauseDurationWhenHit();
            bullet.pauseDurationWhenCritical = currentAttackInfo.GetPauseDurationWhenCritical();
            bullet.pauseWhenCritical = currentAttackInfo.ShouldPauseWhenCritical();
            bullet.allowCritical = currentAttackInfo.AllowCritical();
            bullet.criticalMutiply = currentAttackInfo.GetCriticalMultiply();
            bullet.shakeWhenHit = currentAttackInfo.GetShakeWhenHit();
            bullet.shakeWhenCritical = currentAttackInfo.GetShakeWhenCritical();

            // 設置範圍爆炸傷害相關屬性
            bullet.enableAreaDamage = currentAttackInfo.IsAreaDamageEnabled();
            bullet.explosionRadius = currentAttackInfo.GetExplosionRadius();
            bullet.areaAffectWeakPoints = currentAttackInfo.DoesAreaAffectWeakPoints();

            SetStamina(currentStamina - currentAttackInfo.GetStaminaCost());
            SetHealth(currentHealth - currentAttackInfo.GetHealthCost());
        }
    }
}
