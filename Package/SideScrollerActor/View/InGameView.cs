using DG.Tweening;
using KahaGameCore.Package.SideScrollerActor.WeaponScripts;
using UnityEngine;
using UnityEngine.UI;

namespace KahaGameCore.Package.SideScrollerActor.View
{
    public class InGameView : MonoBehaviour
    {
        [SerializeField] private Image heroHealthBar;
        [SerializeField] private Image heroHealthDelayBar;
        [SerializeField] private Image heroPortrait;
        [SerializeField] private Image weaponIcon;
        [SerializeField] private GameObject ammoTextRoot;
        [SerializeField] private TMPro.TextMeshProUGUI ammoText;

        private Tween heroPortraitTween;

        public void Initialize()
        {
            if (heroPortraitTween != null)
            {
                heroPortraitTween.Kill();
            }
        }

        private void OnEnable()
        {
            StartPortraitTween();
        }

        private void OnDisable()
        {
            StopPortraitTween();
        }

        public void StartPortraitTween()
        {
            StopPortraitTween();
            heroPortraitTween = heroPortrait.transform.DOScale(0.56f, 4f).SetLoops(-1, LoopType.Yoyo);
        }

        public void StopPortraitTween()
        {
            if (heroPortraitTween != null)
            {
                heroPortraitTween.Kill();
                heroPortraitTween = null;
            }
            heroPortrait.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
        }

        public void DoHurtEffect()
        {
            StopPortraitTween();
            heroPortraitTween = heroPortrait.transform.DOShakePosition(0.5f, 10f, 10, 90f, false, true).OnComplete(StartPortraitTween);
            DOTween.To(GetHeroPortraitColor, SetHeroPortraitColor, Color.red, 0.25f).OnComplete(() =>
            {
                DOTween.To(GetHeroPortraitColor, SetHeroPortraitColor, new Color(204f / 255f, 204f / 255f, 204f / 255f, 255f / 255f), 0.25f);
            });
        }

        public void DoSimpleShakeEffect(float strength)
        {
            StopPortraitTween();
            heroPortraitTween = heroPortrait.transform.DOShakePosition(0.5f, strength, 10, 45f, false, true).OnComplete(StartPortraitTween);
        }

        public void SetHeroHealth(float healthPercentage)
        {
            heroHealthBar.fillAmount = healthPercentage;
            DOTween.To(GetDelayBarFillAmount, SetDelayBarFillAmount, healthPercentage, 0.5f);

            if (heroHealthDelayBar.fillAmount > healthPercentage && heroPortraitTween != null)
            {
                DoHurtEffect();
            }
        }

        public void UpdateDelayImmediatly()
        {
            heroHealthDelayBar.fillAmount = heroHealthBar.fillAmount;
        }

        private float GetDelayBarFillAmount()
        {
            return heroHealthDelayBar.fillAmount;
        }

        public void SetDelayBarFillAmount(float healthPercentage)
        {
            heroHealthDelayBar.fillAmount = healthPercentage;
        }

        private Color GetHeroPortraitColor()
        {
            return heroPortrait.color;
        }

        public void SetHeroPortraitColor(Color color)
        {
            heroPortrait.color = color;
        }

        public void SetWeaponState(Weapon weapon)
        {
            if (weapon == null)
            {
                weaponIcon.gameObject.SetActive(false);
                ammoTextRoot.SetActive(false);
                return;
            }

            weaponIcon.gameObject.SetActive(true);
            weaponIcon.sprite = weapon.Icon;
            weaponIcon.SetNativeSize();

            if (weapon is RangeWeapon)
            {
                ammoTextRoot.SetActive(true);
                RangeWeapon rangeWeapon = weapon as RangeWeapon;
                ammoText.text = (rangeWeapon.CurrentAmmo + rangeWeapon.RemainingAmmo).ToString();
            }
            else
            {
                ammoTextRoot.SetActive(false);
            }
        }
    }
}