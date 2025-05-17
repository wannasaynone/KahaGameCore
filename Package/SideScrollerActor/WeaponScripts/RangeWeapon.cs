using KahaGameCore.GameEvent;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.WeaponScripts
{
    public class RangeWeapon : Weapon
    {
        [Header("遠距離武器只允許存在一個攻擊資訊")]
        [SerializeField] private AttackInfo attackInfo;
        [Header("武器的最大裝填彈藥數量")]
        [SerializeField] private int ammo;
        [Header("重新填裝時需要消耗多少時間")]
        [SerializeField] private float reloadTime;
        [Header("瞄準視線(可空)")]
        [SerializeField] private Sight sight;
        [Header("彈射彈殼(可空)")]
        [SerializeField] private GameObject bulletShellPrefab;
        [SerializeField] private Transform bulletShellSpawnPoint;
        [Header("子彈不足音效(可空)")]
        [SerializeField] private AudioClip outOfAmmoSound;


        public int MaxAmmo => ammo;
        public int CurrentAmmo => currentAmmo;
        [Header("遊戲中子彈狀態")]
        [SerializeField] private int currentAmmo = -1;
        public int RemainingAmmo => remainingAmmo;
        [SerializeField] private int remainingAmmo = 0;

        private float attackTimer = 0f;
        private float currentAttackDuration = 0f;

        private bool isReloading = false;
        private float reloadTimer = 0f;

        [SerializeField] private int ammoID = -1;

        private void Awake()
        {
            Invoke(nameof(PublishAmmoAmountChanged), 0.1f);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<RangeWeapon_OnReloadInterupted>(OnReloadInterupted);
            EventBus.Subscribe<Weapon_CallSimplePingPong>(OnCallSimplePingPong);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RangeWeapon_OnReloadInterupted>(OnReloadInterupted);
            EventBus.Unsubscribe<Weapon_CallSimplePingPong>(OnCallSimplePingPong);
        }

        private void OnCallSimplePingPong(Weapon_CallSimplePingPong e)
        {
            if (e.weaponInstanceID == GetInstanceID())
            {
                CallSimplePingPong();
            }
        }

        private void OnReloadInterupted(RangeWeapon_OnReloadInterupted e)
        {
            if (e.weaponInstanceID != GetInstanceID())
            {
                return;
            }

            isReloading = false;
            reloadTimer = 0f;
            EventBus.Publish(new RangeWeapon_OnReloading() { weaponInstanceID = GetInstanceID(), currentReloadTime = reloadTimer, maxReloadTime = reloadTime });
        }

        public void Reload()
        {
            if (currentAmmo >= ammo || remainingAmmo <= 0)
            {
                return;
            }

            if (isReloading)
            {
                return;
            }

            isReloading = true;
            reloadTimer = reloadTime;
            EventBus.Publish(new RangeWeapon_OnReloading() { weaponInstanceID = GetInstanceID(), currentReloadTime = reloadTime, maxReloadTime = reloadTime });
        }

        private void PublishAmmoAmountChanged()
        {
            EventBus.Publish(new RangeWeapon_OnAmmoAmountChanged() { weaponInstanceID = GetInstanceID(), currentAmmo = currentAmmo, maxAmmo = ammo, remainingAmmo = remainingAmmo });
        }

        public void CallSimplePingPong()
        {
            if (sight != null)
            {
                sight.SimplePingPongRotation(5f, 0.15f);
            }
        }

        public void SetRemainingAmmo(int ammoID, int amount)
        {
            this.ammoID = ammoID;
            remainingAmmo = amount;

            if (currentAmmo == -1) // means not initialized
            {
                if (remainingAmmo >= ammo)
                {
                    currentAmmo = ammo;
                    remainingAmmo -= ammo;
                }
                else
                {
                    currentAmmo = remainingAmmo;
                    remainingAmmo = 0;
                }
            }
            else
            {
                remainingAmmo -= currentAmmo;
            }

            EventBus.Publish(new RangeWeapon_OnAmmoAmountChanged() { weaponInstanceID = GetInstanceID(), currentAmmo = currentAmmo, maxAmmo = ammo, remainingAmmo = remainingAmmo });
        }

        public override AttackInfo GetNextAttackInfo()
        {
            if (attackInfo == null)
            {
                Debug.LogError("No attack info found in Weapon: " + gameObject.name);
                return null;
            }

            if (currentAmmo <= 0)
            {
                if (remainingAmmo <= 0 && outOfAmmoSound != null)
                {
                    Audio.AudioManager.Instance.PlaySound(outOfAmmoSound);
                    return null;
                }

                Reload();
                return null;
            }

            if (attackTimer < attackInfo.allowNextAttackTime || isReloading)
            {
                return null;
            }

            attackTimer = 0f;
            currentAttackDuration = attackInfo.duration;
            EventBus.Publish(new Weapon_AttackDurationChanged() { weaponInstanceID = GetInstanceID(), currentAttackDuration = attackTimer, maxAttackDuration = currentAttackDuration });

            currentAmmo--;
            EventBus.Publish(new RangeWeapon_OnAmmoAmountChanged() { weaponInstanceID = GetInstanceID(), currentAmmo = currentAmmo, maxAmmo = ammo, remainingAmmo = remainingAmmo });

            if (bulletShellPrefab != null)
            {
                GameObject bulletShell = Instantiate(bulletShellPrefab);

                if (bulletShellSpawnPoint != null)
                {
                    bulletShell.transform.position = bulletShellSpawnPoint.position;
                }
                else
                {
                    bulletShell.transform.position = transform.position;
                }

                bulletShell.SetActive(true);
                bulletShell.transform.SetParent(null);
                bulletShell.transform.rotation = transform.rotation;
            }

            EventBus.Publish(new InGameItem_OnAmountChanged()
            {
                itemID = ammoID,
                addAmount = -1
            });

            return attackInfo;
        }

        public override AttackInfo PeekNextAttackInfo()
        {
            if (attackInfo == null)
            {
                Debug.LogError("No attack info found in Weapon: " + gameObject.name);
                return null;
            }

            return attackInfo;
        }

        private void Update()
        {
            attackTimer += Time.deltaTime;
            EventBus.Publish(new Weapon_AttackDurationChanged() { weaponInstanceID = GetInstanceID(), currentAttackDuration = attackTimer, maxAttackDuration = currentAttackDuration });

            if (isReloading)
            {
                reloadTimer -= Time.deltaTime;
                EventBus.Publish(new RangeWeapon_OnReloading() { weaponInstanceID = GetInstanceID(), currentReloadTime = reloadTimer, maxReloadTime = reloadTime });
                if (reloadTimer <= 0)
                {
                    int addAmmo = ammo - currentAmmo;
                    if (remainingAmmo >= addAmmo)
                    {
                        remainingAmmo -= addAmmo;
                        currentAmmo = ammo;
                    }
                    else
                    {
                        currentAmmo = remainingAmmo;
                        remainingAmmo = 0;
                    }
                    EventBus.Publish(new RangeWeapon_OnAmmoAmountChanged() { weaponInstanceID = GetInstanceID(), currentAmmo = currentAmmo, maxAmmo = ammo, remainingAmmo = remainingAmmo });
                    isReloading = false;
                }
            }
        }
    }
}
