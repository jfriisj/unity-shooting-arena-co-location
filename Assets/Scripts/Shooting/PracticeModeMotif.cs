// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using System.Reflection;
using Meta.XR.Samples;
using Meta.XR.MultiplayerBlocks.Fusion;

namespace MRMotifs.SharedActivities.ShootingSample
{
    /// <summary>
    /// Enables single-player practice mode for testing shooting mechanics without networking.
    /// When enabled, spawns AI targets for the player to shoot at.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class PracticeModeMotif : MonoBehaviour
    {
        [Header("Practice Mode Settings")]
        [Tooltip("Enable practice mode on start (skips waiting for other players).")]
        [SerializeField] private bool m_enableOnStart = false;

        [Tooltip("Number of AI targets to spawn.")]
        [SerializeField] private int m_targetCount = 3;

        [Tooltip("Radius around player to spawn targets (keep small to stay within room).")]
        [SerializeField] private float m_spawnRadius = 2f;

        [Tooltip("Height of spawned targets.")]
        [SerializeField] private float m_targetHeight = 1.5f;

        [Tooltip("Prefab for practice targets (optional - uses primitives if not set).")]
        [SerializeField] private GameObject m_targetPrefab;

        [Tooltip("Time between target respawns after being destroyed.")]
        [SerializeField] private float m_respawnDelay = 3f;

        [Header("Target Movement")]
        [Tooltip("Enable target movement.")]
        [SerializeField] private bool m_movingTargets = false;

        [Tooltip("Movement speed of targets.")]
        [SerializeField] private float m_moveSpeed = 1f;

        private bool m_isPracticeMode = false;
        private readonly System.Collections.Generic.List<GameObject> m_targets = new();
        private OVRCameraRig m_cameraRig;
        private ShootingGameManagerMotif m_gameManager;
        private ShootingGameConfigMotif m_gameConfig;
        private bool m_initialized = false;
        private int m_originalMinPlayers = 2;
        private float m_initTimeout = 10f; // Fallback initialization timeout
        private Coroutine m_initCoroutine;

        private void Awake()
        {
            FusionBBEvents.OnSceneLoadDone += OnNetworkLoaded;
        }

        private void Start()
        {
            // Start fallback initialization coroutine in case the network event doesn't fire
            if (m_enableOnStart)
            {
                m_initCoroutine = StartCoroutine(FallbackInitialization());
            }
        }

        private System.Collections.IEnumerator FallbackInitialization()
        {
            float elapsed = 0f;
            Debug.Log("[PracticeModeMotif] Starting fallback initialization...");
            
            // Wait for dependencies with timeout
            while (!m_initialized && elapsed < m_initTimeout)
            {
                elapsed += 0.5f;
                yield return new WaitForSeconds(0.5f);
                
                // Try to find dependencies
                if (m_cameraRig == null)
                    m_cameraRig = FindAnyObjectByType<OVRCameraRig>();
                if (m_gameManager == null)
                    m_gameManager = FindAnyObjectByType<ShootingGameManagerMotif>();
                if (m_gameConfig == null)
                    m_gameConfig = FindAnyObjectByType<ShootingGameConfigMotif>();
                
                // If we found a camera rig, we can proceed even without network
                if (m_cameraRig != null)
                {
                    Debug.Log("[PracticeModeMotif] Found camera rig, initializing practice mode via fallback");
                    InitializePracticeMode();
                    yield break;
                }
            }
            
            if (!m_initialized)
            {
                Debug.LogWarning("[PracticeModeMotif] Fallback initialization timed out - could not find OVRCameraRig");
            }
        }

        private void OnDestroy()
        {
            FusionBBEvents.OnSceneLoadDone -= OnNetworkLoaded;
            if (m_initCoroutine != null)
                StopCoroutine(m_initCoroutine);
            CleanupTargets();
        }

        private void OnNetworkLoaded(NetworkRunner runner)
        {
            // Stop fallback coroutine if network event fires
            if (m_initCoroutine != null)
            {
                StopCoroutine(m_initCoroutine);
                m_initCoroutine = null;
            }
            
            InitializePracticeMode();
        }

        private void InitializePracticeMode()
        {
            m_cameraRig = FindAnyObjectByType<OVRCameraRig>();
            m_gameManager = FindAnyObjectByType<ShootingGameManagerMotif>();
            m_gameConfig = FindAnyObjectByType<ShootingGameConfigMotif>();
            m_initialized = true;

            // Store original min players setting
            if (m_gameManager != null)
            {
                var field = typeof(ShootingGameManagerMotif).GetField("m_minPlayersToStart", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    m_originalMinPlayers = (int)field.GetValue(m_gameManager);
                }
            }

            if (m_enableOnStart)
            {
                EnablePracticeMode();
            }
        }

        private void Update()
        {
            if (!m_isPracticeMode || !m_initialized)
            {
                return;
            }

            UpdateTargets();

            // Check for practice mode toggle (A + B buttons)
            bool aButton = OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch);
            bool bButton = OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch);
            
