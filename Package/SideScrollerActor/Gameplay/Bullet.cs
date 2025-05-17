using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class Bullet : MonoBehaviour
    {
        public bool IsMelee => speed <= 0.1f;

        [Header("Observable Bullet Info")]
        [Observerable] public Actor owner;
        [Observerable] public int damage = 10;
        [Observerable] public int deductStamina = 0;
        [Observerable] public float hitForce_power = 0f;
        [Observerable] public float hitForce_duration = 0f;
        [Observerable] public bool isHeavyHit = false;
        [Observerable] public bool pauseWhenHit = false;
        [Observerable] public float pauseDurationWhenHit = 0f;
        [Observerable] public bool pauseWhenCritical = false;
        [Observerable] public float pauseDurationWhenCritical = 0f;
        [Observerable] public bool allowCritical;
        [Observerable] public float criticalMutiply = 1f;
        [Observerable] public float shakeWhenHit = 0f;
        [Observerable] public float shakeWhenCritical = 0f;

        [Observerable] public bool enableAreaDamage = false;
        [Observerable] public float explosionRadius = 2f;
        [Observerable] public bool areaAffectWeakPoints = true;

        [Observerable] private List<Actor> hitActors = new List<Actor>();

        [Header("Bullet Settings")]
        [SerializeField] private float speed = 20f;
        [SerializeField] private bool destroyAfterHit = true;

        [System.Serializable]
        private class HitEffectInfo
        {
            public string tagName;
            public GameObject hitEffect;
            public AudioClip hitSound;
        }
        [SerializeField] private GameObject defaultHitEffect;
        [SerializeField] private AudioClip defaultHitSound;
        [SerializeField] private HitEffectInfo[] overridHitEffects;
        [SerializeField] private float lifeTime = 5f;
        [SerializeField] private int maxHit = 1;

        private BoxCollider2D hitBox;

        private void Start()
        {
            hitBox = GetComponent<BoxCollider2D>();
            hitBox.isTrigger = true;
            GetComponent<Rigidbody2D>().gravityScale = 0;

            Destroy(gameObject, lifeTime);
        }

        private void FixedUpdate()
        {
            transform.position += speed * Time.fixedDeltaTime * transform.right;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hitActors.Count >= maxHit)
            {
                return;
            }

            Bullet bullet = other.GetComponent<Bullet>();
            if (bullet != null && bullet.owner != owner
                && ((owner.camp == Actor.Camp.Monster && bullet.owner.camp == Actor.Camp.Hero)
                    || (owner.camp == Actor.Camp.Hero && bullet.owner.camp == Actor.Camp.Monster)))
            {
                Explode(bullet.tag);
                return;
            }

            WeakPoint weakPoint = other.GetComponent<WeakPoint>();
            if (allowCritical && weakPoint != null && weakPoint.ReferenceActor != owner
                && !hitActors.Contains(weakPoint.ReferenceActor)
                && !weakPoint.ReferenceActor.IsInvincible
                && ((owner.camp == Actor.Camp.Monster && weakPoint.ReferenceActor.camp == Actor.Camp.Hero)
                    || (owner.camp == Actor.Camp.Hero && weakPoint.ReferenceActor.camp == Actor.Camp.Monster)))
            {
                hitActors.Add(weakPoint.ReferenceActor);
                weakPoint.TakeDamage(owner, this);

                if (shakeWhenCritical > 0f || shakeWhenHit > 0f)
                {
                    float shake = shakeWhenCritical > shakeWhenHit ? shakeWhenCritical : shakeWhenHit;
                    Camera.CameraController.Instance.Shake(0.5f, shake, 10, 0.1f);
                }

                Explode(weakPoint.tag);
                return;
            }

            Actor hitActor = other.GetComponent<Actor>();
            if (hitActor != null && hitActor != owner)
            {
                if (hitActor != null
                    && !hitActors.Contains(hitActor)
                    && !hitActor.IsInvincible
                    && hitActor != owner
                    && ((owner.camp == Actor.Camp.Monster && hitActor.camp == Actor.Camp.Hero)
                        || (owner.camp == Actor.Camp.Hero && hitActor.camp == Actor.Camp.Monster)))
                {
                    hitActors.Add(hitActor);
                    hitActor.TakeDamage(owner, this);

                    if (shakeWhenHit > 0f)
                    {
                        Camera.CameraController.Instance.Shake(0.5f, shakeWhenHit, 10, 0.1f);
                    }

                    Explode(hitActor.tag);
                }
            }
        }

        public void Explode(string hitObjectTag)
        {
            // 原有的視覺和音效效果代碼
            HitEffectInfo hitEffectInfo = null;

            for (int i = 0; i < overridHitEffects.Length; i++)
            {
                if (overridHitEffects[i].tagName == hitObjectTag)
                {
                    hitEffectInfo = overridHitEffects[i];
                    break;
                }
            }

            if (hitEffectInfo == null)
            {
                hitEffectInfo = new HitEffectInfo
                {
                    hitEffect = defaultHitEffect,
                    hitSound = defaultHitSound
                };
            }

            if (hitEffectInfo.hitEffect != null)
            {
                GameObject cloneEffect = Instantiate(hitEffectInfo.hitEffect, transform.position, Quaternion.identity);
                cloneEffect.gameObject.SetActive(true);
                Destroy(cloneEffect, 1f);
            }

            if (hitEffectInfo.hitSound != null)
            {
                Audio.AudioManager.Instance.PlaySound(hitEffectInfo.hitSound);
            }

            if (enableAreaDamage && explosionRadius > 0)
            {
                HashSet<int> damagedActorIDs = new HashSet<int>();
                Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);

                if (areaAffectWeakPoints)
                {
                    foreach (Collider2D collider in colliders)
                    {
                        if (collider.gameObject == gameObject)
                            continue;

                        if (hitActors.Count >= maxHit)
                        {
                            break;
                        }

                        WeakPoint weakPoint = collider.GetComponent<WeakPoint>();

                        if (weakPoint != null && weakPoint.ReferenceActor != owner &&
                            !damagedActorIDs.Contains(weakPoint.ReferenceActor.GetInstanceID()) &&
                            !hitActors.Contains(weakPoint.ReferenceActor) &&
                            !weakPoint.ReferenceActor.IsInvincible &&
                            ((owner.camp == Actor.Camp.Monster && weakPoint.ReferenceActor.camp == Actor.Camp.Hero) ||
                             (owner.camp == Actor.Camp.Hero && weakPoint.ReferenceActor.camp == Actor.Camp.Monster)))
                        {
                            weakPoint.TakeDamage(owner, this);
                            damagedActorIDs.Add(weakPoint.ReferenceActor.GetInstanceID());
                            hitActors.Add(weakPoint.ReferenceActor);
                        }
                    }
                }

                if (hitActors.Count >= maxHit)
                {
                    hitBox.enabled = false;

                    return;
                }

                foreach (Collider2D collider in colliders)
                {
                    if (collider.gameObject == gameObject)
                        continue;

                    if (hitActors.Count >= maxHit)
                    {
                        EndBullet();
                        break;
                    }

                    Actor hitActor = collider.GetComponent<Actor>();

                    if (hitActor != null && hitActor != owner && !hitActor.IsInvincible &&
                        !damagedActorIDs.Contains(hitActor.GetInstanceID()) &&
                        !hitActors.Contains(hitActor) &&
                        ((owner.camp == Actor.Camp.Monster && hitActor.camp == Actor.Camp.Hero) ||
                         (owner.camp == Actor.Camp.Hero && hitActor.camp == Actor.Camp.Monster)))
                    {
                        if (damagedActorIDs.Contains(hitActor.GetInstanceID()))
                            continue;

                        hitActor.TakeDamage(owner, this);
                        damagedActorIDs.Add(hitActor.GetInstanceID());
                        hitActors.Add(hitActor);
                    }
                }
            }

            EndBullet();
        }

        private void EndBullet()
        {
            if (destroyAfterHit)
            {
                Destroy(gameObject);
            }
            else
            {
                hitBox.enabled = false;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (enableAreaDamage && explosionRadius > 0)
            {
                // 繪製爆炸範圍
                Gizmos.color = new Color(1, 0, 0, 0.3f);
                Gizmos.DrawSphere(transform.position, explosionRadius);

                // 繪製爆炸範圍輪廓
                UnityEditor.Handles.color = Color.red;
                UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, explosionRadius);
            }
        }
#endif
    }
}
