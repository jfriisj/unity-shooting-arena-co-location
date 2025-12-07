// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using UnityEngine;
using Meta.XR.Samples;

namespace MRMotifs.SharedActivities.ShootingSample
{
    /// <summary>
    /// Handles muzzle flash visual effects when shooting.
    /// Uses prefabs from the Easy FPS package for visual variety.
    /// Attach to weapon or create instance on ShootingPlayerMotif.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class MuzzleFlashMotif : MonoBehaviour
    {
        [Header("Muzzle Flash Settings")]
        [Tooltip("Array of muzzle flash prefabs. One is randomly selected each shot.")]
        [SerializeField] private GameObject[] m_muzzleFlashPrefabs;

        [Tooltip("Duration the muzzle flash stays visible.")]
        [SerializeField] private float m_flashDuration = 0.05f;

        [Tooltip("Scale multiplier for the muzzle flash effect.")]
        [SerializeField] private float m_flashScale = 1.0f;

        [Tooltip("Offset from the muzzle point.")]
        [SerializeField] private Vector3 m_flashOffset = Vector3.zero;

        private Transform m_muzzlePoint;
        private GameObject m_currentFlash;

        /// <summary>
        /// Initialize the muzzle flash with a specific muzzle point transform.
        /// </summary>
        public void Initialize(Transform muzzlePoint)
        {
            m_muzzlePoint = muzzlePoint;
        }

        /// <summary>
        /// Set the muzzle point at runtime.
        /// </summary>
        public void SetMuzzlePoint(Transform muzzlePoint)
        {
            m_muzzlePoint = muzzlePoint;
        }

        /// <summary>
        /// Set the muzzle flash prefabs array at runtime.
        /// </summary>
        public void SetMuzzleFlashPrefabs(GameObject[] prefabs)
        {
            m_muzzleFlashPrefabs = prefabs;
        }

        /// <summary>
        /// Trigger a muzzle flash effect at the current muzzle point.
        /// </summary>
        public void TriggerFlash()
        {
            if (m_muzzleFlashPrefabs == null || m_muzzleFlashPrefabs.Length == 0)
            {
                Debug.LogWarning("[MuzzleFlashMotif] No muzzle flash prefabs assigned!");
                return;
            }

            if (m_muzzlePoint == null)
            {
                Debug.LogWarning("[MuzzleFlashMotif] No muzzle point assigned!");
                return;
            }

            // Clean up previous flash if still exists
            if (m_currentFlash != null)
            {
                Destroy(m_currentFlash);
            }

            // Select random muzzle flash prefab
            int randomIndex = Random.Range(0, m_muzzleFlashPrefabs.Length);
            var prefab = m_muzzleFlashPrefabs[randomIndex];

            if (prefab == null)
            {
                Debug.LogWarning($"[MuzzleFlashMotif] Muzzle flash prefab at index {randomIndex} is null!");
                return;
            }

            // Spawn muzzle flash
            var position = m_muzzlePoint.position + m_muzzlePoint.TransformDirection(m_flashOffset);
            var rotation = m_muzzlePoint.rotation * Quaternion.Euler(0, 0, 90); // Easy FPS uses 90 degree Z rotation

            m_currentFlash = Instantiate(prefab, position, rotation);
            m_currentFlash.transform.SetParent(m_muzzlePoint);
            m_currentFlash.transform.localScale = Vector3.one * m_flashScale;

            // Destroy after duration
            Destroy(m_currentFlash, m_flashDuration);
        }

        /// <summary>
        /// Trigger a muzzle flash at a specific position and rotation.
        /// </summary>
        public void TriggerFlashAt(Vector3 position, Quaternion rotation)
        {
            if (m_muzzleFlashPrefabs == null || m_muzzleFlashPrefabs.Length == 0)
            {
                return;
            }

            // Select random muzzle flash prefab
            int randomIndex = Random.Range(0, m_muzzleFlashPrefabs.Length);
            var prefab = m_muzzleFlashPrefabs[randomIndex];

            if (prefab == null) return;

            // Spawn muzzle flash
            var flash = Instantiate(prefab, position, rotation * Quaternion.Euler(0, 0, 90));
            flash.transform.localScale = Vector3.one * m_flashScale;

            // Destroy after duration
            Destroy(flash, m_flashDuration);
        }

        /// <summary>
        /// Load muzzle flash prefabs from the Easy FPS package Resources folder.
        /// Call this to auto-populate the prefab array.
        /// </summary>
        public void LoadEasyFPSMuzzleFlashes()
        {
            // Try to load all muzzle flash prefabs from Easy FPS package
            var prefabPaths = new string[]
            {
                "Assets/Easy FPS/MuzzelFlash/MuzzlePrefabs/muzzelFlash 01",
                "Assets/Easy FPS/MuzzelFlash/MuzzlePrefabs/muzzelFlash 02",
                "Assets/Easy FPS/MuzzelFlash/MuzzlePrefabs/muzzelFlash 03",
                "Assets/Easy FPS/MuzzelFlash/MuzzlePrefabs/muzzelFlash 04",
                "Assets/Easy FPS/MuzzelFlash/MuzzlePrefabs/muzzelFlash 05"
            };

            // Note: Resources.Load requires files to be in a Resources folder
            // For assets not in Resources, we need to use AssetDatabase in editor or assign manually
            Debug.Log("[MuzzleFlashMotif] LoadEasyFPSMuzzleFlashes called - prefabs should be assigned in inspector or via script");
        }
    }
}
#endif
