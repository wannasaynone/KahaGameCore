using System.Collections;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class WeakPoint : MonoBehaviour
    {
        public Actor ReferenceActor => referenceActor;
        [SerializeField] private Actor referenceActor;
        [SerializeField] private float damageMultiplier = 2f;
        [SerializeField] private CanvasGroup hitHintPrefab_nullable;

        private void Start()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
            GetComponent<Rigidbody2D>().gravityScale = 0;
        }

        public void TakeDamage(Actor attacker, Bullet bullet)
        {
            bullet.damage = System.Convert.ToInt32(bullet.damage * damageMultiplier);
            bullet.hitForce_power *= 2f;
            referenceActor.TakeDamage(attacker, bullet);

            if (hitHintPrefab_nullable != null)
            {
                KahaGameCore.Common.GeneralCoroutineRunner.Instance.StartCoroutine(IEShowHitHint());
            }
        }


        private IEnumerator IEShowHitHint()
        {
            CanvasGroup hitHint = Instantiate(hitHintPrefab_nullable, transform.position, Quaternion.identity);
            hitHint.transform.SetParent(null);
            hitHint.transform.position = hitHintPrefab_nullable.transform.position;
            hitHint.transform.localScale = hitHintPrefab_nullable.transform.localScale;
            hitHint.alpha = 0f;
            hitHint.gameObject.SetActive(true);

            while (hitHint.alpha < 1f)
            {
                hitHint.alpha += Time.deltaTime * 3f;
                hitHint.transform.position += 0.5f * Time.deltaTime * Vector3.up;
                yield return null;
            }

            while (hitHint.alpha > 0f)
            {
                hitHint.alpha -= Time.deltaTime * 3f;
                hitHint.transform.position += 0.5f * Time.deltaTime * Vector3.up;
                yield return null;
            }

            Destroy(hitHint.gameObject);
        }
    }
}