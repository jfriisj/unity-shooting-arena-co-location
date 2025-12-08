// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using Meta.XR.Samples;

namespace MRMotifs.Shooting
{
    /// <summary>
    /// Allows the host to spawn cover objects in the arena.
    /// Point with controller and press grip to place cover.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class CoverSpawnerMotif : MonoBehaviour
    {
        [Header("Spawning Settings")]
        [Tooltip("The networked cover prefab to spawn.")]
        [SerializeField] private NetworkedCoverMotif m_coverPrefab;

        [Tooltip("Maximum number of cover objects allowed.")]
        [SerializeField] private int m_maxCoverCount = 10;

        [Tooltip("Distance to spawn cover from controller.")]
        [SerializeField] private float m_spawnDistance = 2f;

        [Header("Preview")]
        [Tooltip("Show a preview of where cover will be placed.")]
        [SerializeField] private bool m_showPreview = true;

        [Tooltip("Preview material (semi-transparent).")]
        [SerializeField] private Material m_previewMaterial;

        [Header("Audio")]
        [Tooltip("Sound played when placing cover.")]
        [SerializeField] private AudioClip m_placeSound;

        private NetworkRunner m_runner;
        private int m_currentCoverCount = 0;
        private GameObject m_previewObject;
        private bool m_isSpawningEnabled = false;
        private OVRCameraRig m_cameraRig;

        private void Start()
        {
            m_cameraRig = FindAnyObjectByType<OVRCameraRig>();

            // Create preview object
            if (m_showPreview)
            {
                CreatePreviewObject();
            }

            // Load place sound from Resources
            if (m_placeSound == null)
            {
                m_placeSound = Resources.Load<AudioClip>("Audio/RoundStart");
            }
        }

        private void Update()
        {
            // Only host can spawn cover, and only when enabled
            m_runner = FindAnyObjectByType<NetworkRunner>();
            if (m_runner == null || !m_runner.IsServer)
            {
                if (m_previewObject != null)
                {
                    m_previewObject.SetActive(false);
                }
                return;
            }

            // Toggle spawning mode with Y button (left controller)
            if (OVRInput.GetDown(OVRInput.Button.Four)) // Y button
            {
                m_isSpawningEnabled = !m_isSpawningEnabled;
                Debug.Log($"[CoverSpawner] Spawning mode: {(m_isSpawningEnabled ? "ENABLED" : "DISABLED")}");
            }

            if (!m_isSpawningEnabled)
            {
                if (m_previewObject != null)
                {
                    m_previewObject.SetActive(false);
                }
                return;
            }

            // Get right controller position and forward
            var rightHand = m_cameraRig?.rightControllerAnchor;
            if (rightHand == null) return;

            Vector3 spawnPos = GetSpawnPosition(rightHand);
            Quaternion spawnRot = GetSpawnRotation(rightHand);

            // Update preview
            if (m_previewObject != null && m_showPreview)
            {
                m_previewObject.SetActive(true);
                m_previewObject.transform.position = spawnPos;
                m_previewObject.transform.rotation = spawnRot;

                // Change preview color based on whether we can spawn
                var renderer = m_previewObject.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.material.color = m_currentCoverCount < m_maxCoverCount
                        ? new Color(0f, 1f, 0f, 0.4f)  // Green = can spawn
                        : new Color(1f, 0f, 0f, 0.4f); // Red = max reached
                }
            }

            // Spawn cover with right grip button
            if (OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger) && m_currentCoverCount < m_maxCoverCount)
            {
                SpawnCover(spawnPos, spawnRot);
            }

            // Delete last cover with left grip (while in spawn mode)
            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger) && m_currentCoverCount > 0)
            {
                DeleteLastCover();
            }
        }

        private Vector3 GetSpawnPosition(Transform controller)
        {
            // Raycast to find floor or use fixed distance
            Vector3 origin = controller.position;
            Vector3 direction = controller.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, 10f))
            {
                // Place on the surface
                return hit.point;
            }

            // Default: place at fixed distance on floor level
            Vector3 pos = origin + direction * m_spawnDistance;
            pos.y = 0f; // Floor level
            return pos;
        }

        private Quaternion GetSpawnRotation(Transform controller)
        {
            // Face the player (rotate to face controller)
            Vector3 lookDir = controller.position - GetSpawnPosition(controller);
            lookDir.y = 0; // Keep upright

            if (lookDir.sqrMagnitude > 0.01f)
            {
                return Quaternion.LookRotation(lookDir);
            }

            return Quaternion.identity;
        }

        private void SpawnCover(Vector3 position, Quaternion rotation)
        {
            if (m_coverPrefab == null)
            {
                Debug.LogError("[CoverSpawner] Cover prefab not assigned!");
                return;
            }

            // Spawn networked cover
            var cover = m_runner.Spawn(m_coverPrefab, position, rotation);
            if (cover != null)
            {
                cover.Initialize(position, rotation);
                m_currentCoverCount++;

                // Play sound
                if (m_placeSound != null)
                {
                    AudioSource.PlayClipAtPoint(m_placeSound, position, 0.5f);
                }

                Debug.Log($"[CoverSpawner] Spawned cover {m_currentCoverCount}/{m_maxCoverCount}");
            }
        }

        private void DeleteLastCover()
        {
            // Find and delete the most recent cover
            var covers = FindObjectsByType<NetworkedCoverMotif>(FindObjectsSortMode.None);
            if (covers.Length > 0)
            {
                var lastCover = covers[covers.Length - 1];
                if (lastCover.Object != null && lastCover.Object.IsValid)
                {
                    m_runner.Despawn(lastCover.Object);
                    m_currentCoverCount = Mathf.Max(0, m_currentCoverCount - 1);
                    Debug.Log($"[CoverSpawner] Deleted cover. Remaining: {m_currentCoverCount}");
                }
            }
        }

        private void CreatePreviewObject()
        {
            m_previewObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_previewObject.name = "CoverPreview";
            m_previewObject.transform.localScale = new Vector3(1f, 1.5f, 0.3f);

            // Remove collider from preview
            var collider = m_previewObject.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            // Create semi-transparent material
            var renderer = m_previewObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 0);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
                mat.color = new Color(0f, 1f, 0f, 0.4f);
                renderer.material = mat;
            }

            m_previewObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (m_previewObject != null)
            {
                Destroy(m_previewObject);
            }
        }

        /// <summary>
        /// Clear all cover objects (for game reset).
        /// </summary>
        public void ClearAllCover()
        {
            if (m_runner == null || !m_runner.IsServer) return;

            var covers = FindObjectsByType<NetworkedCoverMotif>(FindObjectsSortMode.None);
            foreach (var cover in covers)
            {
                if (cover.Object != null && cover.Object.IsValid)
                {
                    m_runner.Despawn(cover.Object);
                }
            }
            m_currentCoverCount = 0;
            Debug.Log("[CoverSpawner] All cover cleared");
        }
    }
}
#endif
