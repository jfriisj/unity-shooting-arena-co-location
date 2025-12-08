// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Netcode;
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
        public NetworkVariable<int> Kills = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Number of times this player has died.
        /// </summary>
        public NetworkVariable<int> Deaths = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Total number of shots fired by this player.
        /// </summary>
        public NetworkVariable<int> ShotsFired = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Number of shots that hit a target.
        /// </summary>
        public NetworkVariable<int> ShotsHit = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Total damage dealt by this player.
        /// </summary>
        public NetworkVariable<float> DamageDealt = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Total damage taken by this player.
        /// </summary>
        public NetworkVariable<float> DamageTaken = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Total healing received by this player.
        /// </summary>
        public NetworkVariable<float> HealingReceived = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Time survived in seconds.
        /// </summary>
        public NetworkVariable<float> TimeSurvived = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Number of drones killed by this player.
        /// </summary>
        public NetworkVariable<int> DroneKills = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Calculates shooting accuracy as a percentage (0-1).
        /// </summary>
        public float Accuracy => ShotsFired.Value > 0 ? (float)ShotsHit.Value / ShotsFired.Value : 0f;

        /// <summary>
        /// Calculates K/D ratio.
        /// </summary>
        public float KDRatio => Deaths.Value > 0 ? (float)Kills.Value / Deaths.Value : Kills.Value;

        /// <summary>
        /// Increments kill count.
        /// </summary>
        public void AddKill()
        {
            if (IsOwner)
            {
                Kills.Value++;
                DebugLogger.Player($"Kill recorded | total={Kills.Value}", this);
            }
        }

        /// <summary>
        /// Increments death count.
        /// </summary>
        public void AddDeath()
        {
            if (IsOwner)
            {
                Deaths.Value++;
                DebugLogger.Player($"Death recorded | total={Deaths.Value}", this);
            }
        }

        /// <summary>
        /// Increments drone kill count.
        /// </summary>
        public void AddDroneKill()
        {
            if (IsOwner)
            {
                DroneKills.Value++;
                DebugLogger.Player($"Drone kill recorded | total={DroneKills.Value}", this);
            }
        }

        /// <summary>
        /// Records a shot fired.
        /// </summary>
        public void RecordShotFired()
        {
            if (IsOwner)
            {
                ShotsFired.Value++;
            }
        }

        /// <summary>
        /// Records a shot that hit a target.
        /// </summary>
        /// <param name="damage">Damage dealt by the shot.</param>
        public void RecordShotHit(float damage)
        {
            if (IsOwner)
            {
                ShotsHit.Value++;
                DamageDealt.Value += damage;
                DebugLogger.Player($"Hit recorded | accuracy={Accuracy:P0} totalDamage={DamageDealt.Value:F0}", this);
            }
        }

        /// <summary>
        /// Records damage taken.
        /// </summary>
        /// <param name="damage">Amount of damage taken.</param>
        public void RecordDamageTaken(float damage)
        {
            if (IsOwner)
            {
                DamageTaken.Value += damage;
            }
        }

        /// <summary>
        /// Records healing received.
        /// </summary>
        /// <param name="healing">Amount of healing received.</param>
        public void RecordHealing(float healing)
        {
            if (IsOwner)
            {
                HealingReceived.Value += healing;
            }
        }

        /// <summary>
        /// Resets all stats to zero.
        /// </summary>
        public void ResetStats()
        {
            if (IsOwner)
            {
                Kills.Value = 0;
                Deaths.Value = 0;
                ShotsFired.Value = 0;
                ShotsHit.Value = 0;
                DamageDealt.Value = 0f;
                DamageTaken.Value = 0f;
                HealingReceived.Value = 0f;
                TimeSurvived.Value = 0f;
                
                DebugLogger.Player("Stats reset", this);
            }
        }

        /// <summary>
        /// Gets formatted stats string for display.
        /// </summary>
        public string GetStatsString()
        {
            return $"K/D: {Kills.Value}/{Deaths.Value} ({KDRatio:F2})\n" +
                   $"Accuracy: {Accuracy:P0}\n" +
                   $"Damage: {DamageDealt.Value:F0} / {DamageTaken.Value:F0}\n" +
                   $"Time: {TimeSurvived.Value:F0}s";
        }

        private void FixedUpdate()
        {
            // Ensure network object is valid before accessing
            if (!IsSpawned) return;

            // Track survival time (only when alive and game is running)
            if (IsOwner)
            {
                var health = GetComponent<PlayerHealthMotif>();
                if (health != null && !health.IsDeadLocal)
                {
                    TimeSurvived.Value += Time.fixedDeltaTime;
                }
            }
        }
    }
}
