// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using System.Collections;
using Meta.XR.MultiplayerBlocks.Shared;
using Meta.XR.MultiplayerBlocks.Fusion;
using Meta.XR.Samples;
using MRMotifs.SharedActivities.Spawning;
using MRMotifs.SharedActivities.Startup;

namespace MRMotifs.SharedActivities.Avatars
{
    /// <summary>
    /// Handles the spawning of avatars in the scene, managing their positions using the spawn manager.
    /// Also, responsible for releasing spawn locations when players leave the scene.
    /// 
    /// Avatar spawning is gated on GameStartupManagerMotif completing its flow to prevent race conditions.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class AvatarSpawnerHandlerMotif : MonoBehaviour
    {
        [Tooltip("Reference to the SpawnManagerMotif, which manages the spawn locations and queues.")]
        [SerializeField]
        private SpawnManagerMotif spawnManagerMotif;

        private NetworkRunner m_networkRunner;
        private GameStartupManagerMotif m_startupManager;

        private void Awake()
        {
            FusionBBEvents.OnSceneLoadDone += OnLoaded;
            AvatarEntity.OnSpawned += HandleAvatarSpawned;
            FusionBBEvents.OnPlayerLeft += FreeSpawnLocation;
        }

        private void Start()
        {
            m_startupManager = FindAnyObjectByType<GameStartupManagerMotif>();
        }

        private void OnDestroy()
        {
            FusionBBEvents.OnSceneLoadDone -= OnLoaded;
            AvatarEntity.OnSpawned -= HandleAvatarSpawned;
            FusionBBEvents.OnPlayerLeft -= FreeSpawnLocation;
        }

        private void OnLoaded(NetworkRunner networkRunner)
        {
            m_networkRunner = networkRunner;
        }

        private void HandleAvatarSpawned(AvatarEntity avatarEntity)
        {
            StartCoroutine(WaitForSpawnedAndEnqueue(avatarEntity));
        }

        private IEnumerator WaitForSpawnedAndEnqueue(AvatarEntity avatarEntity)
        {
            // Wait for startup manager to complete its flow (room scan, colocation, etc.)
            if (m_startupManager == null)
            {
                m_startupManager = FindAnyObjectByType<GameStartupManagerMotif>();
            }

            if (m_startupManager != null)
            {
                Debug.Log("[AvatarSpawnerHandlerMotif] Waiting for startup flow to complete...");
                float startupTimeout = 60f;
                float startupElapsed = 0f;
                while (!m_startupManager.IsStartupComplete && startupElapsed < startupTimeout)
                {
                    if (m_startupManager.CurrentState == StartupState.Error)
                    {
                        Debug.LogWarning("[AvatarSpawnerHandlerMotif] Startup flow failed, aborting avatar spawn");
                        yield break;
                    }
                    yield return new WaitForSeconds(0.5f);
                    startupElapsed += 0.5f;
                }

                if (!m_startupManager.IsStartupComplete)
                {
                    Debug.LogWarning("[AvatarSpawnerHandlerMotif] Startup flow timed out, proceeding anyway");
                }
                else
                {
                    Debug.Log("[AvatarSpawnerHandlerMotif] Startup flow complete, proceeding with avatar spawn");
                }
            }

            // Additional delay required since Avatars v28+ require some additional time to be loaded
            // No event to await the full "readiness" of the avatar is available yet
            yield return new WaitForSeconds(1.5f);

            // Check if spawnManagerMotif is available - try to find it if null
            if (spawnManagerMotif == null)
            {
                spawnManagerMotif = FindAnyObjectByType<SpawnManagerMotif>();
            }

            // Wait for SpawnManagerMotif to exist (network object may spawn later)
            float timeout = 10f;
            float elapsed = 0f;
            while (spawnManagerMotif == null && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
                spawnManagerMotif = FindAnyObjectByType<SpawnManagerMotif>();
            }

            if (spawnManagerMotif == null)
            {
                Debug.LogWarning("[AvatarSpawnerHandlerMotif] SpawnManagerMotif not found after timeout, cannot enqueue player for spawn");
                yield break;
            }

            while (!spawnManagerMotif.HasSpawned)
            {
                yield return null;
            }

            var avatarNetworkObj = avatarEntity.gameObject.GetComponent<AvatarBehaviourFusion>();
            if (avatarNetworkObj == null || !avatarNetworkObj.HasStateAuthority)
            {
                yield break;
            }

            yield return spawnManagerMotif.StartCoroutine(
                spawnManagerMotif.EnqueuePlayerForSpawn(m_networkRunner.LocalPlayer, avatarNetworkObj));
        }

        private void FreeSpawnLocation(NetworkRunner runner, PlayerRef player)
        {
            if (spawnManagerMotif == null)
            {
                return;
            }

            for (var i = 0; i < spawnManagerMotif.OccupyingPlayers.Length; i++)
            {
                if (spawnManagerMotif.OccupyingPlayers.Get(i) != player)
                {
                    continue;
                }

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
