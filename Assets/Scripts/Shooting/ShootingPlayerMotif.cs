// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using Meta.XR.Samples;
using Meta.XR.MultiplayerBlocks.Fusion;

namespace MRMotifs.SharedActivities.ShootingSample
{
    /// <summary>
    /// Handles player shooting mechanics in a co-located multiplayer shooting game.
    /// Spawns networked projectiles when the player presses the trigger and manages
    /// the player's weapon visuals locally.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class ShootingPlayerMotif : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [Tooltip("The networked bullet prefab to spawn when shooting.")]
        [SerializeField] private NetworkObject m_bulletPrefab;

        [Tooltip("Force applied to the bullet when fired.")]
        [SerializeField] private float m_fireForce = 15f;

        [Tooltip("Time between shots in seconds.")]
        [SerializeField] private float m_fireRate = 0.2f;

        [Tooltip("Lifetime of bullets in seconds before auto-despawn.")]
        [SerializeField] private float m_bulletLifetime = 5f;

        [Header("Audio")]
        [Tooltip("Audio source for shooting sounds.")]
        [SerializeField] private AudioSource m_audioSource;

        [Tooltip("Sound played when firing.")]
        [SerializeField] private AudioClip m_fireSound;

        [Header("Weapon Visuals")]
        [Tooltip("Weapon prefab to attach to the right hand/controller.")]
        [SerializeField] private GameObject m_weaponPrefab;

        [Tooltip("Position offset for the weapon relative to the controller.")]
        [SerializeField] private Vector3 m_weaponPositionOffset = new Vector3(0f, 0f, 0.05f);

        [Tooltip("Rotation offset for the weapon relative to the controller (Euler angles).")]
        [SerializeField] private Vector3 m_weaponRotationOffset = new Vector3(0f, 180f, 0f);

        [Tooltip("Scale of the weapon model.")]
        [SerializeField] private float m_weaponScale = 0.8f;

        [Header("References")]
        [Tooltip("Transform used as the firing point (controller or hand).")]
        [SerializeField] private Transform m_leftFirePoint;

        [Tooltip("Transform used as the firing point (controller or hand).")]
        [SerializeField] private Transform m_rightFirePoint;

        private float m_lastFireTime;
        private OVRCameraRig m_cameraRig;
        private PlayerHealthMotif m_playerHealth;
        private GameObject m_rightWeaponInstance;
        private GameObject m_leftWeaponInstance;
        private Transform m_rightMuzzle;
        private Transform m_leftMuzzle;
        private NetworkRunner m_networkRunner;
        private AudioSource m_spawnedAudioSource;

        /// <summary>
        /// The PlayerRef of the owner of this shooting player.
        /// </summary>
        public PlayerRef OwnerPlayer { get; set; }

        private void Start()
        {
            m_cameraRig = FindAnyObjectByType<OVRCameraRig>();
            m_playerHealth = GetComponent<PlayerHealthMotif>();
            
            // Find the NetworkRunner
            m_networkRunner = FindAnyObjectByType<NetworkRunner>();
            if (m_networkRunner != null)
            {
                OwnerPlayer = m_networkRunner.LocalPlayer;
            }

            // Create audio source for firing sounds
            if (m_audioSource == null)
            {
                m_spawnedAudioSource = gameObject.AddComponent<AudioSource>();
                m_spawnedAudioSource.spatialBlend = 1f;
                m_audioSource = m_spawnedAudioSource;
            }

            // Set up fire points from camera rig if not assigned
            if (m_cameraRig != null)
            {
                if (m_leftFirePoint == null)
                {
                    m_leftFirePoint = m_cameraRig.leftControllerAnchor;
                }
                if (m_rightFirePoint == null)
                {
                    m_rightFirePoint = m_cameraRig.rightControllerAnchor;
                }

                // Spawn weapon models (this is a local player component)
                SpawnWeaponModels();
            }
            
            Debug.Log("[ShootingPlayerMotif] Started - weapons spawned");
        }

