// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using Meta.XR.Samples;
using MRMotifs.Shared;

namespace MRMotifs.Shooting
{
    /// <summary>
    /// Tracks player statistics in the shooting game.
    /// Separated from PlayerHealthMotif following single responsibility principle.
    /// Based on Discover/DroneRage pattern for networked stats tracking.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class PlayerStatsMotif : NetworkBehaviour
    {
        /// <summary>
        /// Number of kills this player has scored.
        /// </summary>
        [Networked]
        public int Kills { get; set; }

        /// <summary>
        /// Number of times this player has died.
        /// </summary>
        [Networked]
        public int Deaths { get; set; }

        /// <summary>
        /// Total number of shots fired by this player.
        /// </summary>
        [Networked]
        public int ShotsFired { get; set; }

        /// <summary>
        /// Number of shots that hit a target.
        /// </summary>
        [Networked]
        public int ShotsHit { get; set; }

        /// <summary>
        /// Total damage dealt by this player.
        /// </summary>
        [Networked]
        public float DamageDealt { get; set; }

        /// <summary>
        /// Total damage taken by this player.
        /// </summary>
        [Networked]
        public float DamageTaken { get; set; }

        /// <summary>
        /// Total healing received by this player.
        /// </summary>
        [Networked]
        public float HealingReceived { get; set; }

        /// <summary>
        /// Time survived in seconds.
        /// </summary>
        [Networked]
        public float TimeSurvived { get; set; }

        /// <summary>
        /// Number of drones killed by this player.
        /// </summary>
        [Networked]
        public int DroneKills { get; set; }

        /// <summary>
        /// Calculates shooting accuracy as a percentage (0-1).
        /// </summary>
        public float Accuracy => ShotsFired > 0 ? (float)ShotsHit / ShotsFired : 0f;

        /// <summary>
        /// Calculates K/D ratio.
        /// </summary>
        public float KDRatio => Deaths > 0 ? (float)Kills / Deaths : Kills;

        /// <summary>
        /// Increments kill count.
        /// </summary>
        public void AddKill()
        {
            if (Object.HasStateAuthority)
            {
                Kills++;
                DebugLogger.Player($"Kill recorded | total={Kills}", this);
            }
        }

        /// <summary>
        /// Increments death count.
        /// </summary>
        public void AddDeath()
        {
            if (Object.HasStateAuthority)
            {
                Deaths++;
                DebugLogger.Player($"Death recorded | total={Deaths}", this);
            }
        }

        /// <summary>
        /// Increments drone kill count.
        /// </summary>
        public void AddDroneKill()
        {
            if (Object.HasStateAuthority)
            {
                DroneKills++;
                DebugLogger.Player($"Drone kill recorded | total={DroneKills}", this);
            }
        }

        /// <summary>
        /// Records a shot fired.
        /// </summary>
        public void RecordShotFired()
        {
            if (Object.HasStateAuthority)
            {
                ShotsFired++;
            }
        }

        /// <summary>
        /// Records a shot that hit a target.
        /// </summary>
        /// <param name="damage">Damage dealt by the shot.</param>
        public void RecordShotHit(float damage)
        {
            if (Object.HasStateAuthority)
            {
                ShotsHit++;
                DamageDealt += damage;
                DebugLogger.Player($"Hit recorded | accuracy={Accuracy:P0} totalDamage={DamageDealt:F0}", this);
            }
        }

        /// <summary>
        /// Records damage taken.
        /// </summary>
        /// <param name="damage">Amount of damage taken.</param>
        public void RecordDamageTaken(float damage)
        {
            if (Object.HasStateAuthority)
            {
                DamageTaken += damage;
            }
        }

        /// <summary>
        /// Records healing received.
        /// </summary>
        /// <param name="healing">Amount of healing received.</param>
        public void RecordHealing(float healing)
        {
            if (Object.HasStateAuthority)
            {
                HealingReceived += healing;
            }
        }

        /// <summary>
        /// Resets all stats to zero.
        /// </summary>
        public void ResetStats()
        {
            if (Object.HasStateAuthority)
            {
                Kills = 0;
                Deaths = 0;
                ShotsFired = 0;
                ShotsHit = 0;
                DamageDealt = 0f;
                DamageTaken = 0f;
                HealingReceived = 0f;
                TimeSurvived = 0f;
                
                DebugLogger.Player("Stats reset", this);
            }
        }

        /// <summary>
        /// Gets formatted stats string for display.
        /// </summary>
        public string GetStatsString()
        {
            return $"K/D: {Kills}/{Deaths} ({KDRatio:F2})\n" +
                   $"Accuracy: {Accuracy:P0}\n" +
                   $"Damage: {DamageDealt:F0} / {DamageTaken:F0}\n" +
                   $"Time: {TimeSurvived:F0}s";
        }

        private void FixedUpdate()
        {
            // Ensure network object is valid before accessing Object property
            if (Object == null || !Object.IsValid) return;

            // Track survival time (only when alive and game is running)
            if (Object.HasStateAuthority)
            {
                var health = GetComponent<PlayerHealthMotif>();
                if (health != null && !health.IsDeadLocal)
                {
                    TimeSurvived += Time.fixedDeltaTime;
                }
            }
        }
    }
}
#endif
