// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using Meta.XR.Samples;
using Meta.XR.MultiplayerBlocks.Fusion;
using MRMotifs.Shared;

namespace MRMotifs.Shooting
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
        public static float ConfigBulletSpeed = 60f;
        public static float ConfigBulletLifetime = 5f;

        [Header("Projectile Settings")]
        [Tooltip("The networked bullet prefab to spawn when shooting.")]
        [SerializeField] private NetworkObject m_bulletPrefab;

        [Tooltip("Force applied to the bullet when fired.")]
        [SerializeField] private float m_fireForce = 60f;

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

        [Header("Muzzle Flash")]
        [Tooltip("Muzzle flash prefab to spawn when firing.")]
        [SerializeField] private GameObject m_muzzleFlashPrefab;

        [Tooltip("Duration to display muzzle flash.")]
        [SerializeField] private float m_muzzleFlashDuration = 0.05f;

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

            // Require critical dependencies - no fallbacks, expose missing config
            m_cameraRig = DebugLogger.RequireNotNull(
                FindAnyObjectByType<OVRCameraRig>(), 
                "PLAYER", "OVRCameraRig in scene", this);
            
            m_networkRunner = FindAnyObjectByType<NetworkRunner>();
            if (m_networkRunner != null && m_networkRunner.IsRunning)
            {
                OwnerPlayer = m_networkRunner.LocalPlayer;
                DebugLogger.Player($"NetworkRunner found | player={OwnerPlayer}", this);
            }
            else
            {
                DebugLogger.Player("No NetworkRunner - running in local/practice mode", this);
            }
            
            m_playerHealth = GetComponent<PlayerHealthMotif>();

            // Ensure AudioSource exists
            if (m_audioSource == null)
            {
                m_audioSource = GetComponent<AudioSource>();
                if (m_audioSource == null)
                {
                    m_audioSource = gameObject.AddComponent<AudioSource>();
                    m_audioSource.spatialBlend = 1.0f; // 3D sound
                }
            }

            // Require serialized fields - these must be assigned in Inspector
            DebugLogger.RequireSerializedField(m_bulletPrefab, "m_bulletPrefab", this);
            // m_audioSource is now auto-created if missing
            // m_fireSound might be set via SetFireSound later, so we warn but don't require strictly if we expect runtime setup
            if (m_fireSound == null)
            {
                DebugLogger.Warning("PLAYER", "m_fireSound is null. Sound will not play unless set via SetFireSound.");
            }

            // Fire points from camera rig
            m_leftFirePoint = m_cameraRig.leftControllerAnchor;
            m_rightFirePoint = m_cameraRig.rightControllerAnchor;
            
            DebugLogger.Require(m_leftFirePoint != null, "PLAYER", "leftControllerAnchor exists on OVRCameraRig", this);
            DebugLogger.Require(m_rightFirePoint != null, "PLAYER", "rightControllerAnchor exists on OVRCameraRig", this);

            // Spawn weapon models
            SpawnWeaponModels();
            
            DebugLogger.Player($"Started | fireRate={m_fireRate:F2}s bulletSpeed={m_fireForce:F1} lifetime={m_bulletLifetime:F1}s", this);
        }

        private void OnDestroy()
        {
            // Restore controller visibility when this component is destroyed
            ShowControllerModels();
        }

        private void SpawnWeaponModels()
        {
            if (m_weaponPrefab == null)
            {
                DebugLogger.Warning("PLAYER", "No weapon prefab assigned");
                return;
            }

            // Check if weapons already exist to prevent duplication
            if (m_rightWeaponInstance != null || m_leftWeaponInstance != null)
            {
                DebugLogger.Player("Weapons already spawned, skipping duplication.", this);
                return;
            }

            DebugLogger.Player($"Spawning weapons | prefab={m_weaponPrefab.name} rightPoint={m_rightFirePoint != null} leftPoint={m_leftFirePoint != null}", this);

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
                DebugLogger.Player($"Right weapon spawned | parent={m_rightFirePoint.name}", this);
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
                DebugLogger.Player($"Left weapon spawned | parent={m_leftFirePoint.name}", this);
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

            DebugLogger.Player("Controller models hidden", this);
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

            DebugLogger.Player("Controller models restored", this);
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
            // Use GetDown for semi-automatic fire (must release and press again)
            var leftTrigger = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
            var rightTrigger = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);

            // Each hand has independent fire rate
            if (leftTrigger && CanFireLeft())
            {
                DebugLogger.Player($"Fire LEFT | ready={m_bulletPrefab != null && m_leftFirePoint != null}", this);
                var firePoint = m_leftMuzzle != null ? m_leftMuzzle : m_leftFirePoint;
                FireBullet(firePoint, m_leftFirePoint, isLeft: true);
            }

            if (rightTrigger && CanFireRight())
            {
                DebugLogger.Player($"Fire RIGHT | ready={m_bulletPrefab != null && m_rightFirePoint != null}", this);
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
            // These should never be null if Start() passed - throw to expose bugs
            DebugLogger.RequireNotNull(firePoint, "PLAYER", $"firePoint ({(isLeft ? "left" : "right")})", this);
            DebugLogger.RequireNotNull(m_bulletPrefab, "PLAYER", "m_bulletPrefab", this);

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
                DebugLogger.Player($"Spawn NETWORKED bullet | pos={firePoint.position:F2} dir={direction:F2}", this);

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
                    DebugLogger.Error("PLAYER", "Failed to spawn networked bullet!");
                }
            }
            else
            {
                // Local bullet spawn (practice mode / offline)
                DebugLogger.Player($"Spawn LOCAL bullet | pos={firePoint.position:F2} dir={direction:F2}", this);
                SpawnLocalBullet(firePoint.position, direction);
            }

            // Play fire sound
            PlayFireSound();

            // Spawn muzzle flash effect
            SpawnMuzzleFlash(firePoint);
        }

        /// <summary>
        /// Spawns a muzzle flash effect at the given fire point.
        /// </summary>
        private void SpawnMuzzleFlash(Transform firePoint)
        {
            if (m_muzzleFlashPrefab == null || firePoint == null)
            {
                return;
            }

            var muzzleFlash = Instantiate(m_muzzleFlashPrefab, firePoint.position, firePoint.rotation);
            muzzleFlash.transform.SetParent(firePoint, true);
            Destroy(muzzleFlash, m_muzzleFlashDuration);
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

            DebugLogger.Player($"RespawnWeaponModels | cameraRig={m_cameraRig != null} weaponPrefab={m_weaponPrefab != null} rightFP={m_rightFirePoint != null}", this);

            // Spawn new weapons
            SpawnWeaponModels();
        }

        /// <summary>
        /// Sets the fire sound clip at runtime.
        /// </summary>
        public void SetFireSound(AudioClip clip)
        {
            m_fireSound = clip;
        }

        /// <summary>
        /// Sets the muzzle flash prefab at runtime.
        /// </summary>
        public void SetMuzzleFlashPrefab(GameObject prefab)
        {
            m_muzzleFlashPrefab = prefab;
        }
    }
}
#endif
