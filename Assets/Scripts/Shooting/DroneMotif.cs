// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Meta.XR.Samples;
using MRMotifs.Shared;

namespace MRMotifs.Shooting
{
    /// <summary>
    /// Simplified drone enemy for the shooting game.
    /// Based on Discover/DroneRage Enemy.cs but streamlined for simplicity.
    /// Implements IDamageable for bullet hit detection.
    /// Server authority - only runs physics/AI on server.
    /// Converted from Photon Fusion to Unity NGO.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    [RequireComponent(typeof(NetworkObject), typeof(Rigidbody), typeof(Collider))]
    public class DroneMotif : NetworkBehaviour, IDamageable
    {
        /// <summary>
        /// Static configuration values set by ShootingGameConfigMotif.
        /// </summary>
        public static int ConfigHealth { get; set; } = 50;
        public static float ConfigSpeed { get; set; } = 3f;

        [Header("=== DRONE SETTINGS ===")]
        
        [SerializeField, Tooltip("Maximum health for this drone")]
        private float m_maxHealth = 50f;
        
        [SerializeField, Tooltip("Speed when moving towards player")]
        private float m_chaseSpeed = 3f;
        
        [SerializeField, Tooltip("Radius for hover movement noise")]
        private float m_hoverRadius = 0.5f;
        
        [SerializeField, Tooltip("Frequency of hover noise")]
        private Vector3 m_hoverNoiseFrequency = new Vector3(0.2f, 0.2f, 0.2f);
        
        [SerializeField, Tooltip("Maximum acceleration for movement")]
        private float m_maxAcceleration = 8f;
        
        [Header("=== REFERENCES ===")]
        
        [SerializeField, Tooltip("Visual effects to spawn when drone dies")]
        private GameObject m_deathEffectPrefab;

        /// <summary>
        /// Current health of the drone. Networked for all clients.
        /// </summary>
        public NetworkVariable<float> Health = new NetworkVariable<float>(
            50f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        /// <summary>
        /// Current target player. Only meaningful on server.
        /// </summary>
        public PlayerHealthMotif TargetPlayer { get; private set; }

        /// <summary>
        /// Event fired when drone takes damage.
        /// </summary>
        public static event Action<DroneMotif, float> OnDroneDamaged;
        
        /// <summary>
        /// Event fired when drone is destroyed.
        /// </summary>
        public static event Action<DroneMotif> OnDroneDestroyed;

        private Rigidbody m_rigidbody;
        private DroneAIMotif m_ai;
        private float m_hoverNoiseOffset;
        private bool m_isDead = false;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Initialize health from config
            if (IsServer)
            {
                Health.Value = ConfigHealth > 0 ? ConfigHealth : m_maxHealth;
            }
            
            // Apply configured speed
            if (ConfigSpeed > 0) m_chaseSpeed = ConfigSpeed;
            
            m_rigidbody = GetComponent<Rigidbody>();
            m_ai = GetComponent<DroneAIMotif>();
            
            // Randomize hover for organic movement
            m_hoverNoiseOffset = UnityEngine.Random.Range(-3600f, 3600f);
            
            DebugLogger.Shooting($"Drone spawned | Health={Health.Value} | IsServer={IsServer}", this);

            // Only server runs drone physics and AI
            if (!IsServer)
            {
                m_rigidbody.isKinematic = true;
                if (m_ai) m_ai.enabled = false;
                return;
            }

            // Find initial target player
            FindTargetPlayer();
            
            DebugLogger.Shooting($"Drone initialized on server | Target={TargetPlayer?.name}", this);
        }

        private void FindTargetPlayer()
        {
            // Find closest alive player
            var players = FindObjectsByType<PlayerHealthMotif>(FindObjectsSortMode.None);
            PlayerHealthMotif closestPlayer = null;
            float closestDistance = float.MaxValue;

            foreach (var player in players)
            {
                if (player.CurrentHealth.Value <= 0) continue; // Skip dead players
                
                var distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = player;
                }
            }

            TargetPlayer = closestPlayer;
        }

        #region Movement Methods

        /// <summary>
        /// Flies towards a target position using physics.
        /// Returns true if close enough to target.
        /// </summary>
        public bool FlyTowards(Vector3 targetPosition)
        {
            if (!IsServer || m_isDead) return false;

            var direction = targetPosition - transform.position;
            var distance = direction.magnitude;
            
            // Check if we've reached the target
            if (distance <= 0.5f) return true;

            // Calculate target velocity
            var targetVelocity = direction.normalized * m_chaseSpeed;
            var velocityChange = targetVelocity - m_rigidbody.linearVelocity;
            
            // Limit acceleration
            var acceleration = velocityChange / Time.fixedDeltaTime;
            if (acceleration.magnitude > m_maxAcceleration)
            {
                acceleration = acceleration.normalized * m_maxAcceleration;
            }
            
            // Account for gravity
            acceleration -= Physics.gravity;
            
            m_rigidbody.AddForce(acceleration, ForceMode.Acceleration);
            return false;
        }

