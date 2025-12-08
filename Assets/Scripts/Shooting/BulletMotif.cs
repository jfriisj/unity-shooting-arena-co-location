// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using System.Collections;
using Meta.XR.Samples;
using MRMotifs.Shared;

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

        [Tooltip("Visual effect prefab spawned on bullet-to-bullet collision.")]
        [SerializeField] private GameObject m_bulletCollisionVFXPrefab;

        [Tooltip("Trail renderer for bullet trail effect.")]
        [SerializeField] private TrailRenderer m_trailRenderer;

        [Header("Physics Settings")]
        [Tooltip("Use gravity for realistic bullet drop.")]
        [SerializeField] private bool m_useGravity = true;

        [Tooltip("Air resistance (drag).")]
        [SerializeField] private float m_drag = 0.1f;

        [Tooltip("Chance for bullet to ricochet off walls (0-1).")]
        [SerializeField] private float m_ricochetChance = 0.3f;

        [Tooltip("Maximum impact angle for ricochet to occur (degrees).")]
        [Range(0f, 90f)]
        [SerializeField] private float m_ricochetAngleThreshold = 30f;

        [Tooltip("Energy retention multiplier on ricochet (0-1).")]
        [Range(0.1f, 1f)]
        [SerializeField] private float m_ricochetEnergyMultiplier = 0.6f;

        [Tooltip("Layer mask for wall/room mesh detection.")]
        [SerializeField] private LayerMask m_wallLayers = ~0;

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
            m_rigidbody.useGravity = m_useGravity;
            m_rigidbody.linearDamping = m_drag;
            m_rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            
            // Apply static config
            if (ConfigDamage > 0) m_damage = ConfigDamage;

            // Require serialized fields - no Resources.Load fallbacks
            // These must be assigned in the bullet prefab Inspector
            DebugLogger.RequireSerializedField(m_hitEffectPrefab, "m_hitEffectPrefab (BulletHole)", this);
            DebugLogger.RequireSerializedField(m_playerHitEffectPrefab, "m_playerHitEffectPrefab (BloodEffect)", this);
            DebugLogger.RequireSerializedField(m_hitSound, "m_hitSound", this);
        }

        public override void Spawned()
        {
            base.Spawned();
            m_spawnTime = Time.time;
            HasHit = false;
            m_previousPosition = transform.position;
            
            DebugLogger.Bullet($"Spawned | pos={transform.position:F2} vel={NetworkedVelocity:F1} owner={OwnerPlayer} lifetime={m_lifetime:F1}s", this);

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

            // Orient bullet to velocity direction (looks realistic in flight)
            if (m_rigidbody != null && m_rigidbody.linearVelocity.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(m_rigidbody.linearVelocity.normalized);
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
                
                // Check if we hit own avatar - wrap in try/catch to handle unspawned NetworkBehaviours
                if (playerHealth != null)
                {
                    try
                    {
                        // Only check OwnerPlayer if the NetworkBehaviour is spawned
                        if (playerHealth.Object != null && playerHealth.Object.IsValid && 
                            (playerHealth.OwnerPlayer == OwnerPlayer || isOwnAvatar))
                        {
                            // Hit own avatar, ignore
                            return;
                        }
                    }
                    catch (System.InvalidOperationException)
                    {
                        // PlayerHealth not spawned yet, check via NetworkObject instead
                        if (isOwnAvatar)
                        {
                            return;
                        }
                    }
                }

                // Process the hit
                HasHit = true;
                bool hitPlayer = playerHealth != null;
                
                if (hitPlayer)
                {
                    DebugLogger.Bullet($"HIT PLAYER (raycast) | damage={m_damage} target={hit.collider.gameObject.name} attacker={OwnerPlayer}", this);
                    playerHealth.ApplyDamageLocal(m_damage, OwnerPlayer);
                }
                else if (IsWallCollision(hit.collider.gameObject))
                {
                    DebugLogger.Bullet($"Hit wall (raycast) | object={hit.collider.gameObject.name} layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}", this);
                }
                else
                {
                    DebugLogger.Bullet($"Hit environment (raycast) | object={hit.collider.gameObject.name}", this);
                }

                SpawnHitEffectRpc(hit.point, hit.normal, hitPlayer, false);
                DespawnBullet();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Guard against Object being null before NetworkBehaviour is fully spawned
            if (Object == null || !Object.IsValid)
            {
                return;
            }
            
            if (!Object.HasStateAuthority || HasHit)
            {
                return;
            }
            
            // Guard against no contact points
            if (collision.contactCount == 0)
            {
                return;
            }

            var contact = collision.GetContact(0);
            var impactPoint = contact.point;
            var impactNormal = contact.normal;

            // Check if we hit another bullet
            if (collision.gameObject.CompareTag("Bullet"))
            {
                HandleBulletCollision(impactPoint, collision);
                return;
            }

            // Check if we hit a player
            var damageable = collision.gameObject.GetComponentInParent<IDamageable>();
            var networkObject = collision.gameObject.GetComponentInParent<NetworkObject>();

            // Check if we hit our own avatar
            bool isOwnAvatar = false;
            if (networkObject != null && networkObject.InputAuthority == OwnerPlayer)
            {
                isOwnAvatar = true;
            }

            if (damageable != null)
            {
                // Don't damage ourselves
                var playerHealth = damageable as PlayerHealthMotif;
                if (playerHealth == null || (playerHealth.OwnerPlayer != OwnerPlayer && !isOwnAvatar))
                {
                    HandleAvatarImpact(impactPoint, impactNormal, damageable);
                    return;
                }
                else
                {
                    // Hit our own avatar, ignore
                    return;
                }
            }

            // Check if we hit a wall/room mesh
            if (IsWallCollision(collision.gameObject))
            {
                HandleWallImpact(impactPoint, impactNormal, collision);
                return;
            }

            // Generic environment collision
            HandleGenericImpact(impactPoint, impactNormal);
        }

        /// <summary>
        /// Check if the collision is with a wall/room mesh.
        /// </summary>
        private bool IsWallCollision(GameObject obj)
        {
            bool hasTag = obj.CompareTag("RoomMesh");
            bool hasLayer = ((1 << obj.layer) & m_wallLayers) != 0;
            
            if (hasTag || hasLayer)
            {
                DebugLogger.Bullet($"Wall detected | object={obj.name} tag={obj.tag} layer={LayerMask.LayerToName(obj.layer)}", this);
            }
            
            return hasTag || hasLayer;
        }

        /// <summary>
        /// Handle bullet-to-bullet collision.
        /// </summary>
        private void HandleBulletCollision(Vector3 point, Collision collision)
        {
            if (m_bulletCollisionVFXPrefab != null)
            {
                SpawnHitEffectRpc(point, Vector3.up, false, true);
            }
            else
            {
                SpawnHitEffectRpc(point, Vector3.up, false, false);
            }
            
            HasHit = true;
            DespawnBullet();
        }

        /// <summary>
        /// Handle wall impact with optional ricochet.
        /// </summary>
        private void HandleWallImpact(Vector3 point, Vector3 normal, Collision collision)
        {
            // Guard against null rigidbody
            if (m_rigidbody == null)
            {
                HasHit = true;
                SpawnHitEffectRpc(point, normal, false, false);
                DespawnBullet();
                return;
            }
            
            // Check for ricochet based on impact angle
            float impactAngle = Vector3.Angle(-m_rigidbody.linearVelocity.normalized, normal);
            
            if (impactAngle < m_ricochetAngleThreshold && UnityEngine.Random.value < m_ricochetChance)
            {
                // Ricochet! Reflect velocity
                Vector3 reflectedVelocity = Vector3.Reflect(m_rigidbody.linearVelocity, normal);
                m_rigidbody.linearVelocity = reflectedVelocity * m_ricochetEnergyMultiplier;
                
                // Spawn smaller ricochet spark
                SpawnHitEffectRpc(point, normal, false, false);
                
                Debug.Log($"[BulletMotif] Bullet ricocheted at angle {impactAngle}°");
                // Don't set HasHit or despawn - bullet continues
            }
            else
            {
                // Full impact - destroy bullet
                HasHit = true;
                SpawnHitEffectRpc(point, normal, false, false);
                DespawnBullet();
            }
        }

        /// <summary>
        /// Handle avatar/player impact.
        /// </summary>
        private void HandleAvatarImpact(Vector3 point, Vector3 normal, IDamageable damageable)
        {
            HasHit = true;
            
            // Apply damage through IDamageable interface
            damageable.TakeDamage(m_damage, point, normal);
            
            // Spawn player hit effect
            SpawnHitEffectRpc(point, normal, true, false);
            DespawnBullet();
        }

        /// <summary>
        /// Handle generic environment impact.
        /// </summary>
        private void HandleGenericImpact(Vector3 point, Vector3 normal)
        {
            HasHit = true;
            SpawnHitEffectRpc(point, normal, false, false);
            DespawnBullet();
        }

        private void OnTriggerEnter(Collider other)
        {
            // Guard against Object being null before NetworkBehaviour is fully spawned
            if (Object == null || !Object.IsValid)
            {
                return;
            }
            
            DebugLogger.Bullet($"Trigger | object={other.gameObject.name} auth={Object.HasStateAuthority} hasHit={HasHit}", this);

            if (!Object.HasStateAuthority || HasHit)
            {
                return;
            }

            // Check if we hit a player by looking for PlayerHealthMotif or NetworkObject
            var playerHealth = other.GetComponentInParent<PlayerHealthMotif>();
            var networkObject = other.GetComponentInParent<NetworkObject>();
            
            DebugLogger.Bullet($"Trigger check | playerHealth={playerHealth != null} networkObj={networkObject != null}", this);
            
            bool hitPlayer = false;
            bool isOwnAvatar = false;

            // Determine if this is the bullet owner's own avatar
            if (networkObject != null)
            {
                DebugLogger.Bullet($"Owner check | inputAuth={networkObject.InputAuthority} bulletOwner={OwnerPlayer}", this);
                
                // Better check: compare input authority with bullet owner
                if (networkObject.InputAuthority == OwnerPlayer)
                {
                    isOwnAvatar = true;
                }
            }
            
            if (playerHealth != null)
            {
                DebugLogger.Bullet($"Player check | healthOwner={playerHealth.OwnerPlayer} bulletOwner={OwnerPlayer} isOwn={isOwnAvatar}", this);
                
                // Use both checks - OwnerPlayer or InputAuthority
                if (playerHealth.OwnerPlayer == OwnerPlayer || isOwnAvatar)
                {
                    // Hit our own collider - ignore and continue
                    DebugLogger.Bullet("Ignored own avatar", this);
                    return;
                }
                
                DebugLogger.Bullet($"HIT PLAYER (trigger) | damage={m_damage} attacker={OwnerPlayer}", this);
                // Apply damage directly (local) since PlayerHealthMotif may not have working RPCs
                playerHealth.ApplyDamageLocal(m_damage, OwnerPlayer);
                hitPlayer = true;
            }
            else if (isOwnAvatar)
            {
                // Hit own avatar but no PlayerHealth found - still ignore
                DebugLogger.Bullet("Ignored own avatar (no health component)", this);
                return;
            }

            HasHit = true;

            // Spawn hit effect and despawn
            DebugLogger.Bullet($"Despawning | hitPlayer={hitPlayer}", this);
            SpawnHitEffectRpc(transform.position, -transform.forward, hitPlayer, false);
            DespawnBullet();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void SpawnHitEffectRpc(Vector3 position, Vector3 normal, NetworkBool isPlayerHit, NetworkBool isBulletCollision)
        {
            GameObject effectPrefab;
            
            // Choose appropriate effect
            if (isBulletCollision && m_bulletCollisionVFXPrefab != null)
            {
                effectPrefab = m_bulletCollisionVFXPrefab;
            }
            else if (isPlayerHit && m_playerHitEffectPrefab != null)
            {
                effectPrefab = m_playerHitEffectPrefab;
            }
            else if (m_hitEffectPrefab != null)
            {
                effectPrefab = m_hitEffectPrefab;
            }
            else
            {
                effectPrefab = null;
            }
            
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
