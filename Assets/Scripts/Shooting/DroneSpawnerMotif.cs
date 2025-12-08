// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Meta.XR.Samples;
using Meta.XR.MRUtilityKit;
using MRMotifs.Shared;

namespace MRMotifs.Shooting
{
    /// <summary>
    /// Spawns drone enemies in waves for the shooting game.
    /// Only runs on server for authoritative spawning.
    /// Based on Discover/DroneRage Spawner.cs but simplified.
    /// Converted from Photon Fusion to Unity NGO.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class DroneSpawnerMotif : NetworkBehaviour
    {
        [Header("=== SPAWNING CONFIG ===")]
        
        [SerializeField, Tooltip("Drone prefab to spawn (must have NetworkObject)")]
        private NetworkObject m_dronePrefab;
        
        [SerializeField, Tooltip("Initial number of drones per wave")]
        [Range(1, 10)]
        private int m_baseWaveDroneCount = 3;
        
        [SerializeField, Tooltip("How many more drones each wave")]
        [Range(0, 5)]
        private int m_droneCountIncrease = 1;
        
        [SerializeField, Tooltip("Maximum drones alive at once")]
        [Range(1, 8)]
        private int m_maxLiveDrones = 4;
        
        [SerializeField, Tooltip("Time between spawns in seconds")]
        [Range(1f, 10f)]
        private float m_spawnInterval = 3f;
        
        [SerializeField, Tooltip("Time before starting first wave")]
        [Range(1f, 10f)]
        private float m_firstWaveDelay = 5f;
        
        [SerializeField, Tooltip("Height offset above room for spawn points")]
        [Range(0.5f, 3f)]
        private float m_spawnHeightOffset = 1.5f;
        
        [SerializeField, Tooltip("Distance from room center for spawn points")]
        [Range(2f, 10f)]
        private float m_spawnDistance = 5f;

        /// <summary>
        /// Current wave number (starts at 1).
        /// </summary>
        public NetworkVariable<int> CurrentWave = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        
        /// <summary>
        /// Number of drones spawned this wave.
        /// </summary>
        public NetworkVariable<int> DronesSpawnedThisWave = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        
        /// <summary>
        /// Number of drones currently alive.
        /// </summary>
        public NetworkVariable<int> LiveDroneCount = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        /// <summary>
        /// Event fired when a new wave starts.
        /// </summary>
        public static event Action<int> OnWaveStarted;
        
        /// <summary>
        /// Event fired when a wave is completed.
        /// </summary>
        public static event Action<int> OnWaveCompleted;

        private ShootingGameConfigMotif m_config;
        private Vector3 m_roomCenter = Vector3.zero;
#if UNITY_EDITOR
        private Vector3 m_roomMinExtent = Vector3.zero;
#endif
        private Vector3 m_roomMaxExtent = new Vector3(3f, 2.5f, 3f);
        private bool m_isSpawning = false;
        private Coroutine m_spawnCoroutine;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            m_config = FindFirstObjectByType<ShootingGameConfigMotif>();
            
            DebugLogger.Shooting($"DroneSpawner spawned | IsServer={IsServer}", this);

            // Only server spawns drones
            if (!IsServer)
            {
                DebugLogger.Shooting($"Not server, disabling spawner", this);
                enabled = false;
                return;
            }

            // Subscribe to drone events
            DroneMotif.OnDroneDestroyed += OnDroneDestroyed;
            
            // Calculate room bounds for spawn points
            CalculateRoomBounds();
            
            // Start first wave after delay
            StartCoroutine(StartFirstWaveAfterDelay());
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            DroneMotif.OnDroneDestroyed -= OnDroneDestroyed;
            
            if (m_spawnCoroutine != null)
            {
                StopCoroutine(m_spawnCoroutine);
                m_spawnCoroutine = null;
            }
        }

        private void CalculateRoomBounds()
        {
            if (MRUK.Instance == null || MRUK.Instance.Rooms.Count == 0)
            {
                DebugLogger.Shooting($"No MRUK rooms found, using default bounds", this);
                return;
            }

            var room = MRUK.Instance.Rooms[0];
            m_roomCenter = room.transform.position;
            
            // Get room bounds
            var bounds = room.GetRoomBounds();
            
#if UNITY_EDITOR
            m_roomMinExtent = bounds.min;
#endif
            m_roomMaxExtent = bounds.max;
            
            DebugLogger.Shooting($"Room bounds calculated | min={bounds.min} | max={m_roomMaxExtent}", this);
        }

        private IEnumerator StartFirstWaveAfterDelay()
        {
            DebugLogger.Shooting($"Starting first wave in {m_firstWaveDelay} seconds", this);
            yield return new WaitForSeconds(m_firstWaveDelay);
            StartNextWave();
        }

        /// <summary>
        /// Starts the next wave of drones.
        /// </summary>
        public void StartNextWave()
        {
            if (m_isSpawning) return;
            
            CurrentWave.Value++;
            DronesSpawnedThisWave.Value = 0;
            
            var dronesThisWave = GetDronesForWave(CurrentWave.Value);
            DebugLogger.Shooting($"Starting wave {CurrentWave.Value} | drones={dronesThisWave}", this);
            
            OnWaveStarted?.Invoke(CurrentWave.Value);
            
            m_spawnCoroutine = StartCoroutine(SpawnWave(dronesThisWave));
        }

