// Copyright (c) Meta Platforms, Inc. and affiliates.
using UnityEngine;
using Meta.XR.Samples;

namespace MRMotifs.Shooting
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

        [Tooltip("Whether to automatically restart rounds.")]
        public bool autoRestart = true;

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

        [Header("=== DRONE SETTINGS ===")]
        
        [Tooltip("Health for each drone")]
        [Range(10, 200)]
        public int droneHealth = 50;
        
        [Tooltip("Number of drones per wave")]
        [Range(1, 15)]
        public int dronesPerWave = 5;
        
        [Tooltip("Maximum drones alive at once")]
        [Range(1, 8)]
        public int maxLiveDrones = 4;
        
        [Tooltip("Speed when drones chase players")]
        [Range(1f, 8f)]
        public float droneSpeed = 3f;
        
        [Tooltip("Time between drone spawns in seconds")]
        [Range(1f, 10f)]
        public float droneSpawnInterval = 3f;

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
                    droneHealth = 50;
                    dronesPerWave = 5;
                    maxLiveDrones = 4;
                    droneSpeed = 3f;
                    droneSpawnInterval = 3f;
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
                    droneHealth = 30;
                    dronesPerWave = 8;
                    maxLiveDrones = 6;
                    droneSpeed = 4f;
                    droneSpawnInterval = 2f;
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
                    droneHealth = 80;
                    dronesPerWave = 4;
                    maxLiveDrones = 3;
                    droneSpeed = 2.5f;
                    droneSpawnInterval = 4f;
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
                    droneHealth = 1;
                    dronesPerWave = 10;
                    maxLiveDrones = 8;
                    droneSpeed = 1.5f;
                    droneSpawnInterval = 1f;
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
                    droneHealth = 25;
                    dronesPerWave = 3;
                    maxLiveDrones = 2;
                    droneSpeed = 2f;
                    droneSpawnInterval = 5f;
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
            // Game Manager now reads config directly, no need to push values
            ApplyToPlayerHealth();
            ApplyToBullets();
            ApplyToShootingPlayer();
            ApplyToDrones();

            Debug.Log("[ShootingGameConfig] Configuration applied to all components");
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

        private void ApplyToDrones()
        {
            // This will be applied when drones spawn
            DroneMotif.ConfigHealth = droneHealth;
            DroneMotif.ConfigSpeed = droneSpeed;
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
