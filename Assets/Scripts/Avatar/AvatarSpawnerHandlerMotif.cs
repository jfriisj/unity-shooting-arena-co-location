// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using System.Collections;
using Meta.XR.MultiplayerBlocks.Shared;
using Meta.XR.MultiplayerBlocks.Fusion;
using Meta.XR.Samples;
using MRMotifs.SharedActivities.Spawning;
using MRMotifs.ColocatedExperiences.Colocation;

namespace MRMotifs.SharedActivities.Avatars
{
    /// <summary>
    /// Handles the spawning of avatars in the scene, managing their positions using the spawn manager.
    /// Also, responsible for releasing spawn locations when players leave the scene.
    /// 
    /// Avatar spawning is gated on ColocationStartup completing its flow.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class AvatarSpawnerHandlerMotif : MonoBehaviour
    {
        [Tooltip("Reference to the SpawnManagerMotif, which manages the spawn locations and queues.")]
        [SerializeField]
        private SpawnManagerMotif spawnManagerMotif;

        [Tooltip("Timeout in seconds to wait for colocation startup to complete.")]
        [SerializeField]
        private float m_startupTimeout = 60f;

        private NetworkRunner m_networkRunner;
        private ColocationStartup m_colocationStartup;

        private void Awake()
        {
            FusionBBEvents.OnSceneLoadDone += OnLoaded;
            AvatarEntity.OnSpawned += HandleAvatarSpawned;
            FusionBBEvents.OnPlayerLeft += FreeSpawnLocation;
            
            Debug.Log("[AvatarSpawnerHandlerMotif] Awake - event handlers registered");
        }

        private void Start()
        {
            m_colocationStartup = FindAnyObjectByType<ColocationStartup>();
            
            if (m_colocationStartup == null)
            {
                Debug.LogError("[AvatarSpawnerHandlerMotif] ColocationStartup not found in scene! " +
                    "Avatar spawning will not be gated on colocation completion. " +
                    "Ensure ColocationStartup component exists in the scene.");
            }
            else
            {
                Debug.Log($"[AvatarSpawnerHandlerMotif] Found ColocationStartup. " +
                    $"IsReady: {m_colocationStartup.IsReady}, IsHost: {m_colocationStartup.IsHost}");
            }
        }

        private void OnDestroy()
        {
            FusionBBEvents.OnSceneLoadDone -= OnLoaded;
            AvatarEntity.OnSpawned -= HandleAvatarSpawned;
            FusionBBEvents.OnPlayerLeft -= FreeSpawnLocation;
            
            Debug.Log("[AvatarSpawnerHandlerMotif] OnDestroy - event handlers unregistered");
        }

        private void OnLoaded(NetworkRunner networkRunner)
        {
            m_networkRunner = networkRunner;
            Debug.Log($"[AvatarSpawnerHandlerMotif] Network scene loaded. Runner: {networkRunner?.name}");
        }

        private void HandleAvatarSpawned(AvatarEntity avatarEntity)
        {
            Debug.Log($"[AvatarSpawnerHandlerMotif] Avatar spawned: {avatarEntity?.name}");
            StartCoroutine(WaitForSpawnedAndEnqueue(avatarEntity));
        }

        private IEnumerator WaitForSpawnedAndEnqueue(AvatarEntity avatarEntity)
        {
            Debug.Log("[AvatarSpawnerHandlerMotif] Starting spawn enqueue process...");
            
            // Wait for colocation startup to complete
            if (m_colocationStartup == null)
            {
                m_colocationStartup = FindAnyObjectByType<ColocationStartup>();
            }

            if (m_colocationStartup != null)
            {
                Debug.Log($"[AvatarSpawnerHandlerMotif] Waiting for ColocationStartup. " +
                    $"Current state: {m_colocationStartup.State}, IsReady: {m_colocationStartup.IsReady}");
                
                float elapsed = 0f;
                while (!m_colocationStartup.IsReady && elapsed < m_startupTimeout)
                {
                    yield return new WaitForSeconds(0.5f);
                    elapsed += 0.5f;
                    
                    if (elapsed % 5f < 0.5f) // Log every 5 seconds
                    {
                        Debug.Log($"[AvatarSpawnerHandlerMotif] Still waiting... " +
                            $"State: {m_colocationStartup.State}, Elapsed: {elapsed:F1}s/{m_startupTimeout}s");
                    }
                }

                if (!m_colocationStartup.IsReady)
                {
                    Debug.LogError($"[AvatarSpawnerHandlerMotif] ColocationStartup TIMEOUT after {m_startupTimeout}s. " +
                        $"Final state: {m_colocationStartup.State}. Avatar spawn may fail.");
                }
                else
                {
                    Debug.Log($"[AvatarSpawnerHandlerMotif] ColocationStartup ready! " +
                        $"IsHost: {m_colocationStartup.IsHost}, GroupUuid: {m_colocationStartup.GroupUuid}");
                }
            }
            else
            {
                Debug.LogError("[AvatarSpawnerHandlerMotif] ColocationStartup NOT FOUND. " +
                    "Cannot gate avatar spawn on colocation. Check scene setup.");
            }

            // Additional delay required since Avatars v28+ require some additional time to be loaded
            Debug.Log("[AvatarSpawnerHandlerMotif] Waiting 1.5s for avatar loading...");
            yield return new WaitForSeconds(1.5f);

            // Find SpawnManagerMotif
            if (spawnManagerMotif == null)
            {
                spawnManagerMotif = FindAnyObjectByType<SpawnManagerMotif>();
                Debug.Log($"[AvatarSpawnerHandlerMotif] SpawnManagerMotif search: {(spawnManagerMotif != null ? "Found" : "Not found")}");
            }

            // Wait for SpawnManagerMotif to exist (network object may spawn later)
            float spawnTimeout = 10f;
            float spawnElapsed = 0f;
            while (spawnManagerMotif == null && spawnElapsed < spawnTimeout)
            {
                yield return new WaitForSeconds(0.5f);
                spawnElapsed += 0.5f;
                spawnManagerMotif = FindAnyObjectByType<SpawnManagerMotif>();
            }

            if (spawnManagerMotif == null)
            {
                Debug.LogError($"[AvatarSpawnerHandlerMotif] SpawnManagerMotif NOT FOUND after {spawnTimeout}s. " +
                    "Cannot enqueue player for spawn. Check that SpawnManagerMotif is in the scene.");
                yield break;
            }

            Debug.Log($"[AvatarSpawnerHandlerMotif] SpawnManagerMotif found. HasSpawned: {spawnManagerMotif.HasSpawned}");

            while (!spawnManagerMotif.HasSpawned)
            {
                yield return null;
            }

            var avatarNetworkObj = avatarEntity.gameObject.GetComponent<AvatarBehaviourFusion>();
            if (avatarNetworkObj == null)
            {
                Debug.LogError($"[AvatarSpawnerHandlerMotif] AvatarBehaviourFusion NOT FOUND on avatar: {avatarEntity?.name}");
                yield break;
            }
            
            if (!avatarNetworkObj.HasStateAuthority)
            {
                Debug.Log($"[AvatarSpawnerHandlerMotif] Not state authority for avatar: {avatarEntity?.name}. Skipping spawn enqueue.");
                yield break;
            }

            Debug.Log($"[AvatarSpawnerHandlerMotif] Enqueueing player {m_networkRunner?.LocalPlayer} for spawn");
            yield return spawnManagerMotif.StartCoroutine(
                spawnManagerMotif.EnqueuePlayerForSpawn(m_networkRunner.LocalPlayer, avatarNetworkObj));
            
            Debug.Log("[AvatarSpawnerHandlerMotif] Player spawn enqueue complete");
        }

        private void FreeSpawnLocation(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"[AvatarSpawnerHandlerMotif] FreeSpawnLocation called for player: {player}");
            
            if (spawnManagerMotif == null)
            {
                Debug.LogWarning("[AvatarSpawnerHandlerMotif] SpawnManagerMotif is null, cannot free spawn location");
                return;
            }

            for (var i = 0; i < spawnManagerMotif.OccupyingPlayers.Length; i++)
            {
                if (spawnManagerMotif.OccupyingPlayers.Get(i) != player)
                {
                    continue;
                }

                Debug.Log($"[AvatarSpawnerHandlerMotif] Releasing spawn location {i} for player {player}");
                spawnManagerMotif.ReleaseLocationRpc(i, player);

                var avatarHandler = FindAnyObjectByType<AvatarMovementHandlerMotif>();
                if (avatarHandler != null)
                {
                    avatarHandler.RemoveRemoteAvatarByPlayer(player);
                }
            }
        }
    }
}
#endif
