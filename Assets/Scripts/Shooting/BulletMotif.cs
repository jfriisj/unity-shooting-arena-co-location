// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using System.Collections;
using Meta.XR.Samples;

namespace MRMotifs.SharedActivities.ShootingSample
{
    /// <summary>
    /// Represents a networked bullet/projectile in the shooting game.
    /// Handles physics, collision detection, hit registration, and visual effects.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    [RequireComponent(typeof(Rigidbody))]
    public class BulletMotif : NetworkBehaviour
    {
        // Static configuration from ShootingGameConfigMotif
        public static int ConfigDamage = 10;

        [Header("Bullet Settings")]
        [Tooltip("Damage dealt on hit.")]
        [SerializeField] private int m_damage = 10;

        [Tooltip("Visual effect prefab spawned on impact.")]
        [SerializeField] private GameObject m_hitEffectPrefab;

        [Tooltip("Bullet hole decal prefab spawned on surface impact (Easy FPS bulletHole).")]
        [SerializeField] private GameObject m_bulletHolePrefab;

        [Tooltip("Trail renderer for bullet trail effect.")]
        [SerializeField] private TrailRenderer m_trailRenderer;

        [Tooltip("How long bullet holes stay before fading.")]
        [SerializeField] private float m_bulletHoleLifetime = 10f;

        [Header("Audio")]
        [Tooltip("Sound played on impact.")]
        [SerializeField] private AudioClip m_hitSound;

        /// <summary>
        /// The player who fired this bullet.
        /// </summary>
        [Networked] public PlayerRef OwnerPlayer { get; set; }

        /// <summary>
        /// The velocity of the bullet, synchronized across clients.
        /// </summary>
        [Networked] private Vector3 NetworkedVelocity { get; set; }

        /// <summary>
        /// Whether this bullet has already hit something.
        /// </summary>
        [Networked] private NetworkBool HasHit { get; set; }

        private Rigidbody m_rigidbody;
        private float m_lifetime;
        private float m_spawnTime;

        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody>();
            m_rigidbody.useGravity = false;
            m_rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            
            // Apply static config
            if (ConfigDamage > 0) m_damage = ConfigDamage;

            // Load hit sound from Resources if not assigned
            if (m_hitSound == null)
            {
                m_hitSound = Resources.Load<AudioClip>("Audio/Hit");
            }
        }

        public override void Spawned()
        {
            base.Spawned();
            m_spawnTime = Time.time;
            HasHit = false;

            // Apply velocity if we're the authority
            if (Object.HasStateAuthority && NetworkedVelocity != Vector3.zero)
            {
                m_rigidbody.linearVelocity = NetworkedVelocity;
            }
        }

        /// <summary>
        /// Initialize the bullet with owner, velocity, and lifetime.
        /// Called by ShootingPlayerMotif after spawning.
        /// </summary>
        public void Initialize(PlayerRef owner, Vector3 velocity, float lifetime)
        {
            OwnerPlayer = owner;
            NetworkedVelocity = velocity;
            m_lifetime = lifetime;

            if (m_rigidbody != null)
            {
                m_rigidbody.linearVelocity = velocity;
            }

            // Start despawn timer
            StartCoroutine(DespawnAfterLifetime());
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority)
            {
                return;
            }

            // Check if bullet has exceeded lifetime
            if (Time.time - m_spawnTime > m_lifetime)
            {
                DespawnBullet();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!Object.HasStateAuthority || HasHit)
            {
                return;
            }

            HasHit = true;

            // Check if we hit a player
            var playerHealth = collision.gameObject.GetComponentInParent<PlayerHealthMotif>();
            bool hitPlayer = false;
            if (playerHealth != null)
            {
                hitPlayer = true;
                // Don't damage ourselves
                if (playerHealth.OwnerPlayer != OwnerPlayer)
                {
                    // Send damage RPC
                    playerHealth.TakeDamageRpc(m_damage, OwnerPlayer);
                }
            }

            // Spawn hit effect and despawn
            SpawnHitEffectRpc(collision.contacts[0].point, collision.contacts[0].normal, hitPlayer);
            DespawnBullet();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!Object.HasStateAuthority || HasHit)
            {
                return;
            }

            HasHit = true;

            // Check if we hit a player
            var playerHealth = other.GetComponentInParent<PlayerHealthMotif>();
            bool hitPlayer = false;
            if (playerHealth != null)
            {
                hitPlayer = true;
                // Don't damage ourselves
                if (playerHealth.OwnerPlayer != OwnerPlayer)
                {
                    playerHealth.TakeDamageRpc(m_damage, OwnerPlayer);
                }
            }

            // Spawn hit effect and despawn
            SpawnHitEffectRpc(transform.position, -transform.forward, hitPlayer);
            DespawnBullet();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void SpawnHitEffectRpc(Vector3 position, Vector3 normal, NetworkBool hitPlayer)
        {
            if (m_hitEffectPrefab != null)
            {
                var effect = Instantiate(m_hitEffectPrefab, position, Quaternion.LookRotation(normal));
                Destroy(effect, 2f);
            }

            // Spawn bullet hole on surfaces (not on players)
            if (!hitPlayer && m_bulletHolePrefab != null)
            {
                // Offset slightly from surface to prevent z-fighting
                var holePosition = position + normal * 0.001f;
                var bulletHole = Instantiate(m_bulletHolePrefab, holePosition, Quaternion.LookRotation(-normal));
                
                // Random rotation around normal for variety
                bulletHole.transform.Rotate(0, 0, Random.Range(0f, 360f), Space.Self);
                
                Destroy(bulletHole, m_bulletHoleLifetime);
            }

            if (m_hitSound != null)
            {
                AudioSource.PlayClipAtPoint(m_hitSound, position);
            }
        }

        private IEnumerator DespawnAfterLifetime()
        {
            yield return new WaitForSeconds(m_lifetime);

            if (Object != null && Object.IsValid && Object.HasStateAuthority)
            {
                DespawnBullet();
            }
        }

        private void DespawnBullet()
        {
            if (Object != null && Object.IsValid && Runner != null)
            {
                Runner.Despawn(Object);
            }
        }

        /// <summary>
        /// Set the damage value for this bullet.
        /// </summary>
        public void SetDamage(int damageAmount)
        {
            m_damage = damageAmount;
        }

        /// <summary>
        /// Set the bullet hole prefab for surface impacts.
        /// Use Easy FPS bulletHole.prefab for decal effects.
        /// </summary>
        public void SetBulletHolePrefab(GameObject prefab)
        {
            m_bulletHolePrefab = prefab;
        }

        /// <summary>
        /// Set the hit effect prefab for impact visuals.
        /// </summary>
        public void SetHitEffectPrefab(GameObject prefab)
        {
            m_hitEffectPrefab = prefab;
        }
    }
}
#endif