        private int GetDronesForWave(int wave)
        {
            if (m_config != null)
            {
                return Mathf.Min(m_config.dronesPerWave, m_baseWaveDroneCount + (wave - 1) * m_droneCountIncrease);
            }
            return m_baseWaveDroneCount + (wave - 1) * m_droneCountIncrease;
        }

        private IEnumerator SpawnWave(int totalDrones)
        {
            m_isSpawning = true;
            
            while (DronesSpawnedThisWave.Value < totalDrones)
            {
                // Wait if too many drones are alive
                if (LiveDroneCount.Value >= m_maxLiveDrones)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }
                
                SpawnDrone();
                DronesSpawnedThisWave.Value++;
                
                // Wait between spawns
                if (DronesSpawnedThisWave.Value < totalDrones)
                {
                    var interval = m_config ? m_config.droneSpawnInterval : m_spawnInterval;
                    yield return new WaitForSeconds(interval);
                }
            }
            
            m_isSpawning = false;
            DebugLogger.Shooting($"Wave {CurrentWave.Value} spawning complete | spawned={DronesSpawnedThisWave.Value}", this);
            
            // Start monitoring for wave completion
            StartCoroutine(WaitForWaveCompletion());
        }

        private void SpawnDrone()
        {
            if (m_dronePrefab == null)
            {
                DebugLogger.Shooting($"Drone prefab not set!", this);
                return;
            }

            var spawnPosition = GetRandomSpawnPosition();
            var spawnRotation = Quaternion.LookRotation(m_roomCenter - spawnPosition);
            
            DebugLogger.Shooting($"Spawning drone at {spawnPosition}", this);
            
            // NGO spawning - instantiate and spawn
            var droneGO = Instantiate(m_dronePrefab.gameObject, spawnPosition, spawnRotation);
            var networkObj = droneGO.GetComponent<NetworkObject>();
            if (networkObj != null)
            {
                networkObj.Spawn();
            }
            
            LiveDroneCount.Value++;
            
            DebugLogger.Shooting($"Drone spawned | live drones={LiveDroneCount.Value}", this);
        }

        private Vector3 GetRandomSpawnPosition()
        {
            // Find target player for directional spawning
            var players = FindObjectsByType<PlayerHealthMotif>(FindObjectsSortMode.None);
            Vector3 targetPosition = m_roomCenter;
            
            foreach (var player in players)
            {
                if (player.CurrentHealth.Value > 0)
                {
                    targetPosition = player.transform.position;
                    break;
                }
            }
            
            // Generate spawn position around room perimeter
            var directionToPlayer = (targetPosition - m_roomCenter).normalized;
            directionToPlayer.y = 0f; // Keep horizontal
            
            // Add some randomness (-90 to +90 degrees from player direction)
            var randomAngle = UnityEngine.Random.Range(-90f, 90f);
            var spawnDirection = Quaternion.AngleAxis(randomAngle, Vector3.up) * directionToPlayer;
            
            // Position outside room bounds
            var spawnPosition = m_roomCenter + spawnDirection * m_spawnDistance;
            spawnPosition.y = m_roomMaxExtent.y + m_spawnHeightOffset;
            
            return spawnPosition;
        }

        private void OnDroneDestroyed(DroneMotif drone)
        {
            if (!IsServer) return;
            
            LiveDroneCount.Value = Mathf.Max(0, LiveDroneCount.Value - 1);
            DebugLogger.Shooting($"Drone destroyed | live drones={LiveDroneCount.Value}", this);
        }

        private IEnumerator WaitForWaveCompletion()
        {
            // Wait until all drones are destroyed
            while (LiveDroneCount.Value > 0)
            {
                yield return new WaitForSeconds(1f);
            }
            
            DebugLogger.Shooting($"Wave {CurrentWave.Value} completed!", this);
            OnWaveCompleted?.Invoke(CurrentWave.Value);
            
            // Auto-start next wave after delay
            yield return new WaitForSeconds(3f);
            StartNextWave();
        }

        /// <summary>
        /// Manually trigger next wave (for testing).
        /// </summary>
        [ContextMenu("Force Next Wave")]
        public void ForceNextWave()
        {
            if (!IsServer) return;
            
            // Stop current spawning
            if (m_spawnCoroutine != null)
            {
                StopCoroutine(m_spawnCoroutine);
                m_spawnCoroutine = null;
                m_isSpawning = false;
            }
            
            StartNextWave();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Visualize room bounds
            Gizmos.color = Color.blue;
            var center = (m_roomMinExtent + m_roomMaxExtent) / 2f;
            var size = m_roomMaxExtent - m_roomMinExtent;
            Gizmos.DrawWireCube(center, size);
            
            // Visualize spawn distance
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(m_roomCenter, m_spawnDistance);
            
            // Visualize spawn height
            Gizmos.color = Color.yellow;
            var spawnY = m_roomMaxExtent.y + m_spawnHeightOffset;
            Gizmos.DrawWireCube(new Vector3(m_roomCenter.x, spawnY, m_roomCenter.z), 
                               new Vector3(m_spawnDistance * 2f, 0.1f, m_spawnDistance * 2f));
        }
#endif
    }
}
