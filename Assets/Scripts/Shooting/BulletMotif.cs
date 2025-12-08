// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Netcode;
using UnityEngine;
using System.Collections;
using Meta.XR.Samples;
using MRMotifs.Shared;

namespace MRMotifs.Shooting
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
        [SerializeField] private bool m_useGravity = false;

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
        /// The player who fired this bullet (client ID).
        /// </summary>
        public NetworkVariable<ulong> OwnerPlayer = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        /// <summary>
        /// The velocity of the bullet, synchronized across clients.
        /// </summary>
        private NetworkVariable<Vector3> NetworkedVelocity = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Whether this bullet has already hit something.
        /// </summary>
        private NetworkVariable<bool> HasHit = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private PlayerHealthMotif m_shooterPlayerReference;

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
            DebugLogger.RequireSerializedField(m_hitEffectPrefab, "m_hitEffectPrefab (BulletHole)", this);
            DebugLogger.RequireSerializedField(m_playerHitEffectPrefab, "m_playerHitEffectPrefab (BloodEffect)", this);
            DebugLogger.RequireSerializedField(m_hitSound, "m_hitSound", this);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            m_spawnTime = Time.time;
            HasHit.Value = false;
            m_previousPosition = transform.position;
            
            DebugLogger.Bullet($"Spawned | pos={transform.position:F2} vel={NetworkedVelocity.Value:F1} owner={OwnerPlayer.Value} lifetime={m_lifetime:F1}s", this);

            // Resolve shooter reference
            if (OwnerPlayer.Value != 0)
            {
                var players = FindObjectsByType<PlayerHealthMotif>(FindObjectsSortMode.None);
                foreach (var player in players)
                {
                    if (player.NetworkObject != null && player.NetworkObject.OwnerClientId == OwnerPlayer.Value)
                    {
                        m_shooterPlayerReference = player;
                        break;
                    }
                }
            }

            // Apply velocity if we're the owner
            if (IsOwner && NetworkedVelocity.Value != Vector3.zero)
            {
                m_rigidbody.linearVelocity = NetworkedVelocity.Value;
            }
        }

        /// <summary>
        /// Initialize the bullet with owner, velocity, and lifetime.
        /// Called by ShootingPlayerMotif after spawning.
        /// </summary>
        public void Initialize(ulong ownerClientId, Vector3 velocity, float lifetime)
        {
            OwnerPlayer.Value = ownerClientId;
            NetworkedVelocity.Value = velocity;
            m_lifetime = lifetime;
            m_previousPosition = transform.position;

            if (m_rigidbody != null)
            {
                m_rigidbody.linearVelocity = velocity;
            }

            // Start despawn timer
            StartCoroutine(DespawnAfterLifetime());
        }

        private void FixedUpdate()
        {
            if (!IsOwner)
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
            if (!HasHit.Value)
            {
                CheckRaycastCollision();
            }

            // Update previous position for next frame's raycast
            m_previousPosition = transform.position;
        }

        /// <summary>
        /// Uses raycast to detect collisions between previous and current position.
        /// </summary>
        private void CheckRaycastCollision()
        {
            Vector3 currentPosition = transform.position;
            Vector3 direction = currentPosition - m_previousPosition;
            float distance = direction.magnitude;

            if (distance < 0.001f)
            {
                return;
            }

            if (Physics.Raycast(m_previousPosition, direction.normalized, out RaycastHit hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            {
                var playerHealth = hit.collider.GetComponentInParent<PlayerHealthMotif>();
                var networkObject = hit.collider.GetComponentInParent<NetworkObject>();
                
                bool isOwnAvatar = false;
                if (networkObject != null && networkObject.OwnerClientId == OwnerPlayer.Value)
                {
                    isOwnAvatar = true;
                }
                
                if (playerHealth != null)
                {
                    if (playerHealth.NetworkObject != null && 
                        (playerHealth.OwnerPlayer.Value == OwnerPlayer.Value || isOwnAvatar))
                    {
                        return;
                    }
                }

                HasHit.Value = true;
                bool hitPlayer = playerHealth != null;
                
                if (hitPlayer)
                {
                    DebugLogger.Bullet($"HIT PLAYER (raycast) | damage={m_damage} target={hit.collider.gameObject.name} attacker={OwnerPlayer.Value}", this);
                    playerHealth.ApplyDamageLocal(m_damage, OwnerPlayer.Value);
                }

                RequestSpawnHitEffectServerRpc(hit.point, hit.normal, hitPlayer, false);
                DespawnBullet();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (NetworkObject == null || !IsSpawned)
            {
                return;
            }
            
            if (!IsOwner || HasHit.Value)
            {
                return;
            }
            
            if (collision.contactCount == 0)
            {
                return;
            }

            var contact = collision.GetContact(0);
            var impactPoint = contact.point;
            var impactNormal = contact.normal;

            if (collision.gameObject.CompareTag("Bullet"))
            {
                HandleBulletCollision(impactPoint, collision);
                return;
            }

            var damageable = collision.gameObject.GetComponentInParent<IDamageable>();
            var networkObject = collision.gameObject.GetComponentInParent<NetworkObject>();

            bool isOwnAvatar = false;
            if (networkObject != null && networkObject.OwnerClientId == OwnerPlayer.Value)
            {
                isOwnAvatar = true;
            }

            if (damageable != null)
            {
                var playerHealth = damageable as PlayerHealthMotif;
                if (playerHealth == null || (playerHealth.OwnerPlayer.Value != OwnerPlayer.Value && !isOwnAvatar))
                {
                    HandleAvatarImpact(impactPoint, impactNormal, damageable);
                    return;
                }
                else
                {
                    return;
                }
            }

            if (IsWallCollision(collision.gameObject))
            {
                HandleWallImpact(impactPoint, impactNormal, collision);
                return;
            }

            HandleGenericImpact(impactPoint, impactNormal);
        }

        private bool IsWallCollision(GameObject obj)
        {
            bool hasTag = obj.CompareTag("RoomMesh");
            bool hasLayer = ((1 << obj.layer) & m_wallLayers) != 0;
            return hasTag || hasLayer;
        }

        private void HandleBulletCollision(Vector3 point, Collision collision)
        {
            if (m_bulletCollisionVFXPrefab != null)
            {
                RequestSpawnHitEffectServerRpc(point, Vector3.up, false, true);
            }
            else
            {
                RequestSpawnHitEffectServerRpc(point, Vector3.up, false, false);
            }
            
            HasHit.Value = true;
            DespawnBullet();
        }

        private void HandleWallImpact(Vector3 point, Vector3 normal, Collision collision)
        {
            if (m_rigidbody == null)
            {
                HasHit.Value = true;
                RequestSpawnHitEffectServerRpc(point, normal, false, false);
                DespawnBullet();
                return;
            }
            
            float impactAngle = Vector3.Angle(-m_rigidbody.linearVelocity.normalized, normal);
            
            if (impactAngle < m_ricochetAngleThreshold && Random.value < m_ricochetChance)
            {
                Vector3 reflectedVelocity = Vector3.Reflect(m_rigidbody.linearVelocity, normal);
                m_rigidbody.linearVelocity = reflectedVelocity * m_ricochetEnergyMultiplier;
                RequestSpawnHitEffectServerRpc(point, normal, false, false);
            }
            else
            {
                HasHit.Value = true;
                RequestSpawnHitEffectServerRpc(point, normal, false, false);
                DespawnBullet();
            }
        }

        private void HandleAvatarImpact(Vector3 point, Vector3 normal, IDamageable damageable)
        {
            HasHit.Value = true;
            
            IDamageable.DamageCallback damageCallback = (affected, damage, died) =>
            {
                if (died && affected is DroneMotif)
                {
                    var shooterStats = m_shooterPlayerReference?.PlayerStats;
                    if (shooterStats != null)
                    {
                        shooterStats.AddDroneKill();
                    }
                }
                
                var shooterPlayerStats = m_shooterPlayerReference?.PlayerStats;
                if (shooterPlayerStats != null)
                {
                    shooterPlayerStats.RecordShotHit(damage);
                }
            };
            
            damageable.TakeDamage(m_damage, point, normal, damageCallback);
            RequestSpawnHitEffectServerRpc(point, normal, true, false);
            DespawnBullet();
        }

        private void HandleGenericImpact(Vector3 point, Vector3 normal)
        {
            HasHit.Value = true;
            RequestSpawnHitEffectServerRpc(point, normal, false, false);
            DespawnBullet();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (NetworkObject == null || !IsSpawned)
            {
                return;
            }

            if (!IsOwner || HasHit.Value)
            {
                return;
            }

            var playerHealth = other.GetComponentInParent<PlayerHealthMotif>();
            var networkObject = other.GetComponentInParent<NetworkObject>();
            
            bool hitPlayer = false;
            bool isOwnAvatar = false;

            if (networkObject != null)
            {
                if (networkObject.OwnerClientId == OwnerPlayer.Value)
                {
                    isOwnAvatar = true;
                }
            }
            
            if (playerHealth != null)
            {
                if (playerHealth.OwnerPlayer.Value == OwnerPlayer.Value || isOwnAvatar)
                {
                    return;
                }
                
                playerHealth.ApplyDamageLocal(m_damage, OwnerPlayer.Value);
                hitPlayer = true;
            }
            else if (isOwnAvatar)
            {
                return;
            }

            HasHit.Value = true;
            RequestSpawnHitEffectServerRpc(transform.position, -transform.forward, hitPlayer, false);
            DespawnBullet();
        }

        [ServerRpc]
        private void RequestSpawnHitEffectServerRpc(Vector3 position, Vector3 normal, bool isPlayerHit, bool isBulletCollision)
        {
            SpawnHitEffectClientRpc(position, normal, isPlayerHit, isBulletCollision);
        }

        [ClientRpc]
        private void SpawnHitEffectClientRpc(Vector3 position, Vector3 normal, bool isPlayerHit, bool isBulletCollision)
        {
            GameObject effectPrefab;
            
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

            if (NetworkObject != null && IsSpawned && IsOwner)
            {
                DespawnBullet();
            }
        }

        private void DespawnBullet()
        {
            if (NetworkObject != null && IsSpawned && IsServer)
            {
                NetworkObject.Despawn();
            }
            else if (IsOwner && NetworkManager.Singleton != null)
            {
                // Request server to despawn
                RequestDespawnServerRpc();
            }
        }

        [ServerRpc]
        private void RequestDespawnServerRpc()
        {
            if (NetworkObject != null && IsSpawned)
            {
                NetworkObject.Despawn();
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
