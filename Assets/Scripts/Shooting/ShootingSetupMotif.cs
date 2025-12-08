// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using Meta.XR.MultiplayerBlocks.Shared;
using Meta.XR.MultiplayerBlocks.Fusion;
using Meta.XR.Samples;
using MRMotifs.Shooting.Spawning;

namespace MRMotifs.Shooting
{
    /// <summary>
    /// Scene-based setup for the shooting game. Attaches shooting components to players
    /// when their avatars spawn. Place this in the scene alongside the Avatar Spawner Handler.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class ShootingSetupMotif : MonoBehaviour
    {
        [Header("Prefabs")]
        [Tooltip("The networked bullet prefab.")]
        [SerializeField] private NetworkObject m_bulletPrefab;

        [Tooltip("Muzzle flash prefab to spawn when firing.")]
        [SerializeField] private GameObject m_muzzleFlashPrefab;

        [Header("Weapon Visuals")]
        [Tooltip("The weapon model prefab to attach to controllers (e.g., from FA FPS Weapons Pack).")]
        [SerializeField] private GameObject m_weaponPrefab;

        [Tooltip("Position offset for the weapon relative to the controller.")]
        [SerializeField] private Vector3 m_weaponPositionOffset = new Vector3(0f, -0.02f, 0.08f);

        [Tooltip("Rotation offset for the weapon relative to the controller (Euler angles).")]
        [SerializeField] private Vector3 m_weaponRotationOffset = new Vector3(-90f, 0f, 0f);

        [Tooltip("Scale of the weapon model.")]
        [SerializeField] private float m_weaponScale = 0.8f;

        private NetworkRunner m_networkRunner;
        private SpawnManagerMotif m_spawnManager;
        private ShootingGameManagerMotif m_gameManager;
        private OVRCameraRig m_cameraRig;

        private void Awake()
        {
            Debug.Log("[ShootingSetupMotif] Awake - subscribing to events");
            FusionBBEvents.OnSceneLoadDone += OnNetworkLoaded;
            AvatarEntity.OnSpawned += OnAvatarSpawned;
        }

        private void Start()
        {
            Debug.Log($"[ShootingSetupMotif] Start - bulletPrefab: {(m_bulletPrefab != null ? m_bulletPrefab.name : "NULL")}, weaponPrefab: {(m_weaponPrefab != null ? m_weaponPrefab.name : "NULL")}");
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
            // Wait for avatar to be fully ready
            yield return new WaitForSeconds(1.5f);

            // Check if avatar was destroyed during wait (e.g., network disconnect)
            if (avatarEntity == null || avatarEntity.gameObject == null)
            {
                Debug.LogWarning("[ShootingSetupMotif] Avatar was destroyed before setup completed");
                yield break;
            }

            // Use AvatarEntity.IsLocal to correctly identify the local player
            // HasStateAuthority only returns true for host, not for all local players
            bool isLocalPlayer = avatarEntity.IsLocal;
            
            Debug.Log($"[ShootingSetupMotif] Setting up avatar - IsLocal: {isLocalPlayer}");

            // Add collider to avatar for bullet collision detection
            SetupAvatarCollider(avatarEntity.gameObject);

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

            // Add wrist health display for local player
            var wristHealth = avatarEntity.gameObject.GetComponent<WristHealthDisplayMotif>();
            if (wristHealth == null)
            {
                avatarEntity.gameObject.AddComponent<WristHealthDisplayMotif>();
            }

            // Configure bullet prefab
            if (m_bulletPrefab != null)
            {
                shootingPlayer.SetBulletPrefab(m_bulletPrefab);
            }

            // Configure muzzle flash
            if (m_muzzleFlashPrefab != null)
            {
                shootingPlayer.SetMuzzleFlashPrefab(m_muzzleFlashPrefab);
            }

            // Configure audio
            if (m_gameManager != null)
            {
                var audioMotif = m_gameManager.GetComponent<ShootingAudioMotif>();
                if (audioMotif != null && audioMotif.FireClip != null)
                {
                    shootingPlayer.SetFireSound(audioMotif.FireClip);
                }
            }

            // Configure weapon visuals
            if (m_weaponPrefab != null)
            {
                // Check if ShootingPlayerMotif already has a weapon prefab assigned to avoid duplication
                // If it does, we assume it handles spawning itself.
                // However, ShootingPlayerMotif.SpawnWeaponModels() is called in Start(), 
                // and we are configuring it here potentially after Start() or before.
                // To be safe, we only configure if it doesn't have one, OR we rely on ShootingPlayerMotif's logic
                // to clear existing weapons before spawning new ones.
                
                // Actually, ShootingPlayerMotif.SpawnWeaponModels() is called in its Start().
                // If we add the component here, Start() runs immediately after.
                // If the component was already there, Start() already ran.
                
                // Let's just set the config. ShootingPlayerMotif.RespawnWeaponModels() handles cleanup.
                shootingPlayer.ConfigureWeapon(m_weaponPrefab, m_weaponPositionOffset, m_weaponRotationOffset, m_weaponScale);
                
                // Only call Respawn if the component was already active (Start ran)
                // If we just added it, Start() will pick up the new config.
                if (shootingPlayer.gameObject.activeInHierarchy)
                {
                     shootingPlayer.RespawnWeaponModels();
                }
            }

            Debug.Log("[ShootingSetupMotif] Local player shooting setup complete");
        }

        /// <summary>
        /// Sets up a collider on the avatar so bullets can collide with it.
        /// Uses a CapsuleCollider sized for a typical human avatar.
        /// The collider is set as a TRIGGER to prevent physics bouncing.
        /// </summary>
        private void SetupAvatarCollider(GameObject avatarObj)
        {
            Debug.Log($"[ShootingSetupMotif] SetupAvatarCollider called for: {avatarObj.name}");
            
            // Check if collider already exists
            var existingCollider = avatarObj.GetComponent<Collider>();
            if (existingCollider != null)
            {
                // Ensure existing collider is a trigger
                existingCollider.isTrigger = true;
                Debug.Log($"[ShootingSetupMotif] Avatar already has a collider ({existingCollider.GetType().Name}), set to trigger");
                return;
            }

            // Add a CapsuleCollider sized for a human avatar
            var capsule = avatarObj.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.9f, 0f); // Center at chest height
            capsule.radius = 0.3f; // Approximate torso width
            capsule.height = 1.8f; // Approximate human height
            capsule.direction = 1; // Y-axis (vertical)
            capsule.isTrigger = true; // Use trigger to prevent physics bouncing

            // Rigidbody is still needed for trigger detection with moving objects
            var rb = avatarObj.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = avatarObj.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true; // Don't let physics move the avatar
            rb.useGravity = false;

            Debug.Log($"[ShootingSetupMotif] Added CapsuleCollider to avatar - center: {capsule.center}, radius: {capsule.radius}, height: {capsule.height}, isTrigger: {capsule.isTrigger}");
            Debug.Log($"[ShootingSetupMotif] Avatar layer: {avatarObj.layer} ({LayerMask.LayerToName(avatarObj.layer)})");
        }
    }
}
#endif