        private void SpawnWeaponModels()
        {
            if (m_weaponPrefab == null)
            {
                Debug.Log("[ShootingPlayerMotif] SpawnWeaponModels - No weapon prefab assigned");
                return;
            }

            Debug.Log($"[ShootingPlayerMotif] SpawnWeaponModels - Prefab: {m_weaponPrefab.name}, RightPoint: {m_rightFirePoint != null}, LeftPoint: {m_leftFirePoint != null}");

            // Spawn right hand weapon
            if (m_rightFirePoint != null)
            {
                m_rightWeaponInstance = Instantiate(m_weaponPrefab, m_rightFirePoint);
                m_rightWeaponInstance.transform.localPosition = m_weaponPositionOffset;
                m_rightWeaponInstance.transform.localRotation = Quaternion.Euler(m_weaponRotationOffset);
                m_rightWeaponInstance.transform.localScale = Vector3.one * m_weaponScale;
                m_rightWeaponInstance.name = "RightWeapon";

                // Try to find muzzle point for accurate bullet spawning
                m_rightMuzzle = FindMuzzlePoint(m_rightWeaponInstance);
                Debug.Log($"[ShootingPlayerMotif] Right weapon spawned at {m_rightFirePoint.name}");
            }

            // Optionally spawn left hand weapon (dual wield)
            if (m_leftFirePoint != null)
            {
                m_leftWeaponInstance = Instantiate(m_weaponPrefab, m_leftFirePoint);
                m_leftWeaponInstance.transform.localPosition = m_weaponPositionOffset;
                m_leftWeaponInstance.transform.localRotation = Quaternion.Euler(m_weaponRotationOffset);
                m_leftWeaponInstance.transform.localScale = Vector3.one * m_weaponScale;
                m_leftWeaponInstance.name = "LeftWeapon";

                // Mirror the left weapon
                var localScale = m_leftWeaponInstance.transform.localScale;
                localScale.x *= -1f;
                m_leftWeaponInstance.transform.localScale = localScale;

                m_leftMuzzle = FindMuzzlePoint(m_leftWeaponInstance);
                Debug.Log($"[ShootingPlayerMotif] Left weapon spawned at {m_leftFirePoint.name}");
            }
        }

        private Transform FindMuzzlePoint(GameObject weapon)
        {
            // Look for common muzzle point names
            var muzzleNames = new[] { "Muzzle", "muzzle", "FirePoint", "firePoint", "MuzzlePoint", "Barrel" };
            foreach (var name in muzzleNames)
            {
                var muzzle = weapon.transform.Find(name);
                if (muzzle != null)
                {
                    return muzzle;
                }

                // Search recursively
                muzzle = FindChildRecursive(weapon.transform, name);
                if (muzzle != null)
                {
                    return muzzle;
                }
            }
            return null;
        }

