// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using System;
using System.Collections;
using Meta.XR.Samples;

namespace MRMotifs.SharedActivities.ShootingSample
{
    /// <summary>
    /// Manages player health, death, and respawn in the shooting game.
    /// Synchronizes health state across networked clients and handles scoring.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class PlayerHealthMotif : NetworkBehaviour
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
        /// Number of kills this player has.
        /// </summary>
        [Networked, OnChangedRender(nameof(OnScoreChanged))]
        public int Kills { get; set; }

        /// <summary>
        /// Number of deaths this player has.
        /// </summary>
        [Networked, OnChangedRender(nameof(OnScoreChanged))]
        public int Deaths { get; set; }

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

        // Local state for when Networked properties don't work (dynamic AddComponent)
        private int m_currentHealthLocal;
        private bool m_isDeadLocal;
        private bool m_isInvulnerableLocal;
        private int m_killsLocal;
        private int m_deathsLocal;
        private NetworkObject m_networkObject;

        /// <summary>
        /// Get current health (local fallback if networked doesn't work)
        /// </summary>
        public int GetCurrentHealth() => m_currentHealthLocal;
        
        /// <summary>
        /// Check if player is dead (local fallback)
        /// </summary>
        public bool IsDeadLocal => m_isDeadLocal;

        private void Awake()
        {
            // Initialize local health state
            m_currentHealthLocal = m_maxHealth;
            m_isDeadLocal = false;
            m_isInvulnerableLocal = false;
            m_killsLocal = 0;
            m_deathsLocal = 0;

            // Try to get NetworkObject for owner detection
            m_networkObject = GetComponentInParent<NetworkObject>();
        }

        public override void Spawned()
        {
            base.Spawned();

            // Apply static config if set
            if (ConfigMaxHealth > 0) m_maxHealth = ConfigMaxHealth;
            if (ConfigRespawnDelay > 0) m_respawnDelay = ConfigRespawnDelay;
            if (ConfigInvulnerabilityDuration >= 0) m_invulnerabilityDuration = ConfigInvulnerabilityDuration;

            // Initialize local health state with config
            m_currentHealthLocal = m_maxHealth;
            m_isDeadLocal = false;
            m_isInvulnerableLocal = false;

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
            OnHealthUpdated?.Invoke(m_currentHealthLocal, m_maxHealth);

            Debug.Log($"[PlayerHealthMotif] Spawned - maxHealth: {m_maxHealth}, currentHealth: {m_currentHealthLocal}");

            // Cache original color for flash effect
            if (m_playerRenderer != null)
            {
                m_originalColor = m_playerRenderer.material.color;
            }

            // Get or add audio source
            m_audioSource = GetComponent<AudioSource>();
            if (m_audioSource == null)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Load audio clips from Resources if not assigned
            if (m_hitSound == null)
            {
                m_hitSound = Resources.Load<AudioClip>("Audio/Hit");
            }
            if (m_deathSound == null)
            {
                m_deathSound = Resources.Load<AudioClip>("Audio/Death");
            }
            if (m_respawnSound == null)
            {
                m_respawnSound = Resources.Load<AudioClip>("Audio/Respawn");
            }
        }

        /// <summary>
        /// Apply damage locally without using Fusion RPCs.
        /// This is used when PlayerHealthMotif is added dynamically and RPCs don't work.
        /// </summary>
        public void ApplyDamageLocal(int damage, PlayerRef attacker)
        {
            Debug.Log($"[PlayerHealthMotif] ApplyDamageLocal - damage: {damage}, attacker: {attacker}, currentHealth: {m_currentHealthLocal}");
            
            if (m_isDeadLocal || m_isInvulnerableLocal)
            {
                Debug.Log($"[PlayerHealthMotif] Ignoring damage - dead: {m_isDeadLocal}, invulnerable: {m_isInvulnerableLocal}");
                return;
            }

            m_currentHealthLocal = Mathf.Max(0, m_currentHealthLocal - damage);
            
            // Fire health updated event
            OnHealthUpdated?.Invoke(m_currentHealthLocal, m_maxHealth);

            // Play hit effects locally
            PlayHitEffects();

            if (m_currentHealthLocal <= 0)
            {
                DieLocal(attacker);
            }
        }

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
            Debug.Log($"[PlayerHealthMotif] DieLocal - killed by: {killer}");
            m_isDeadLocal = true;
            m_deathsLocal++;

            // Play death sound
            if (m_audioSource != null && m_deathSound != null)
            {
                m_audioSource.PlayOneShot(m_deathSound);
            }

            // Fire death event
            OnPlayerDied?.Invoke(killer);
            OnScoreUpdated?.Invoke(m_killsLocal, m_deathsLocal);

            // Start respawn timer
            StartCoroutine(RespawnAfterDelayLocal());
        }

        private IEnumerator RespawnAfterDelayLocal()
        {
            yield return new WaitForSeconds(m_respawnDelay);

            m_currentHealthLocal = m_maxHealth;
            m_isDeadLocal = false;
            m_isInvulnerableLocal = true;

            // Fire respawn event
            OnPlayerRespawned?.Invoke();
            OnHealthUpdated?.Invoke(m_currentHealthLocal, m_maxHealth);

            // Invulnerability period
            yield return new WaitForSeconds(m_invulnerabilityDuration);
            m_isInvulnerableLocal = false;
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
                yield return new WaitForSeconds(0.1f);
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
