// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using System;
using System.Collections;
using Meta.XR.Samples;
using MRMotifs.Shared;

namespace MRMotifs.SharedActivities.ShootingSample
{
    /// <summary>
    /// Manages player health, death, and respawn in the shooting game.
    /// Synchronizes health state across networked clients and handles scoring.
    /// Implements IDamageable interface for uniform damage handling.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class PlayerHealthMotif : NetworkBehaviour, IDamageable
    {
        // Static configuration from ShootingGameConfigMotif
        public static int ConfigMaxHealth = 100;
        public static float ConfigRespawnDelay = 3f;
        public static float ConfigInvulnerabilityDuration = 2f;

        [Header("Health Settings")]
        [Tooltip("Maximum health points.")]
        [SerializeField] private int m_maxHealth = 100;

        [Tooltip("Time in seconds before respawning after death.")]
        [SerializeField] private float m_respawnDelay = 3f;

        [Tooltip("Whether the player is invulnerable after respawning.")]
        [SerializeField] private float m_invulnerabilityDuration = 2f;

        [Header("Visual Feedback")]
        [Tooltip("Renderer to flash when taking damage.")]
        [SerializeField] private Renderer m_playerRenderer;

        [Tooltip("Color to flash when hit.")]
        [SerializeField] private Color m_hitFlashColor = Color.red;

        [Tooltip("Duration of hit flash effect (seconds).")]
        [Range(0.05f, 1f)]
        [SerializeField] private float m_hitFlashDuration = 0.1f;

        [Header("Audio")]
        [Tooltip("Sound played when taking damage.")]
        [SerializeField] private AudioClip m_hitSound;

        [Tooltip("Sound played when dying.")]
        [SerializeField] private AudioClip m_deathSound;

        [Tooltip("Sound played when respawning.")]
        [SerializeField] private AudioClip m_respawnSound;

        /// <summary>
        /// Current health, synchronized across all clients.
        /// </summary>
        [Networked, OnChangedRender(nameof(OnHealthChanged))]
        public int CurrentHealth { get; set; }

        /// <summary>
        /// Whether the player is currently dead.
        /// </summary>
        [Networked, OnChangedRender(nameof(OnDeadStateChanged))]
        public NetworkBool IsDead { get; set; }

        /// <summary>
        /// Whether the player is currently invulnerable (after respawn).
        /// </summary>
        [Networked]
        public NetworkBool IsInvulnerable { get; set; }

        /// <summary>
        /// The player who owns this health component.
        /// </summary>
        [Networked]
        public PlayerRef OwnerPlayer { get; set; }

        /// <summary>
        /// Reference to player statistics component.
        /// </summary>
        public PlayerStatsMotif PlayerStats { get; private set; }

        /// <summary>
        /// Number of kills this player has (for backwards compatibility).
        /// Use PlayerStats.Kills instead.
        /// </summary>
        [Networked, OnChangedRender(nameof(OnScoreChanged))]
        public int Kills 
        { 
            get => PlayerStats != null ? PlayerStats.Kills : 0;
            set { if (PlayerStats != null) PlayerStats.Kills = value; }
        }

        /// <summary>
        /// Number of deaths this player has (for backwards compatibility).
        /// Use PlayerStats.Deaths instead.
        /// </summary>
        [Networked, OnChangedRender(nameof(OnScoreChanged))]
        public int Deaths 
        { 
            get => PlayerStats != null ? PlayerStats.Deaths : 0;
            set { if (PlayerStats != null) PlayerStats.Deaths = value; }
        }

        /// <summary>
        /// Event fired when health changes. Parameters: currentHealth, maxHealth
        /// </summary>
        public event Action<int, int> OnHealthUpdated;

        /// <summary>
        /// Event fired when the player dies. Parameter: killer PlayerRef
        /// </summary>
        public event Action<PlayerRef> OnPlayerDied;

        /// <summary>
        /// Event fired when the player respawns.
        /// </summary>
        public event Action OnPlayerRespawned;

        /// <summary>
        /// Event fired when score changes. Parameters: kills, deaths
        /// </summary>
        public event Action<int, int> OnScoreUpdated;

        private Color m_originalColor;
        private AudioSource m_audioSource;
        private Transform m_spawnPoint;

        // Health state - uses local tracking because this component is added via AddComponent
        // at runtime, so Fusion's [Networked] properties don't work. This is intentional.
        private int m_currentHealth;
        private bool m_isDead;
        private bool m_isInvulnerable;
        private NetworkObject m_networkObject;

        /// <summary>
        /// Get current health.
        /// </summary>
        public int GetCurrentHealth() => m_currentHealth;
        
        /// <summary>
        /// Check if player is dead.
        /// </summary>
        public bool IsDeadLocal => m_isDead;

        private void Awake()
        {
            // Initialize health state
            m_currentHealth = m_maxHealth;
            m_isDead = false;
            m_isInvulnerable = false;

            // Get or add PlayerStats component
            PlayerStats = GetComponent<PlayerStatsMotif>();
            if (PlayerStats == null)
            {
                PlayerStats = gameObject.AddComponent<PlayerStatsMotif>();
                DebugLogger.Health("PlayerStats component added", this);
            }

            // Get NetworkObject for owner detection
            m_networkObject = GetComponentInParent<NetworkObject>();
            
            DebugLogger.Health($"Awake | maxHealth={m_maxHealth} networkObject={(m_networkObject != null ? "found" : "NOT FOUND")}", this);
        }

        public override void Spawned()
        {
            base.Spawned();

            // Apply static config if set
            if (ConfigMaxHealth > 0) m_maxHealth = ConfigMaxHealth;
            if (ConfigRespawnDelay > 0) m_respawnDelay = ConfigRespawnDelay;
            if (ConfigInvulnerabilityDuration >= 0) m_invulnerabilityDuration = ConfigInvulnerabilityDuration;

            // Initialize local health state with config
            m_currentHealth = m_maxHealth;
            m_isDead = false;
            m_isInvulnerable = false;

            if (Object.HasStateAuthority)
            {
                OwnerPlayer = Runner.LocalPlayer;
                CurrentHealth = m_maxHealth;
                IsDead = false;
                IsInvulnerable = false;
                Kills = 0;
                Deaths = 0;
            }

            // Fire initial health event
            OnHealthUpdated?.Invoke(m_currentHealth, m_maxHealth);

            DebugLogger.Health($"Spawned | maxHealth={m_maxHealth} currentHealth={m_currentHealth} hasAuthority={Object.HasStateAuthority}", this);

            // Cache original color for flash effect
            DebugLogger.RequireSerializedField(m_playerRenderer, "m_playerRenderer", this);
            m_originalColor = m_playerRenderer.material.color;

            // Get or add audio source
            m_audioSource = GetComponent<AudioSource>();
            if (m_audioSource == null)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Require audio clips - must be assigned in inspector
            DebugLogger.RequireSerializedField(m_hitSound, "m_hitSound", this);
            DebugLogger.RequireSerializedField(m_deathSound, "m_deathSound", this);
            DebugLogger.RequireSerializedField(m_respawnSound, "m_respawnSound", this);
        }

        /// <summary>
        /// Apply damage locally without using Fusion RPCs.
        /// This is used when PlayerHealthMotif is added dynamically and RPCs don't work.
        /// </summary>
        public void ApplyDamageLocal(int damage, PlayerRef attacker)
        {
            DebugLogger.Health($"Damage | amount={damage} attacker={attacker} health={m_currentHealth}/{m_maxHealth}", this);
            
            if (m_isDead || m_isInvulnerable)
            {
                DebugLogger.Health($"Damage ignored | dead={m_isDead} invulnerable={m_isInvulnerable}", this);
                return;
            }

            m_currentHealth = Mathf.Max(0, m_currentHealth - damage);
            
            // Fire health updated event
            OnHealthUpdated?.Invoke(m_currentHealth, m_maxHealth);

            // Play hit effects locally
            PlayHitEffects();

            if (m_currentHealth <= 0)
            {
                DieLocal(attacker);
            }
        }

        #region IDamageable Implementation

        /// <summary>
        /// IDamageable interface implementation - applies damage to this player.
        /// </summary>
        public void TakeDamage(float damage, Vector3 position, Vector3 normal, IDamageable.DamageCallback callback = null)
        {
            ApplyDamageLocal((int)damage, PlayerRef.None);
            
            // Record damage taken
            if (PlayerStats != null)
            {
                PlayerStats.RecordDamageTaken(damage);
            }
            
            // Invoke callback if provided
            callback?.Invoke(this, damage, m_isDead);
        }

        /// <summary>
        /// IDamageable interface implementation - heals this player.
        /// </summary>
        public void Heal(float healing, IDamageable.DamageCallback callback = null)
        {
            if (m_isDead) return;
            
            m_currentHealth = Mathf.Min(m_maxHealth, m_currentHealth + (int)healing);
            OnHealthUpdated?.Invoke(m_currentHealth, m_maxHealth);
            
            // Record healing
            if (PlayerStats != null)
            {
                PlayerStats.RecordHealing(healing);
            }
            
            DebugLogger.Health($"Healed | amount={healing} health={m_currentHealth}/{m_maxHealth}", this);
            
            // Invoke callback if provided
            callback?.Invoke(this, healing, false);
        }

        #endregion

        private void PlayHitEffects()
        {
            // Play hit sound
            if (m_audioSource != null && m_hitSound != null)
            {
                m_audioSource.PlayOneShot(m_hitSound);
            }

            // Flash the player model
            if (m_playerRenderer != null)
            {
                StartCoroutine(FlashColor());
            }
        }

        private void DieLocal(PlayerRef killer)
        {
            DebugLogger.Health($"DEATH | killer={killer} deaths={PlayerStats.Deaths}", this);
            m_isDead = true;
            PlayerStats.AddDeath();

            // Play death sound
            if (m_audioSource != null && m_deathSound != null)
            {
                m_audioSource.PlayOneShot(m_deathSound);
            }

            // Fire death event
            OnPlayerDied?.Invoke(killer);
            OnScoreUpdated?.Invoke(PlayerStats.Kills, PlayerStats.Deaths);

            // Start respawn timer
            StartCoroutine(RespawnAfterDelayLocal());
        }

        private IEnumerator RespawnAfterDelayLocal()
        {
            yield return new WaitForSeconds(m_respawnDelay);

            m_currentHealth = m_maxHealth;
            m_isDead = false;
            m_isInvulnerable = true;

            // Fire respawn event
            OnPlayerRespawned?.Invoke();
            OnHealthUpdated?.Invoke(m_currentHealth, m_maxHealth);

            // Invulnerability period
            yield return new WaitForSeconds(m_invulnerabilityDuration);
            m_isInvulnerable = false;
        }

        /// <summary>
        /// RPC to apply damage to this player. Called by BulletMotif on hit.
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void TakeDamageRpc(int damage, PlayerRef attacker)
        {
            if (IsDead || IsInvulnerable)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

            // Notify all clients about the hit
            OnHitRpc();

            if (CurrentHealth <= 0)
            {
                Die(attacker);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void OnHitRpc()
        {
            // Play hit sound
            if (m_audioSource != null && m_hitSound != null)
            {
                m_audioSource.PlayOneShot(m_hitSound);
            }

            // Flash the player model
            if (m_playerRenderer != null)
            {
                _ = StartCoroutine(FlashColor());
            }
        }

        private IEnumerator FlashColor()
        {
            if (m_playerRenderer != null)
            {
                m_playerRenderer.material.color = m_hitFlashColor;
                yield return new WaitForSeconds(m_hitFlashDuration);
                m_playerRenderer.material.color = m_originalColor;
            }
        }

        private void Die(PlayerRef killer)
        {
            IsDead = true;
            Deaths++;

            // Award kill to attacker
            if (killer != PlayerRef.None)
            {
                AwardKillToPlayer(killer);
            }

            // Broadcast death
            OnDeathRpc(killer);

            // Start respawn timer
            _ = StartCoroutine(RespawnAfterDelay());
        }

        private void AwardKillToPlayer(PlayerRef killer)
        {
            // Find the killer's PlayerHealthMotif and request kill increment on their state authority
            var players = FindObjectsByType<PlayerHealthMotif>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player.OwnerPlayer == killer)
                {
                    player.IncrementKillsRpc();
                    break;
                }
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void IncrementKillsRpc()
        {
            // Only state authority modifies the networked Kills property
            Kills++;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void OnDeathRpc(PlayerRef killer)
        {
            // Play death sound
            if (m_audioSource != null && m_deathSound != null)
            {
                m_audioSource.PlayOneShot(m_deathSound);
            }

            OnPlayerDied?.Invoke(killer);

            // Disable player visuals/collider during death
            SetPlayerActive(false);
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(m_respawnDelay);

            if (Object.HasStateAuthority)
            {
                Respawn();
            }
        }

        private void Respawn()
        {
            CurrentHealth = m_maxHealth;
            IsDead = false;
            IsInvulnerable = true;

            // Move to spawn point if available
            if (m_spawnPoint != null)
            {
                var cameraRig = FindAnyObjectByType<OVRCameraRig>();
                if (cameraRig != null)
                {
                    cameraRig.transform.position = m_spawnPoint.position;
                    cameraRig.transform.rotation = m_spawnPoint.rotation;
                }
            }

            // Notify all clients about respawn
            OnRespawnRpc();

            // Start invulnerability timer
            _ = StartCoroutine(EndInvulnerabilityAfterDelay());
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void OnRespawnRpc()
        {
            // Play respawn sound
            if (m_audioSource != null && m_respawnSound != null)
            {
                m_audioSource.PlayOneShot(m_respawnSound);
            }

            SetPlayerActive(true);
            OnPlayerRespawned?.Invoke();
        }

        private IEnumerator EndInvulnerabilityAfterDelay()
        {
            yield return new WaitForSeconds(m_invulnerabilityDuration);

            if (Object.HasStateAuthority)
            {
                IsInvulnerable = false;
            }
        }

        private void SetPlayerActive(bool active)
        {
            // Enable/disable colliders and visuals during death/respawn
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = active;
            }

            if (m_playerRenderer != null)
            {
                m_playerRenderer.enabled = active;
            }
        }

        /// <summary>
        /// Sets the spawn point for respawning.
        /// </summary>
        public void SetSpawnPoint(Transform spawnPoint)
        {
            m_spawnPoint = spawnPoint;
        }

        /// <summary>
        /// Resets the player's stats (for new round).
        /// </summary>
        public void ResetStats()
        {
            if (Object == null || !Object.IsValid) return;

            if (Object.HasStateAuthority)
            {
                Kills = 0;
                Deaths = 0;
                CurrentHealth = m_maxHealth;
                IsDead = false;
                IsInvulnerable = false;
            }
        }

        // Change callbacks for UI updates
        private void OnHealthChanged()
        {
            OnHealthUpdated?.Invoke(CurrentHealth, m_maxHealth);
        }

        private void OnDeadStateChanged()
        {
            // Additional visual effects can be added here
        }

        private void OnScoreChanged()
        {
            OnScoreUpdated?.Invoke(Kills, Deaths);
        }
    }
}
#endif