        private Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.Contains(name))
                {
                    return child;
                }
                var result = FindChildRecursive(child, name);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private void Update()
        {
            // Only process input if network runner is available
            if (m_networkRunner == null || !m_networkRunner.IsRunning)
            {
                return;
            }

            // Don't allow shooting if player is dead
            // Check if PlayerHealthMotif is spawned before accessing networked properties
            if (m_playerHealth != null && m_playerHealth.Object != null && m_playerHealth.Object.IsValid && m_playerHealth.IsDead)
            {
                return;
            }

            HandleShootingInput();
        }

        private void HandleShootingInput()
        {
            // Check for trigger input on either controller
            var leftTrigger = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
            var rightTrigger = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);

            if (leftTrigger && CanFire())
            {
                Debug.Log($"[ShootingPlayerMotif] Left trigger pressed! BulletPrefab: {m_bulletPrefab != null}, LeftFirePoint: {m_leftFirePoint != null}");
                var firePoint = m_leftMuzzle != null ? m_leftMuzzle : m_leftFirePoint;
                FireBullet(firePoint, m_leftFirePoint);
            }

            if (rightTrigger && CanFire())
            {
                Debug.Log($"[ShootingPlayerMotif] Right trigger pressed! BulletPrefab: {m_bulletPrefab != null}, RightFirePoint: {m_rightFirePoint != null}");
                var firePoint = m_rightMuzzle != null ? m_rightMuzzle : m_rightFirePoint;
                FireBullet(firePoint, m_rightFirePoint);
            }
        }

        private bool CanFire()
        {
            return Time.time - m_lastFireTime >= m_fireRate;
        }

        private void FireBullet(Transform firePoint, Transform directionSource)
        {
            if (firePoint == null || m_bulletPrefab == null || m_networkRunner == null)
            {
                Debug.LogWarning($"[ShootingPlayerMotif] Cannot fire! firePoint: {firePoint != null}, bulletPrefab: {m_bulletPrefab != null}, networkRunner: {m_networkRunner != null}");
                return;
            }

            m_lastFireTime = Time.time;

            // Use direction from controller/hand, not necessarily muzzle
            var direction = directionSource != null ? directionSource.forward : firePoint.forward;

            Debug.Log($"[ShootingPlayerMotif] Spawning bullet at {firePoint.position}, direction: {direction}");

            // Spawn the networked bullet
            var bullet = m_networkRunner.Spawn(
                m_bulletPrefab,
                firePoint.position,
                Quaternion.LookRotation(direction),
                m_networkRunner.LocalPlayer
            );

            if (bullet != null)
            {
                Debug.Log($"[ShootingPlayerMotif] Bullet spawned successfully!");
                var bulletMotif = bullet.GetComponent<BulletMotif>();
                if (bulletMotif != null)
                {
                    bulletMotif.Initialize(OwnerPlayer, direction * m_fireForce, m_bulletLifetime);
                }

                // Play fire sound locally
                PlayFireSound();
            }
            else
            {
                Debug.LogError("[ShootingPlayerMotif] Failed to spawn bullet!");
            }
        }

        private void PlayFireSound()
        {
            if (m_audioSource != null && m_fireSound != null)
            {
                m_audioSource.PlayOneShot(m_fireSound);
            }
        }

        /// <summary>
        /// Sets the bullet prefab at runtime (useful for weapon switching).
        /// </summary>
        public void SetBulletPrefab(NetworkObject prefab)
        {
            m_bulletPrefab = prefab;
        }

        /// <summary>
        /// Sets the fire rate at runtime.
        /// </summary>
        public void SetFireRate(float rate)
        {
            m_fireRate = rate;
        }

        /// <summary>
        /// Configures the weapon visual prefab and its positioning at runtime.
        /// Call this before Spawned() is called, or call SpawnWeaponModels() manually after.
        /// </summary>
        /// <param name="weaponPrefab">The weapon model prefab to attach to controllers.</param>
        /// <param name="positionOffset">Local position offset from the controller anchor.</param>
        /// <param name="rotationOffset">Local rotation offset in Euler angles.</param>
        /// <param name="scale">Uniform scale of the weapon.</param>
        public void ConfigureWeapon(GameObject weaponPrefab, Vector3 positionOffset, Vector3 rotationOffset, float scale)
        {
            m_weaponPrefab = weaponPrefab;
            m_weaponPositionOffset = positionOffset;
            m_weaponRotationOffset = rotationOffset;
            m_weaponScale = scale;
        }

        /// <summary>
        /// Manually spawns weapon models if ConfigureWeapon was called after Start().
        /// </summary>
        public void RespawnWeaponModels()
        {
            // Destroy existing weapons
            if (m_rightWeaponInstance != null)
            {
                Destroy(m_rightWeaponInstance);
            }
            if (m_leftWeaponInstance != null)
            {
                Destroy(m_leftWeaponInstance);
            }

            // Ensure camera rig and fire points are set up
            if (m_cameraRig == null)
            {
                m_cameraRig = FindAnyObjectByType<OVRCameraRig>();
            }
            
            if (m_cameraRig != null)
            {
                if (m_leftFirePoint == null)
                {
                    m_leftFirePoint = m_cameraRig.leftControllerAnchor;
                }
                if (m_rightFirePoint == null)
                {
                    m_rightFirePoint = m_cameraRig.rightControllerAnchor;
                }
            }

            Debug.Log($"[ShootingPlayerMotif] RespawnWeaponModels - CameraRig: {m_cameraRig != null}, WeaponPrefab: {m_weaponPrefab != null}, RightFirePoint: {m_rightFirePoint != null}");

            // Spawn new weapons
            SpawnWeaponModels();
        }
    }
}
#endif