        /// <summary>
        /// Hovers around a target position with organic noise.
        /// </summary>
        public void HoverAround(Vector3 centerPosition)
        {
            if (!IsServer || m_isDead) return;

            // Add noise for organic movement
            var noise = Vector3.zero;
            if (m_hoverRadius > 0f)
            {
                var time = (float)((Time.timeAsDouble + m_hoverNoiseOffset) % 3600.0);
                noise = m_hoverRadius * new Vector3(
                    GeneratePerlinNoise(time * m_hoverNoiseFrequency.x, 0f),
                    GeneratePerlinNoise(time * m_hoverNoiseFrequency.y, 10f),
                    GeneratePerlinNoise(time * m_hoverNoiseFrequency.z, 20f)
                );
            }

            FlyTowards(centerPosition + noise);
        }

        private float GeneratePerlinNoise(float x, float y)
        {
            return 2f * Mathf.Clamp01(Mathf.PerlinNoise(x + m_hoverNoiseOffset, y + m_hoverNoiseOffset)) - 1f;
        }

        #endregion

        #region IDamageable Implementation

        public void TakeDamage(float damage, Vector3 position, Vector3 normal, IDamageable.DamageCallback callback = null)
        {
            if (m_isDead) return;
            
            // Only server processes damage
            if (IsServer)
            {
                ProcessDamage(damage, position, normal);
            }
            else
            {
                // Request server to process damage
                ProcessDamageServerRpc(damage, position, normal);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void ProcessDamageServerRpc(float damage, Vector3 position, Vector3 normal)
        {
            ProcessDamage(damage, position, normal);
        }

        private void ProcessDamage(float damage, Vector3 position, Vector3 normal)
        {
            if (!IsServer || m_isDead) return;

            var oldHealth = Health.Value;
            Health.Value = Mathf.Max(0f, Health.Value - damage);
            
            DebugLogger.Shooting($"Drone took damage | damage={damage:F1} | health={Health.Value:F1}/{m_maxHealth}", this);
            
            // Trigger damage visual effects on all clients
            PlayDamageEffectsClientRpc(damage, position, normal);
            
            // Fire events
            OnDroneDamaged?.Invoke(this, damage);
            
            // Check for death
            if (Health.Value <= 0f && oldHealth > 0f)
            {
                Die();
            }
        }

        [ClientRpc]
        private void PlayDamageEffectsClientRpc(float damage, Vector3 position, Vector3 normal)
        {
            // TODO: Play damage particle effects, sounds, etc.
            DebugLogger.Shooting($"Playing damage effects | damage={damage:F1}", this);
        }

        public void Heal(float healing, IDamageable.DamageCallback callback = null)
        {
            if (m_isDead || !IsServer) return;
            
            var oldHealth = Health.Value;
            Health.Value = Mathf.Min(m_maxHealth, Health.Value + healing);
            
            DebugLogger.Shooting($"Drone healed | healing={healing:F1} | health={Health.Value:F1}/{m_maxHealth}", this);
        }

        #endregion

        #region Death Handling

        private void Die()
        {
            if (m_isDead) return;
            
            m_isDead = true;
            DebugLogger.Shooting($"Drone died | Health={Health.Value}", this);
            
            // Trigger death effects on all clients
            PlayDeathEffectsClientRpc();
            
            // Fire events
            OnDroneDestroyed?.Invoke(this);
            
            // Despawn after a short delay to show death effects
            StartCoroutine(DespawnAfterDelay());
        }

        [ClientRpc]
        private void PlayDeathEffectsClientRpc()
        {
            DebugLogger.Shooting($"Playing death effects", this);
            
            // Spawn death effect if configured
            if (m_deathEffectPrefab != null)
            {
                Instantiate(m_deathEffectPrefab, transform.position, transform.rotation);
            }
            
            // TODO: Play death sound, particles, etc.
        }

        private IEnumerator DespawnAfterDelay()
        {
            // Stop AI and make kinematic
            if (m_ai) m_ai.enabled = false;
            m_rigidbody.isKinematic = true;
            
            yield return new WaitForSeconds(1f);
            
            if (NetworkObject != null && IsServer)
            {
                NetworkObject.Despawn();
            }
        }

        #endregion

        private void OnDestroy()
        {
            DebugLogger.Shooting($"Drone destroyed", this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Visualize hover radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, m_hoverRadius);
            
            // Visualize target if available
            if (TargetPlayer != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, TargetPlayer.transform.position);
            }
        }
#endif
    }
}
