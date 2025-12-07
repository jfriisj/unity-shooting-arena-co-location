// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using Meta.XR.MultiplayerBlocks.Shared;
using Meta.XR.MultiplayerBlocks.Fusion;
using Meta.XR.Samples;
using MRMotifs.SharedActivities.Spawning;
using MRMotifs.ColocatedExperiences.Colocation;

namespace MRMotifs.SharedActivities.ShootingSample
{
    /// <summary>
    /// Scene-based setup for the shooting game. Attaches shooting components to players
    /// when their avatars spawn. Place this in the scene alongside the Avatar Spawner Handler.
    /// 
    /// Setup is gated on ColocationStartup completing its flow to prevent race conditions.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class ShootingSetupMotif : MonoBehaviour
    {
        [Header("Prefabs")]
        [Tooltip("The networked bullet prefab.")]
        [SerializeField] private NetworkObject m_bulletPrefab;

        [Tooltip("Bullet hole decal prefab from Easy FPS.")]
        [SerializeField] private GameObject m_bulletHolePrefab;

        [Header("Weapon Visuals")]
        [Tooltip("The weapon model prefab to attach to controllers (e.g., from FA FPS Weapons Pack).")]
        [SerializeField] private GameObject m_weaponPrefab;

        [Tooltip("Position offset for the weapon relative to the controller.")]
        [SerializeField] private Vector3 m_weaponPositionOffset = new Vector3(0f, -0.02f, 0.08f);

        [Tooltip("Rotation offset for the weapon relative to the controller (Euler angles).")]
        [SerializeField] private Vector3 m_weaponRotationOffset = new Vector3(-90f, 0f, 0f);

        [Tooltip("Scale of the weapon model.")]
        [SerializeField] private float m_weaponScale = 0.8f;

        [Header("Muzzle Flash (Easy FPS)")]
        [Tooltip("Array of muzzle flash prefabs from Easy FPS. One is randomly selected per shot.")]
        [SerializeField] private GameObject[] m_muzzleFlashPrefabs;

        private NetworkRunner m_networkRunner;
        private SpawnManagerMotif m_spawnManager;
        private ShootingGameManagerMotif m_gameManager;
        private OVRCameraRig m_cameraRig;
        private ColocationStartup m_colocationStartup;

        [Tooltip("Timeout in seconds to wait for colocation startup to complete.")]
        [SerializeField]
        private float m_startupTimeout = 60f;

        private void Awake()
        {
            Debug.Log("[ShootingSetupMotif] Awake - subscribing to events");
            FusionBBEvents.OnSceneLoadDone += OnNetworkLoaded;
            AvatarEntity.OnSpawned += OnAvatarSpawned;
        }

        private void Start()
        {
            Debug.Log($"[ShootingSetupMotif] Start - bulletPrefab: {(m_bulletPrefab != null ? m_bulletPrefab.name : "NULL")}, weaponPrefab: {(m_weaponPrefab != null ? m_weaponPrefab.name : "NULL")}");
            m_colocationStartup = FindAnyObjectByType<ColocationStartup>();
            
            if (m_colocationStartup == null)
            {
                Debug.LogError("[ShootingSetupMotif] ColocationStartup NOT FOUND. " +
                    "Shooting setup will not be gated on colocation. Check scene setup.");
            }
            else
            {
                Debug.Log($"[ShootingSetupMotif] Found ColocationStartup. IsReady: {m_colocationStartup.IsReady}");
            }
        }

        private void OnDestroy()
        {
            FusionBBEvents.OnSceneLoadDone -= OnNetworkLoaded;
            AvatarEntity.OnSpawned -= OnAvatarSpawned;
        }

        private void OnNetworkLoaded(NetworkRunner networkRunner)
        {
            m_networkRunner = networkRunner;
            m_spawnManager = FindAnyObjectByType<SpawnManagerMotif>();
            m_gameManager = FindAnyObjectByType<ShootingGameManagerMotif>();
            m_cameraRig = FindAnyObjectByType<OVRCameraRig>();

            Debug.Log($"[ShootingSetupMotif] Network loaded - runner: {networkRunner != null}, spawnManager: {m_spawnManager != null}, gameManager: {m_gameManager != null}");
        }

        private void OnAvatarSpawned(AvatarEntity avatarEntity)
        {
            Debug.Log($"[ShootingSetupMotif] OnAvatarSpawned called! Entity: {avatarEntity?.gameObject.name}, isLocal: {avatarEntity?.IsLocal}");
            StartCoroutine(SetupShootingForAvatar(avatarEntity));
        }

        private System.Collections.IEnumerator SetupShootingForAvatar(AvatarEntity avatarEntity)
        {
            // Wait for colocation startup to complete
            if (m_colocationStartup == null)
            {
                m_colocationStartup = FindAnyObjectByType<ColocationStartup>();
            }

            if (m_colocationStartup != null)
            {
                Debug.Log($"[ShootingSetupMotif] Waiting for ColocationStartup. " +
                    $"Current state: {m_colocationStartup.State}, IsReady: {m_colocationStartup.IsReady}");
                
                float elapsed = 0f;
                while (!m_colocationStartup.IsReady && elapsed < m_startupTimeout)
                {
                    yield return new WaitForSeconds(0.5f);
                    elapsed += 0.5f;
                    
                    if (elapsed % 5f < 0.5f) // Log every 5 seconds
                    {
                        Debug.Log($"[ShootingSetupMotif] Still waiting... " +
                            $"State: {m_colocationStartup.State}, Elapsed: {elapsed:F1}s/{m_startupTimeout}s");
                    }
                }

                if (!m_colocationStartup.IsReady)
                {
                    Debug.LogError($"[ShootingSetupMotif] ColocationStartup TIMEOUT after {m_startupTimeout}s. " +
                        $"Final state: {m_colocationStartup.State}. Shooting setup may fail.");
                }
                else
                {
                    Debug.Log($"[ShootingSetupMotif] ColocationStartup ready! " +
                        $"IsHost: {m_colocationStartup.IsHost}, proceeding with shooting setup");
                }
            }
            else
            {
                Debug.LogError("[ShootingSetupMotif] ColocationStartup NOT FOUND. " +
                    "Cannot gate shooting setup on colocation. Check scene setup.");
            }

            // Wait for avatar to be fully ready
            yield return new WaitForSeconds(1.5f);

            // Check if avatar was destroyed during wait (e.g., network disconnect)
            if (avatarEntity == null || avatarEntity.gameObject == null)
            {
                Debug.LogWarning("[ShootingSetupMotif] Avatar was destroyed before setup completed");
                yield break;
            }

            // Wait for SpawnManager to be available (may spawn later in shared mode)
            if (m_spawnManager == null)
            {
                m_spawnManager = FindAnyObjectByType<SpawnManagerMotif>();
            }
            
            float spawnManagerTimeout = 10f;
            float spawnManagerElapsed = 0f;
            while (m_spawnManager == null && spawnManagerElapsed < spawnManagerTimeout)
            {
                yield return new WaitForSeconds(0.5f);
                spawnManagerElapsed += 0.5f;
                m_spawnManager = FindAnyObjectByType<SpawnManagerMotif>();
            }

            if (m_spawnManager == null)
            {
                Debug.LogWarning("[ShootingSetupMotif] SpawnManager not found, continuing without spawn point");
            }

            // Use AvatarEntity.IsLocal to correctly identify the local player
            // HasStateAuthority only returns true for host, not for all local players
            bool isLocalPlayer = avatarEntity.IsLocal;
            
            Debug.Log($"[ShootingSetupMotif] Setting up avatar - IsLocal: {isLocalPlayer}");

            // Add collider to avatar for bullet hit detection (Meta Avatars don't have colliders by default)
            var avatarCollider = avatarEntity.gameObject.GetComponent<CapsuleCollider>();
            if (avatarCollider == null)
            {
                avatarCollider = avatarEntity.gameObject.AddComponent<CapsuleCollider>();
                avatarCollider.center = new Vector3(0f, 0.85f, 0f); // Center at chest height
                avatarCollider.radius = 0.3f;  // Reasonable body width
                avatarCollider.height = 1.7f;  // Average avatar height
                avatarCollider.direction = 1;  // Y-axis aligned (vertical capsule)
                Debug.Log($"[ShootingSetupMotif] Added CapsuleCollider to avatar: {avatarEntity.name}");
            }

            // Add Rigidbody for collision detection (kinematic so physics doesn't move the avatar)
            var avatarRigidbody = avatarEntity.gameObject.GetComponent<Rigidbody>();
            if (avatarRigidbody == null)
            {
                avatarRigidbody = avatarEntity.gameObject.AddComponent<Rigidbody>();
                avatarRigidbody.isKinematic = true;  // Avatar controlled by tracking, not physics
                avatarRigidbody.useGravity = false;
                Debug.Log($"[ShootingSetupMotif] Added kinematic Rigidbody to avatar: {avatarEntity.name}");
            }

            // Add PlayerHealthMotif to ALL players (needed for player counting and health sync)
            var playerHealth = avatarEntity.gameObject.GetComponent<PlayerHealthMotif>();
            if (playerHealth == null)
            {
                playerHealth = avatarEntity.gameObject.AddComponent<PlayerHealthMotif>();
            }

            // Register ALL players with game manager
            if (m_gameManager != null)
            {
                m_gameManager.RegisterPlayer(playerHealth);
            }

            // Set spawn point for respawning
            if (m_spawnManager != null && m_spawnManager.ObjectOfInterest != null)
            {
                playerHealth.SetSpawnPoint(m_spawnManager.ObjectOfInterest.transform);
            }

            // Only add shooting controls to local player
            if (!isLocalPlayer)
            {
                Debug.Log("[ShootingSetupMotif] Remote player setup complete");
                yield break;
            }

            Debug.Log("[ShootingSetupMotif] Setting up local player shooting components");

            // Add ShootingPlayerMotif if not present
            var shootingPlayer = avatarEntity.gameObject.GetComponent<ShootingPlayerMotif>();
            if (shootingPlayer == null)
            {
                shootingPlayer = avatarEntity.gameObject.AddComponent<ShootingPlayerMotif>();
            }

            // Configure bullet prefab
            if (m_bulletPrefab != null)
            {
                shootingPlayer.SetBulletPrefab(m_bulletPrefab);
            }

            // Configure weapon visuals
            if (m_weaponPrefab != null)
            {
                shootingPlayer.ConfigureWeapon(m_weaponPrefab, m_weaponPositionOffset, m_weaponRotationOffset, m_weaponScale);
                shootingPlayer.RespawnWeaponModels();
            }

            // Configure muzzle flash effects from Easy FPS
            if (m_muzzleFlashPrefabs != null && m_muzzleFlashPrefabs.Length > 0)
            {
                shootingPlayer.SetMuzzleFlashPrefabs(m_muzzleFlashPrefabs);
            }

            // Configure fire sound from ShootingAudioMotif
            var audioMotif = FindAnyObjectByType<ShootingAudioMotif>();
            if (audioMotif != null && audioMotif.FireClip != null)
            {
                shootingPlayer.SetFireSound(audioMotif.FireClip);
            }

            Debug.Log("[ShootingSetupMotif] Local player shooting setup complete");
        }
    }
}
#endif
