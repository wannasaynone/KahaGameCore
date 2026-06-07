using System;
using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.ActorSystem
{
    public enum BulletHitMode
    {
        Single,
        Penetrate,
        Periodic
    }

    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Bullet : MonoBehaviour
    {
        [SerializeField] private BulletHitMode hitMode = BulletHitMode.Single;
        [SerializeField] private float hitCooldown = 1f;

        private float _speed;
        private float _lifetime;
        private bool _isInitialized;
        private bool _hasHit;

        private string _faction;
        private FactionCollisionTable _collisionTable;
        private AGameActor _shooter;

        // Periodic mode: track cooldown end time per collider
        private readonly Dictionary<Collider2D, float> _periodicCooldowns = new();

        // Bullet-side hit predicates: all must return true for the hit to register
        private readonly List<Func<AGameActor, bool>> _hitPredicates = new();

        public void Initialize(float speed, float lifetime, string faction, FactionCollisionTable collisionTable, AGameActor shooter = null, params Func<AGameActor, bool>[] hitPredicates)
        {
            _speed = speed;
            _lifetime = lifetime;
            _faction = faction;
            _collisionTable = collisionTable;
            _shooter = shooter;
            _isInitialized = true;

            _hitPredicates.Clear();
            if (hitPredicates != null)
            {
                foreach (var p in hitPredicates)
                    _hitPredicates.Add(p);
            }

            Rigidbody2D rg = GetComponent<Rigidbody2D>();
            rg.bodyType = RigidbodyType2D.Kinematic;
            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void Update()
        {
            if (!_isInitialized) return;

            transform.position += transform.right * (_speed * Time.deltaTime);

            _lifetime -= Time.deltaTime;
            if (_lifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            // Clean up expired periodic cooldowns
            if (hitMode == BulletHitMode.Periodic)
            {
                List<Collider2D> toRemove = null;
                foreach (var kv in _periodicCooldowns)
                {
                    if (Time.time >= kv.Value)
                    {
                        toRemove ??= new List<Collider2D>();
                        toRemove.Add(kv.Key);
                    }
                }
                if (toRemove != null)
                {
                    foreach (var key in toRemove)
                        _periodicCooldowns.Remove(key);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isInitialized) return;
            if (_collisionTable == null) return;

            // Single mode: already hit once, ignore further collisions
            if (hitMode == BulletHitMode.Single && _hasHit) return;

            // Periodic mode: check cooldown
            if (hitMode == BulletHitMode.Periodic)
            {
                if (_periodicCooldowns.TryGetValue(other, out float cooldownEnd) && Time.time < cooldownEnd)
                    return;
            }

            // Get hitbox bridge component directly on the collider's GameObject
            ActorHitbox hitbox = other.GetComponent<ActorHitbox>();
            if (hitbox == null)
            {
                return;
            }

            if (hitbox.Actor == null)
            {
                UnityEngine.Debug.Log("[Bullet] " + gameObject.name + " hit " + other.gameObject.name + ", but hitbox doesn't ref a actor, will skip");
                return;
            }

            string targetFaction = hitbox.Actor.Faction;
            FactionCollisionResult result = _collisionTable.GetCollisionResult(_faction, targetFaction);

            if (result == FactionCollisionResult.Skip) return;

            // Bullet-side predicate check: all predicates must pass
            foreach (var predicate in _hitPredicates)
            {
                if (!predicate(hitbox.Actor)) return;
            }

            // Target-side filter check: actor decides if it can be hit
            BulletHitContext context = new BulletHitContext(_faction, transform.position, _shooter);
            if (!hitbox.Actor.CanBeHitByBullet(context)) return;

            // Explode: notify target via hitbox and spawn effect
            hitbox.OnHit(context);

            GameObject explosionPrefab = _collisionTable.GetExplosionPrefab(_faction, targetFaction);
            if (explosionPrefab != null)
            {
                GameObject vfxInstance = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                Destroy(vfxInstance, 2f);
            }

            // Handle hit mode post-collision
            switch (hitMode)
            {
                case BulletHitMode.Single:
                    _hasHit = true;
                    Destroy(gameObject);
                    break;
                case BulletHitMode.Penetrate:
                    // Keep flying, do nothing
                    break;
                case BulletHitMode.Periodic:
                    _periodicCooldowns[other] = Time.time + hitCooldown;
                    break;
            }
        }
    }
}
