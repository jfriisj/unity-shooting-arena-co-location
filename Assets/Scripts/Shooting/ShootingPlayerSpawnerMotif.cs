// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using System.Collections;
using Meta.XR.MultiplayerBlocks.Fusion;
using Meta.XR.Samples;
using MRMotifs.SharedActivities.Spawning;

namespace MRMotifs.SharedActivities.ShootingSample
{
    /// <summary>
    /// Spawns and configures the local player's shooting components when joining the game.
    /// Integrates with the existing SpawnManagerMotif for positioning.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class ShootingPlayerSpawnerMotif : NetworkBehaviour
    {
        [Header("Prefabs")]
        [Tooltip("The networked bullet prefab.")]
        [SerializeField] private NetworkObject bulletPrefab;

        [Header("Weapon Visuals")]
        [Tooltip("The weapon model prefab to attach to controllers (e.g., from FA FPS Weapons Pack).")]
        [SerializeField] private GameObject m_weaponPrefab;

        [Tooltip("Position offset for the weapon relative to the controller.")]
        [SerializeField] private Vector3 m_weaponPositionOffset = new Vector3(0f, -0.02f, 0.08f);

        [Tooltip("Rotation offset for the weapon relative to the controller (Euler angles).")]
        [SerializeField] private Vector3 m_weaponRotationOffset = new Vector3(-90f, 0f, 0f);

        [Tooltip("Scale of the weapon model.")]
        [SerializeField] private float m_weaponScale = 0.8f;

        [Header("Settings")]
        [Tooltip("Offset from hand/controller for the gun visual.")]
        [SerializeField] private Vector3 gunOffset = new Vector3(0, 0, 0.1f);

        private SpawnManagerMotif m_spawnManager;
        private ShootingGameManagerMotif m_gameManager;
        private PlayerHealthMotif m_playerHealth;
        private ShootingPlayerMotif m_shootingPlayer;
        private OVRCameraRig m_cameraRig;
        private bool m_isSetup;

        public override void Spawned()
        {
            base.Spawned();

            m_spawnManager = FindAnyObjectByType<SpawnManagerMotif>();
            m_gameManager = FindAnyObjectByType<ShootingGameManagerMotif>();
            m_cameraRig = FindAnyObjectByType<OVRCameraRig>();

            if (Object.HasStateAuthority)
            {
                StartCoroutine(SetupLocalPlayer());
            }
        }

        private IEnumerator SetupLocalPlayer()
        {
            // Wait for spawn manager to be ready
            while (m_spawnManager == null || !m_spawnManager.HasSpawned)
            {
                m_spawnManager = FindAnyObjectByType<SpawnManagerMotif>();
                yield return new WaitForSeconds(0.1f);
            }

            // Add ShootingPlayerMotif if not present
            m_shootingPlayer = GetComponent<ShootingPlayerMotif>();
            if (m_shootingPlayer == null)
            {
                m_shootingPlayer = gameObject.AddComponent<ShootingPlayerMotif>();
            }

            // Configure bullet prefab
            if (bulletPrefab != null)
            {
                m_shootingPlayer.SetBulletPrefab(bulletPrefab);
            }

            // Configure weapon visuals
            if (m_weaponPrefab != null)
            {
                m_shootingPlayer.ConfigureWeapon(m_weaponPrefab, m_weaponPositionOffset, m_weaponRotationOffset, m_weaponScale);
                m_shootingPlayer.RespawnWeaponModels();
            }

            // Add PlayerHealthMotif if not present
            m_playerHealth = GetComponent<PlayerHealthMotif>();
            if (m_playerHealth == null)
            {
                m_playerHealth = gameObject.AddComponent<PlayerHealthMotif>();
            }

            // Register with game manager
            if (m_gameManager != null)
            {
                m_gameManager.RegisterPlayer(m_playerHealth);
            }

            // Set spawn point for respawning
            if (m_spawnManager != null)
            {
                var spawnPoint = m_spawnManager.ObjectOfInterest?.transform;
                if (spawnPoint != null)
                {
                    m_playerHealth.SetSpawnPoint(spawnPoint);
                }
            }

            m_isSetup = true;
            Debug.Log("[ShootingPlayerSpawnerMotif] Local player setup complete");
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);

            // Unregister from game manager
            if (m_gameManager != null && m_playerHealth != null)
            {
                m_gameManager.UnregisterPlayer(m_playerHealth);
            }
        }

        /// <summary>
        /// Sets the bullet prefab to use for shooting.
        /// </summary>
        public void SetBulletPrefab(NetworkObject prefab)
        {
            bulletPrefab = prefab;
            if (m_shootingPlayer != null)
            {
                m_shootingPlayer.SetBulletPrefab(prefab);
            }
        }
    }
}
#endif
