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

        [Tooltip("Visual effect prefab spawned on environment impact (bullet hole).")]
        [SerializeField] private GameObject m_hitEffectPrefab;

        [Tooltip("Visual effect prefab spawned on player/avatar impact (blood effect).")]
        [SerializeField] private GameObject m_playerHitEffectPrefab;

        [Tooltip("Trail renderer for bullet trail effect.")]
        [SerializeField] private TrailRenderer m_trailRenderer;

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
        private Vector3 m_previousPosition;

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

            // Load bullet hole prefab from Resources if not assigned
            if (m_hitEffectPrefab == null)
            {
                m_hitEffectPrefab = Resources.Load<GameObject>("BulletHole");
            }

            // Load player hit effect (blood) from Resources if not assigned
            if (m_playerHitEffectPrefab == null)
            {
                m_playerHitEffectPrefab = Resources.Load<GameObject>("BloodEffect");
            }
        }

        public override void Spawned()
        {
            base.Spawned();
            m_spawnTime = Time.time;
            HasHit = false;
            m_previousPosition = transform.position;

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
            m_previousPosition = transform.position;

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
                return;
            }

            // Raycast-based collision detection to catch fast-moving bullet hits
            if (!HasHit)
            {
                CheckRaycastCollision();
            }

            // Update previous position for next frame's raycast
            m_previousPosition = transform.position;
        }

        /// <summary>
        /// Uses raycast to detect collisions between previous and current position.
        /// This catches collisions that physics triggers might miss due to high bullet speed.
        /// </summary>
        private void CheckRaycastCollision()
        {
            Vector3 currentPosition = transform.position;
            Vector3 direction = currentPosition - m_previousPosition;
            float distance = direction.magnitude;

            if (distance < 0.001f)
            {
                return; // Not enough movement to check
            }

            // Use RaycastAll to see everything the ray hits for debugging
            RaycastHit[] hits = Physics.RaycastAll(m_previousPosition, direction.normalized, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            
            if (hits.Length > 0)
            {
                Debug.Log($"[BulletMotif] Raycast hit {hits.Length} objects:");
                foreach (var h in hits)
                {
                    Debug.Log($"  - {h.collider.gameObject.name} (layer: {h.collider.gameObject.layer}, isTrigger: {h.collider.isTrigger})");
                }
            }

            // Cast a ray from previous position to current position
            // Use QueryTriggerInteraction.Collide to hit trigger colliders (avatar colliders are triggers)
            if (Physics.Raycast(m_previousPosition, direction.normalized, out RaycastHit hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            {
                // Check if we hit a player
                var playerHealth = hit.collider.GetComponentInParent<PlayerHealthMotif>();
                var networkObject = hit.collider.GetComponentInParent<NetworkObject>();
                
                bool isOwnAvatar = false;
                if (networkObject != null && networkObject.InputAuthority == OwnerPlayer)
                {
                    isOwnAvatar = true;
                }
                
                if (playerHealth != null && (playerHealth.OwnerPlayer == OwnerPlayer || isOwnAvatar))
                {
                    // Hit own avatar, ignore
                    return;
                }

                // Process the hit
                HasHit = true;
                bool hitPlayer = playerHealth != null;
                
                if (hitPlayer)
                {
                    Debug.Log($"[BulletMotif] Raycast hit player! Dealing {m_damage} damage");
                    // Apply damage directly (local) since PlayerHealthMotif may not have working RPCs
                    playerHealth.ApplyDamageLocal(m_damage, OwnerPlayer);
                }
                else
                {
                    Debug.Log($"[BulletMotif] Raycast hit environment: {hit.collider.gameObject.name}");
                }

                SpawnHitEffectRpc(hit.point, hit.normal, hitPlayer);
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
                // Don't damage ourselves
                if (playerHealth.OwnerPlayer != OwnerPlayer)
                {
                    // Apply damage directly (local) since PlayerHealthMotif may not have working RPCs
                    playerHealth.ApplyDamageLocal(m_damage, OwnerPlayer);
                    hitPlayer = true;
                }
            }

            // Spawn hit effect and despawn
            SpawnHitEffectRpc(collision.contacts[0].point, collision.contacts[0].normal, hitPlayer);
            DespawnBullet();
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[BulletMotif] OnTriggerEnter - other: {other.gameObject.name}, HasStateAuthority: {Object.HasStateAuthority}, HasHit: {HasHit}");

            if (!Object.HasStateAuthority || HasHit)
            {
                return;
            }

            // Check if we hit a player by looking for PlayerHealthMotif or NetworkObject
            var playerHealth = other.GetComponentInParent<PlayerHealthMotif>();
            var networkObject = other.GetComponentInParent<NetworkObject>();
            
            Debug.Log($"[BulletMotif] PlayerHealth found: {playerHealth != null}, NetworkObject found: {networkObject != null}");
            
            bool hitPlayer = false;
            bool isOwnAvatar = false;

            // Determine if this is the bullet owner's own avatar
            if (networkObject != null)
            {
                // Check if this NetworkObject belongs to the bullet owner
                isOwnAvatar = networkObject.HasStateAuthority && Object.HasStateAuthority;
                Debug.Log($"[BulletMotif] NetworkObject.InputAuthority: {networkObject.InputAuthority}, isOwnAvatar: {isOwnAvatar}");
                
                // Better check: compare input authority with bullet owner
                if (networkObject.InputAuthority == OwnerPlayer)
                {
                    isOwnAvatar = true;
                }
            }
            
            if (playerHealth != null)
            {
                Debug.Log($"[BulletMotif] PlayerHealth.OwnerPlayer: {playerHealth.OwnerPlayer}, Bullet.OwnerPlayer: {OwnerPlayer}");
                
                // Use both checks - OwnerPlayer or InputAuthority
                if (playerHealth.OwnerPlayer == OwnerPlayer || isOwnAvatar)
                {
                    // Hit our own collider - ignore and continue
                    Debug.Log($"[BulletMotif] Hit own avatar, ignoring");
                    return;
                }
                
                Debug.Log($"[BulletMotif] Dealing damage to player!");
                // Apply damage directly (local) since PlayerHealthMotif may not have working RPCs
                playerHealth.ApplyDamageLocal(m_damage, OwnerPlayer);
                hitPlayer = true;
            }
            else if (isOwnAvatar)
            {
                // Hit own avatar but no PlayerHealth found - still ignore
                Debug.Log($"[BulletMotif] Hit own avatar (no PlayerHealth), ignoring");
                return;
            }

            HasHit = true;

            // Spawn hit effect and despawn
            Debug.Log($"[BulletMotif] Spawning hit effect, hitPlayer: {hitPlayer}");
            SpawnHitEffectRpc(transform.position, -transform.forward, hitPlayer);
            DespawnBullet();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void SpawnHitEffectRpc(Vector3 position, Vector3 normal, NetworkBool isPlayerHit)
        {
            // Use different effect for player hits vs environment hits
            GameObject effectPrefab = isPlayerHit ? m_playerHitEffectPrefab : m_hitEffectPrefab;
            
            if (effectPrefab != null)
            {
                var effect = Instantiate(effectPrefab, position, Quaternion.LookRotation(normal));
                Destroy(effect, 2f);
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
    }
}
#endif
