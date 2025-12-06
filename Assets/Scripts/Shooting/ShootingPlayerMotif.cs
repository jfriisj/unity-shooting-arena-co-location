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
        // Static configuration from ShootingGameConfigMotif
        public static float ConfigFireRate = 0.2f;
        public static float ConfigBulletSpeed = 15f;
        public static float ConfigBulletLifetime = 5f;

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
        [SerializeField] private Vector3 m_weaponRotationOffset = new Vector3(-90f, 180f, 0f);

        [Tooltip("Scale of the weapon model.")]
        [SerializeField] private float m_weaponScale = 0.8f;

        [Header("References")]
        [Tooltip("Transform used as the firing point (controller or hand).")]
        [SerializeField] private Transform m_leftFirePoint;

        [Tooltip("Transform used as the firing point (controller or hand).")]
        [SerializeField] private Transform m_rightFirePoint;

        private float m_lastLeftFireTime;
        private float m_lastRightFireTime;
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
            // Apply static config
            if (ConfigFireRate > 0) m_fireRate = ConfigFireRate;
            if (ConfigBulletSpeed > 0) m_fireForce = ConfigBulletSpeed;
            if (ConfigBulletLifetime > 0) m_bulletLifetime = ConfigBulletLifetime;

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

        private void OnDestroy()
        {
            // Restore controller visibility when this component is destroyed
            ShowControllerModels();

            // Clean up spawned audio source
            if (m_spawnedAudioSource != null)
            {
                Destroy(m_spawnedAudioSource);
            }
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

            // Hide controller visuals since weapons are now visible
            HideControllerModels();
        }

        private void HideControllerModels()
        {
            if (m_cameraRig == null) return;

            // Hide controller models under the controller anchors
            HideControllersInTransform(m_cameraRig.leftControllerAnchor);
            HideControllersInTransform(m_cameraRig.rightControllerAnchor);
            
            // Also try the "InHand" anchors which some setups use
            var leftInHand = m_cameraRig.leftHandAnchor?.Find("LeftControllerInHandAnchor");
            var rightInHand = m_cameraRig.rightHandAnchor?.Find("RightControllerInHandAnchor");
            if (leftInHand != null) HideControllersInTransform(leftInHand);
            if (rightInHand != null) HideControllersInTransform(rightInHand);

            Debug.Log("[ShootingPlayerMotif] Controller models hidden");
        }

        private void HideControllersInTransform(Transform parent)
        {
            if (parent == null) return;

            foreach (Transform child in parent)
            {
                // Skip our spawned weapons
                if (child.name == "LeftWeapon" || child.name == "RightWeapon") continue;

                // Hide any renderers that aren't our weapons
                var renderers = child.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    // Check if this renderer is part of our weapon
                    if (m_leftWeaponInstance != null && renderer.transform.IsChildOf(m_leftWeaponInstance.transform)) continue;
                    if (m_rightWeaponInstance != null && renderer.transform.IsChildOf(m_rightWeaponInstance.transform)) continue;
                    
                    renderer.enabled = false;
                }
            }
        }

        private void ShowControllerModels()
        {
            if (m_cameraRig == null) return;

            // Show controller models under the controller anchors
            ShowControllersInTransform(m_cameraRig.leftControllerAnchor);
            ShowControllersInTransform(m_cameraRig.rightControllerAnchor);
            
            // Also try the "InHand" anchors
            var leftInHand = m_cameraRig.leftHandAnchor?.Find("LeftControllerInHandAnchor");
            var rightInHand = m_cameraRig.rightHandAnchor?.Find("RightControllerInHandAnchor");
            if (leftInHand != null) ShowControllersInTransform(leftInHand);
            if (rightInHand != null) ShowControllersInTransform(rightInHand);

            Debug.Log("[ShootingPlayerMotif] Controller models restored");
        }

        private void ShowControllersInTransform(Transform parent)
        {
            if (parent == null) return;

            foreach (Transform child in parent)
            {
                // Skip our spawned weapons (they'll be destroyed separately)
                if (child.name == "LeftWeapon" || child.name == "RightWeapon") continue;

                var renderers = child.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    renderer.enabled = true;
                }
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
            // Don't allow shooting if player is dead (only check if networked and valid)
            if (m_playerHealth != null && m_playerHealth.Object != null && m_playerHealth.Object.IsValid && m_playerHealth.IsDead)
            {
                return;
            }

            HandleShootingInput();
        }

        private void HandleShootingInput()
        {
            // Check for trigger input on either controller - support dual wielding
            var leftTrigger = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
            var rightTrigger = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);

            // Each hand has independent fire rate
            if (leftTrigger && CanFireLeft())
            {
                Debug.Log($"[ShootingPlayerMotif] Left trigger pressed! BulletPrefab: {m_bulletPrefab != null}, LeftFirePoint: {m_leftFirePoint != null}");
                var firePoint = m_leftMuzzle != null ? m_leftMuzzle : m_leftFirePoint;
                FireBullet(firePoint, m_leftFirePoint, isLeft: true);
            }

            if (rightTrigger && CanFireRight())
            {
                Debug.Log($"[ShootingPlayerMotif] Right trigger pressed! BulletPrefab: {m_bulletPrefab != null}, RightFirePoint: {m_rightFirePoint != null}");
                var firePoint = m_rightMuzzle != null ? m_rightMuzzle : m_rightFirePoint;
                FireBullet(firePoint, m_rightFirePoint, isLeft: false);
            }
        }

        private bool CanFireLeft()
        {
            return Time.time - m_lastLeftFireTime >= m_fireRate;
        }

        private bool CanFireRight()
        {
            return Time.time - m_lastRightFireTime >= m_fireRate;
        }

        private void FireBullet(Transform firePoint, Transform directionSource, bool isLeft)
        {
            if (firePoint == null || m_bulletPrefab == null)
            {
                Debug.LogWarning($"[ShootingPlayerMotif] Cannot fire! firePoint: {firePoint != null}, bulletPrefab: {m_bulletPrefab != null}");
                return;
            }

            // Update the correct hand's fire time
            if (isLeft)
                m_lastLeftFireTime = Time.time;
            else
                m_lastRightFireTime = Time.time;

            // Use direction from controller/hand, not necessarily muzzle
            var direction = directionSource != null ? directionSource.forward : firePoint.forward;

            // Check if we're connected to network
            if (m_networkRunner != null && m_networkRunner.IsRunning)
            {
                // Networked bullet spawn
                Debug.Log($"[ShootingPlayerMotif] Spawning networked bullet at {firePoint.position}");

                var bullet = m_networkRunner.Spawn(
                    m_bulletPrefab,
                    firePoint.position,
                    Quaternion.LookRotation(direction),
                    m_networkRunner.LocalPlayer
                );

                if (bullet != null)
                {
                    var bulletMotif = bullet.GetComponent<BulletMotif>();
                    if (bulletMotif != null)
                    {
                        bulletMotif.Initialize(OwnerPlayer, direction * m_fireForce, m_bulletLifetime);
                    }
                }
                else
                {
                    Debug.LogError("[ShootingPlayerMotif] Failed to spawn networked bullet!");
                }
            }
            else
            {
                // Local bullet spawn (practice mode / offline)
                Debug.Log($"[ShootingPlayerMotif] Spawning local bullet at {firePoint.position}");
                SpawnLocalBullet(firePoint.position, direction);
            }

            // Play fire sound
            PlayFireSound();
        }

        /// <summary>
        /// Spawns a local (non-networked) bullet for practice/offline mode.
        /// </summary>
        private void SpawnLocalBullet(Vector3 position, Vector3 direction)
        {
            // Instantiate the bullet prefab locally
            var bulletGO = Instantiate(m_bulletPrefab.gameObject, position, Quaternion.LookRotation(direction));
            
            // Remove NetworkObject component since we're not networked
            var networkObj = bulletGO.GetComponent<NetworkObject>();
            if (networkObj != null)
            {
                Destroy(networkObj);
            }

            // Set up rigidbody velocity
            var rb = bulletGO.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = direction * m_fireForce;
            }

            // Auto-destroy after lifetime
            Destroy(bulletGO, m_bulletLifetime);
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
