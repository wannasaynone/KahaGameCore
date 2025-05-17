using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace KahaGameCore.Package.SideScrollerActor.View
{
    public class BootView : MonoBehaviour
    {
        [SerializeField] private Image coverImage;
        [SerializeField] private Image logoImage;
        [SerializeField] private TitleView titleView;

        public void StartShow()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            StartCoroutine(LoadGame());
        }

        private IEnumerator LoadGame()
        {
            coverImage.DOFade(0f, 1f);

            yield return new WaitForSeconds(1.5f);

            coverImage.DOFade(1f, 1f);

            yield return new WaitForSeconds(1f);

            titleView.gameObject.SetActive(true);
            logoImage.gameObject.SetActive(false);
            coverImage.DOFade(0f, 1f);

            yield return new WaitForSeconds(1f);

            gameObject.SetActive(false);
        }
    }
}