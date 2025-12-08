// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using UnityEngine;
using Meta.XR.Samples;

namespace MRMotifs.SharedActivities.ShootingSample
{
    /// <summary>
    /// Central configuration for the shooting game.
    /// Modify these values to tune gameplay without editing multiple scripts.
    /// Attach to the [MR Motif] Shooting Game Manager GameObject.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class ShootingGameConfigMotif : MonoBehaviour
    {
        [Header("=== GAME RULES ===")]
        
        [Tooltip("Number of kills needed to win a round.")]
        [Range(1, 50)]
        public int killsToWin = 10;

        [Tooltip("Duration of each round in seconds.")]
        [Range(30, 600)]
        public float roundDuration = 180f;

        [Tooltip("Minimum players required to start a match.")]
        [Range(1, 8)]
        public int minPlayersToStart = 2;

        [Tooltip("Countdown duration before round starts (seconds).")]
        [Range(1, 10)]
        public int countdownDuration = 5;

        [Tooltip("Time before automatically starting a new round after round end.")]
        [Range(5, 60)]
        public float autoRestartDelay = 10f;

        [Header("=== PLAYER STATS ===")]
        
        [Tooltip("Maximum health for each player.")]
        [Range(50, 500)]
        public int maxHealth = 100;

        [Tooltip("Time in seconds before respawning after death.")]
        [Range(1, 10)]
        public float respawnDelay = 3f;

        [Tooltip("Invulnerability duration after respawning.")]
        [Range(0, 5)]
        public float invulnerabilityDuration = 2f;

        [Header("=== WEAPON STATS ===")]
        
        [Tooltip("Damage dealt by each bullet.")]
        [Range(5, 100)]
        public int bulletDamage = 10;

        [Tooltip("Bullet travel speed.")]
        [Range(5, 200)]
        public float bulletSpeed = 60f;

        [Tooltip("Time between shots (fire rate).")]
        [Range(0.05f, 1f)]
        public float fireRate = 0.2f;

        [Tooltip("Bullet lifetime before despawning.")]
        [Range(1, 10)]
        public float bulletLifetime = 5f;

        [Header("=== PRESETS ===")]
        
        [Tooltip("Quick preset selection.")]
        public GamePreset preset = GamePreset.Standard;

        public enum GamePreset
        {
            Standard,       // Default balanced settings
            QuickMatch,     // Fast-paced, low health, high damage
            Marathon,       // Long rounds, high health
            OneShot,        // One hit kills
            Training        // Single player practice mode
        }

        private void Awake()
        {
            ApplyConfiguration();
        }

        private void OnValidate()
        {
            // Apply preset if changed in editor
            if (Application.isPlaying)
            {
                ApplyConfiguration();
            }
        }

        /// <summary>
        /// Apply a preset configuration.
        /// </summary>
        public void ApplyPreset(GamePreset presetType)
        {
            preset = presetType;

            switch (presetType)
            {
                case GamePreset.Standard:
                    killsToWin = 10;
                    roundDuration = 180f;
                    minPlayersToStart = 2;
                    maxHealth = 100;
                    respawnDelay = 3f;
                    bulletDamage = 10;
                    bulletSpeed = 60f;
                    fireRate = 0.1f;
                    break;

                case GamePreset.QuickMatch:
                    killsToWin = 5;
                    roundDuration = 60f;
                    minPlayersToStart = 2;
                    maxHealth = 50;
                    respawnDelay = 1f;
                    bulletDamage = 25;
                    bulletSpeed = 80f;
                    fireRate = 0.1f;
                    break;

                case GamePreset.Marathon:
                    killsToWin = 25;
                    roundDuration = 600f;
                    minPlayersToStart = 2;
                    maxHealth = 200;
                    respawnDelay = 5f;
                    bulletDamage = 10;
                    bulletSpeed = 60f;
                    fireRate = 0.25f;
                    break;

                case GamePreset.OneShot:
                    killsToWin = 10;
                    roundDuration = 120f;
                    minPlayersToStart = 2;
                    maxHealth = 1;
                    respawnDelay = 2f;
                    bulletDamage = 100;
                    bulletSpeed = 100f;
                    fireRate = 0.5f;
                    break;

                case GamePreset.Training:
                    killsToWin = 100;
                    roundDuration = 3600f; // 1 hour
                    minPlayersToStart = 1; // Single player
                    maxHealth = 100;
                    respawnDelay = 1f;
                    bulletDamage = 10;
                    bulletSpeed = 60f;
                    fireRate = 0.1f;
                    break;
            }

            ApplyConfiguration();
            Debug.Log($"[ShootingGameConfig] Applied preset: {presetType}");
        }

        /// <summary>
        /// Apply the current configuration to all game components.
        /// </summary>
        public void ApplyConfiguration()
        {
            ApplyToGameManager();
            ApplyToPlayerHealth();
            ApplyToBullets();
            ApplyToShootingPlayer();

            Debug.Log("[ShootingGameConfig] Configuration applied to all components");
        }

        private void ApplyToGameManager()
        {
            var gameManager = GetComponent<ShootingGameManagerMotif>();
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<ShootingGameManagerMotif>();
            }

            if (gameManager != null)
            {
                var type = typeof(ShootingGameManagerMotif);
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

                SetField(type, gameManager, "m_killsToWin", killsToWin, flags);
                SetField(type, gameManager, "m_roundDuration", roundDuration, flags);
                SetField(type, gameManager, "m_minPlayersToStart", minPlayersToStart, flags);
                SetField(type, gameManager, "m_autoRestartDelay", autoRestartDelay, flags);
            }
        }

        private void ApplyToPlayerHealth()
        {
            // This will be applied when players spawn via ShootingSetupMotif
            // Store as static config that PlayerHealthMotif can read
            PlayerHealthMotif.ConfigMaxHealth = maxHealth;
            PlayerHealthMotif.ConfigRespawnDelay = respawnDelay;
            PlayerHealthMotif.ConfigInvulnerabilityDuration = invulnerabilityDuration;
        }

        private void ApplyToBullets()
        {
            // This will be applied when bullets spawn
            BulletMotif.ConfigDamage = bulletDamage;
        }

        private void ApplyToShootingPlayer()
        {
            // Store config for ShootingPlayerMotif to read
            ShootingPlayerMotif.ConfigFireRate = fireRate;
            ShootingPlayerMotif.ConfigBulletSpeed = bulletSpeed;
            ShootingPlayerMotif.ConfigBulletLifetime = bulletLifetime;
        }

        private void SetField(System.Type type, object target, string fieldName, object value, System.Reflection.BindingFlags flags)
        {
            var field = type.GetField(fieldName, flags);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        /// <summary>
        /// Get a formatted summary of current configuration.
        /// </summary>
        public string GetConfigSummary()
        {
            return $"=== Game Config ({preset}) ===\n" +
                   $"Kills to Win: {killsToWin}\n" +
                   $"Round Duration: {roundDuration}s\n" +
                   $"Min Players: {minPlayersToStart}\n" +
                   $"---\n" +
                   $"Max Health: {maxHealth}\n" +
                   $"Respawn Delay: {respawnDelay}s\n" +
                   $"---\n" +
                   $"Bullet Damage: {bulletDamage}\n" +
                   $"Fire Rate: {fireRate}s\n" +
                   $"Bullet Speed: {bulletSpeed}";
        }
    }
}
#endif