            if (aButton && bButton)
            {
                DisablePracticeMode();
            }
        }

        /// <summary>
        /// Enable practice mode, spawning targets for the player to shoot.
        /// Also sets minPlayersToStart to 1 to allow single-player gameplay.
        /// </summary>
        public void EnablePracticeMode()
        {
            if (m_isPracticeMode)
            {
                Debug.Log("[PracticeModeMotif] Practice mode already enabled");
                return;
            }

            m_isPracticeMode = true;
            Debug.Log($"[PracticeModeMotif] Practice mode enabled - Camera: {m_cameraRig != null}, GameManager: {m_gameManager != null}, Config: {m_gameConfig != null}");

            // Set minPlayersToStart to 1 for single-player
            ConfigureForSinglePlayer(true);

            SpawnTargets();
            
            Debug.Log($"[PracticeModeMotif] Spawned {m_targets.Count} targets");
        }

        /// <summary>
        /// Disable practice mode and clean up targets.
        /// Restores original minPlayersToStart setting.
        /// </summary>
        public void DisablePracticeMode()
        {
            if (!m_isPracticeMode)
            {
                return;
            }

            m_isPracticeMode = false;
            Debug.Log("[PracticeModeMotif] Practice mode disabled");

            // Restore original minPlayersToStart
            ConfigureForSinglePlayer(false);

            CleanupTargets();
        }

        private void SpawnTargets()
        {
            if (m_cameraRig == null)
            {
                Debug.LogWarning("[PracticeModeMotif] Cannot spawn targets - no camera rig found, will retry finding it");
                m_cameraRig = FindAnyObjectByType<OVRCameraRig>();
                if (m_cameraRig == null)
                {
                    Debug.LogError("[PracticeModeMotif] Still cannot find OVRCameraRig!");
                    return;
                }
            }

            var playerPosition = m_cameraRig.centerEyeAnchor.position;
            Debug.Log($"[PracticeModeMotif] Spawning {m_targetCount} targets at radius {m_spawnRadius}m around player at {playerPosition}");

            for (var i = 0; i < m_targetCount; i++)
            {
                // Calculate position around player
                var angle = (360f / m_targetCount) * i;
                var targetPosition = FindValidSpawnPosition(playerPosition, angle, m_spawnRadius);
                
                var target = CreateTarget(targetPosition, i);
                m_targets.Add(target);
                Debug.Log($"[PracticeModeMotif] Created target {i} at {targetPosition}");
            }
        }

        /// <summary>
        /// Find a valid spawn position within the room, using raycasting to avoid walls.
        /// </summary>
        private Vector3 FindValidSpawnPosition(Vector3 playerPosition, float angle, float maxRadius)
        {
            var radians = angle * Mathf.Deg2Rad;
            var direction = new Vector3(Mathf.Sin(radians), 0, Mathf.Cos(radians));
            
            // Try to find a valid position by raycasting from player outward
            // Start from player and move outward, checking for walls
            var bestPosition = playerPosition + direction * 1.5f; // Default: 1.5m away
            bestPosition.y = playerPosition.y - 1f + m_targetHeight; // Approximate floor + target height
            
            // Raycast to find walls - if we hit something, spawn in front of it
            if (Physics.Raycast(playerPosition, direction, out var hit, maxRadius))
            {
                // Spawn 0.5m in front of the wall
                var distanceToWall = hit.distance;
                var safeDistance = Mathf.Max(1f, distanceToWall - 0.5f);
                bestPosition = playerPosition + direction * safeDistance;
                bestPosition.y = playerPosition.y - 1f + m_targetHeight;
                Debug.Log($"[PracticeModeMotif] Wall detected at {distanceToWall}m, spawning at {safeDistance}m");
            }
            else
            {
                // No wall hit, use the requested radius (clamped to reasonable value)
                var clampedRadius = Mathf.Min(maxRadius, 3f); // Max 3m to stay in typical rooms
                bestPosition = playerPosition + direction * clampedRadius;
                bestPosition.y = playerPosition.y - 1f + m_targetHeight;
            }
            
            return bestPosition;
        }

        private GameObject CreateTarget(Vector3 position, int index)
        {
            GameObject target;

            if (m_targetPrefab != null)
            {
                target = Instantiate(m_targetPrefab, position, Quaternion.identity);
            }
            else
            {
                // Create default target (capsule)
                target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                target.transform.position = position;
                target.transform.localScale = new Vector3(0.5f, 1f, 0.5f);

                // Add bright red material
                var renderer = target.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.red;
                }
            }

            target.name = $"PracticeTarget_{index}";

            // Add collider if missing
            if (target.GetComponent<Collider>() == null)
            {
                target.AddComponent<CapsuleCollider>();
            }

            // Add rigidbody to make it destroyable by bullets
            var rb = target.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = target.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;

            // Add target component for hit detection
            var targetComponent = target.AddComponent<PracticeTarget>();
            targetComponent.Initialize(this, m_respawnDelay);

            return target;
        }

        private void UpdateTargets()
        {
            if (!m_movingTargets || m_cameraRig == null)
            {
                return;
            }

            var playerPosition = m_cameraRig.centerEyeAnchor.position;

            foreach (var target in m_targets)
            {
                if (target == null)
                {
                    continue;
                }

                // Simple orbit movement around player
                var direction = (target.transform.position - playerPosition).normalized;
                direction = Quaternion.Euler(0, m_moveSpeed * Time.deltaTime * 10f, 0) * direction;
                target.transform.position = playerPosition + direction * m_spawnRadius;
                target.transform.LookAt(playerPosition);
            }
        }

        /// <summary>
        /// Configures the game for single-player practice mode.
        /// Sets minPlayersToStart to 1 when enabling, restores original value when disabling.
        /// </summary>
        private void ConfigureForSinglePlayer(bool enable)
        {
            var minPlayers = enable ? 1 : m_originalMinPlayers;

            // Update ShootingGameManagerMotif
            if (m_gameManager != null)
            {
                var field = typeof(ShootingGameManagerMotif).GetField("m_minPlayersToStart", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(m_gameManager, minPlayers);
                    Debug.Log($"[PracticeModeMotif] Set GameManager minPlayersToStart = {minPlayers}");
                }
            }

            // Update ShootingGameConfigMotif if present
            if (m_gameConfig != null)
            {
                m_gameConfig.minPlayersToStart = minPlayers;
                Debug.Log($"[PracticeModeMotif] Set GameConfig minPlayersToStart = {minPlayers}");
            }
        }

        private void CleanupTargets()
        {
            foreach (var target in m_targets)
            {
                if (target != null)
                {
                    Destroy(target);
                }
            }
            m_targets.Clear();
        }

        /// <summary>
        /// Called when a target is hit to schedule respawn.
        /// </summary>
        public void OnTargetHit(PracticeTarget target)
        {
            Debug.Log($"[PracticeModeMotif] Target hit: {target.name}");
        }

        /// <summary>
        /// Respawn a target at a new position.
        /// </summary>
        public void RespawnTarget(PracticeTarget oldTarget)
        {
            if (!m_isPracticeMode || m_cameraRig == null)
            {
                return;
            }

            // Find index and respawn at a random angle using room-aware positioning
            var index = m_targets.IndexOf(oldTarget.gameObject);
            if (index >= 0)
            {
                _ = m_targets.Remove(oldTarget.gameObject);
                Destroy(oldTarget.gameObject);

                var playerPosition = m_cameraRig.centerEyeAnchor.position;
                var angle = Random.Range(0f, 360f);
                var targetPosition = FindValidSpawnPosition(playerPosition, angle, m_spawnRadius);

                var newTarget = CreateTarget(targetPosition, index);
                m_targets.Add(newTarget);
            }
        }
    }

    /// <summary>
    /// Component attached to practice targets for hit detection.
    /// </summary>
    public class PracticeTarget : MonoBehaviour
    {
        private PracticeModeMotif m_practiceMode;
        private float m_respawnDelay;
        private int m_hitCount = 0;
        private const int HITS_TO_DESTROY = 3;

        public void Initialize(PracticeModeMotif practiceMode, float respawnDelay)
        {
            m_practiceMode = practiceMode;
            m_respawnDelay = respawnDelay;
        }

        private void OnCollisionEnter(Collision collision)
        {
            ProcessHit(collision.gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            ProcessHit(other.gameObject);
        }

        private void ProcessHit(GameObject hitObject)
        {
            // Check if hit by a bullet
            var bullet = hitObject.GetComponent<BulletMotif>();
            if (bullet == null)
            {
                return;
            }

            m_hitCount++;
            m_practiceMode?.OnTargetHit(this);

            // Flash effect
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                StartCoroutine(FlashEffect(renderer));
            }

            if (m_hitCount >= HITS_TO_DESTROY)
            {
                // Schedule respawn
                Invoke(nameof(TriggerRespawn), m_respawnDelay);
                
                // Disable immediately
                gameObject.SetActive(false);
            }
        }

        private System.Collections.IEnumerator FlashEffect(Renderer renderer)
        {
            var originalColor = renderer.material.color;
            renderer.material.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            renderer.material.color = originalColor;
        }

        private void TriggerRespawn()
        {
            m_practiceMode?.RespawnTarget(this);
        }
    }
}
#endif
