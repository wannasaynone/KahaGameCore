using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.WeaponScripts
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class BulletShell : MonoBehaviour
    {
        [SerializeField] private AudioClip landingSound;

        private bool hasLanded = false;

        private void Start()
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.angularVelocity = Random.Range(-360f, 360f);
                rb.velocity = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 2f));
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!hasLanded && collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                hasLanded = true;
                if (landingSound != null) Audio.AudioManager.Instance.PlaySound(landingSound);
                Destroy(gameObject);
            }
        }
    }
}