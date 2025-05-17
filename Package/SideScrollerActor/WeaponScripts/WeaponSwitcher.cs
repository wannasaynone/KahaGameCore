using System.Collections.Generic;
using KahaGameCore.GameEvent;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.WeaponScripts
{
    public class WeaponSwitcher : MonoBehaviour
    {
        [SerializeField] private Object actor;
        [SerializeField] private Transform aimmingWeaponTransform;
        [SerializeField] private List<Weapon> weapons;
        [SerializeField] private AudioClip switchSound;

        private int currentIndex = -1;

        public void Initialize()
        {
            if (weapons == null || weapons.Count == 0)
            {
                Debug.LogError("WeaponSwitcher has no weapons to switch to");
                return;
            }

            for (int i = 0; i < weapons.Count; i++)
            {
                if (weapons[i] == null)
                {
                    Debug.LogError("WeaponSwitcher has a null weapon in the list");
                    return;
                }
            }

            SwitchWeapon(0, true);
        }

        public IWeaponCapability GetDefaultWeapon()
        {
            if (weapons == null || weapons.Count == 0)
            {
                Debug.LogError("WeaponSwitcher has no weapons to switch to");
                return null;
            }

            return weapons[0];
        }

        public void AddWeapon(Weapon weapon)
        {
            if (weapons == null)
            {
                weapons = new List<Weapon>();
            }

            if (weapon == null)
            {
                Debug.LogError("Cannot add null weapon to WeaponSwitcher");
                return;
            }

            if (weapons.Contains(weapon))
            {
                Debug.LogError("Cannot add weapon to WeaponSwitcher that is already in the list");
                return;
            }

            weapons.Add(weapon);

            if (weapon is RangeWeapon)
            {
                weapon.transform.SetParent(aimmingWeaponTransform);
            }

            SwitchWeapon(weapons.Count - 1, true);
        }

        public void RemoveWeapon(Weapon weapon)
        {
            if (weapons == null || weapons.Count == 0)
            {
                Debug.LogError("Cannot remove weapon from WeaponSwitcher that is not in the list");
                return;
            }

            if (weapon == null)
            {
                Debug.LogError("Cannot remove null weapon from WeaponSwitcher");
                return;
            }

            if (!weapons.Contains(weapon))
            {
                Debug.LogError("Cannot remove weapon from WeaponSwitcher that is not in the list");
                return;
            }

            if (weapons.Count == 1)
            {
                Debug.LogError("Cannot remove the last weapon from WeaponSwitcher");
                return;
            }

            weapons.Remove(weapon);
        }

        public Weapon GetWeapon(string name)
        {
            if (weapons == null || weapons.Count == 0)
            {
                Debug.LogError("Cannot get weapon from WeaponSwitcher that is not in the list");
                return null;
            }

            for (int i = 0; i < weapons.Count; i++)
            {
                if (weapons[i].name.Replace("(Clone)", "") == name)
                {
                    return weapons[i];
                }
            }

            return null;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SwitchWeapon(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SwitchWeapon(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SwitchWeapon(2);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                SwitchWeapon(3);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                SwitchWeapon(4);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                SwitchWeapon(5);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                SwitchWeapon(6);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                SwitchWeapon(7);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                SwitchWeapon(8);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                SwitchWeapon(9);
            }

            if (Input.GetAxis("Mouse ScrollWheel") > 0)
            {
                currentIndex++;

                if (currentIndex >= weapons.Count)
                {
                    currentIndex = 0;
                }

                SwitchWeapon(currentIndex);
            }
            else if (Input.GetAxis("Mouse ScrollWheel") < 0)
            {
                currentIndex--;

                if (currentIndex < 0)
                {
                    currentIndex = weapons.Count - 1;
                }

                SwitchWeapon(currentIndex);
            }
        }

        private void SwitchWeapon(int index, bool skipSound = false)
        {
            if (index < 0 || index >= weapons.Count)
            {
                Debug.Log("index out of range in WeaponSwitcher");
                return;
            }

            WeaponSwitcher_OnWeaponRequested e = new WeaponSwitcher_OnWeaponRequested()
            {
                actorInstanceID = actor.GetInstanceID(),
                weapon = weapons[index]
            };

            EventBus.Publish(e);
            currentIndex = index;

            if (!skipSound) Audio.AudioManager.Instance.PlaySound(switchSound);
        }
    }
}