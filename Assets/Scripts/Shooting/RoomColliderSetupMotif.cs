// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using Meta.XR.MRUtilityKit;
using Meta.XR.Samples;

namespace MRMotifs.SharedActivities.ShootingSample
{
    /// <summary>
    /// Ensures room mesh colliders are set up for bullet collision detection.
    /// This is a fallback/standalone script that works independently of RoomSharingMotif.
    /// Also provides optional debug visualization of the room mesh.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class RoomColliderSetupMotif : MonoBehaviour
    {
        [Header("Collider Settings")]
        [Tooltip("Automatically set up room colliders when MRUK loads a scene.")]
        [SerializeField] private bool m_autoSetupColliders = true;

        [Tooltip("Layer name for room mesh colliders.")]
        [SerializeField] private string m_roomMeshLayerName = "RoomMesh";

        [Tooltip("Tag for room mesh objects.")]
        [SerializeField] private string m_roomMeshTag = "RoomMesh";

        [Header("Debug Visualization")]
        [Tooltip("Enable debug visualization of room colliders.")]
        [SerializeField] private bool m_showDebugVisualization = true;

        [Tooltip("Color for debug wireframe.")]
        [SerializeField] private Color m_debugColor = new Color(0f, 1f, 1f, 0.3f);

        [Tooltip("Material for debug visualization (optional, will create default if null).")]
        [SerializeField] private Material m_debugMaterial;

        private bool m_collidersSetup = false;
        private int m_roomMeshLayer = -1;

        private void Start()
        {
            Debug.Log("[RoomColliderSetup] ========== START ==========" );
            
            m_roomMeshLayer = LayerMask.NameToLayer(m_roomMeshLayerName);
            Debug.Log($"[RoomColliderSetup] Layer '{m_roomMeshLayerName}' = {m_roomMeshLayer}");
            
            if (m_roomMeshLayer < 0)
            {
                Debug.LogWarning($"[RoomColliderSetup] Layer '{m_roomMeshLayerName}' not found. Using Default layer.");
                m_roomMeshLayer = 0;
            }

            // Subscribe to MRUK scene loaded event
            Debug.Log($"[RoomColliderSetup] MRUK.Instance: {(MRUK.Instance != null ? "exists" : "NULL")}");
            
            if (MRUK.Instance != null)
            {
                MRUK.Instance.SceneLoadedEvent.AddListener(OnSceneLoaded);
                Debug.Log("[RoomColliderSetup] Subscribed to SceneLoadedEvent");
                
                // Also check if scene is already loaded
                var currentRoom = MRUK.Instance.GetCurrentRoom();
                Debug.Log($"[RoomColliderSetup] Current room: {(currentRoom != null ? "exists" : "NULL")}");
                
                if (currentRoom != null)
                {
                    SetupRoomColliders();
                }
            }
            else
            {
                Debug.Log("[RoomColliderSetup] MRUK not ready, starting coroutine to wait...");
                // Wait for MRUK to be available
                StartCoroutine(WaitForMRUK());
            }
        }

        private System.Collections.IEnumerator WaitForMRUK()
        {
            float timeout = 30f;
            float elapsed = 0f;

            while (MRUK.Instance == null && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }

            if (MRUK.Instance != null)
            {
                MRUK.Instance.SceneLoadedEvent.AddListener(OnSceneLoaded);
                
                // Check if room already exists
                if (MRUK.Instance.GetCurrentRoom() != null)
                {
                    SetupRoomColliders();
                }
            }
            else
            {
                Debug.LogError("[RoomColliderSetup] MRUK not available after timeout.");
            }
        }

        private void OnSceneLoaded()
        {
            Debug.Log("[RoomColliderSetup] MRUK scene loaded event received.");
            SetupRoomColliders();
        }

        private void OnDestroy()
        {
            if (MRUK.Instance != null)
            {
                MRUK.Instance.SceneLoadedEvent.RemoveListener(OnSceneLoaded);
            }
        }

        /// <summary>
        /// Periodically attempt to set up colliders every 2 seconds for the first 30 seconds.
        /// This ensures colliders get set up even if there are timing issues with MRUK loading.
        /// </summary>
        private System.Collections.IEnumerator PeriodicRetryColliderSetup()
        {
            float maxRetryTime = 30f;
            float elapsed = 0f;
            float retryInterval = 2f;

            while (elapsed < maxRetryTime)
            {
                yield return new WaitForSeconds(retryInterval);
                elapsed += retryInterval;

                if (!m_collidersSetup && MRUK.Instance != null && MRUK.Instance.GetCurrentRoom() != null)
                {
                    Debug.Log($"[RoomColliderSetup] Periodic retry attempt at {elapsed}s");
                    SetupRoomColliders();
                    
                    if (m_collidersSetup)
                    {
                        Debug.Log("[RoomColliderSetup] Periodic retry succeeded!");
                        yield break; // Exit if successful
                    }
                }
            }

            if (!m_collidersSetup)
            {
                Debug.LogWarning("[RoomColliderSetup] Failed to set up colliders after periodic retries.");
            }
        }

        /// <summary>
        /// Set up colliders on all room meshes.
        /// </summary>
        public void SetupRoomColliders()
        {
            Debug.Log("[RoomColliderSetup] ========== SETUP COLLIDERS ==========" );
            Debug.Log($"[RoomColliderSetup] m_autoSetupColliders: {m_autoSetupColliders}");
            Debug.Log($"[RoomColliderSetup] m_collidersSetup: {m_collidersSetup}");
            
            if (!m_autoSetupColliders)
            {
                Debug.Log("[RoomColliderSetup] Auto setup disabled, skipping.");
                return;
            }

            if (m_collidersSetup)
            {
                Debug.Log("[RoomColliderSetup] Colliders already set up.");
                return;
            }

            var mruk = MRUK.Instance;
            if (mruk == null)
            {
                Debug.LogWarning("[RoomColliderSetup] MRUK not available.");
                return;
            }
            
            Debug.Log($"[RoomColliderSetup] MRUK.Rooms count: {mruk.Rooms?.Count ?? 0}");

            int totalColliders = 0;

            foreach (var room in mruk.Rooms)
            {
                if (room == null) continue;
                
                Debug.Log($"[RoomColliderSetup] Processing room: {room.Anchor.Uuid}");

                // Set up GlobalMesh collider
                var globalMeshAnchor = room.GlobalMeshAnchor;
                Debug.Log($"[RoomColliderSetup] Room.GlobalMeshAnchor: {(globalMeshAnchor != null ? "exists" : "NULL")}");
                
                if (globalMeshAnchor != null)
                {
                    var meshFilters = globalMeshAnchor.GetComponentsInChildren<MeshFilter>(true);
                    Debug.Log($"[RoomColliderSetup] MeshFilters found in GlobalMeshAnchor: {meshFilters.Length}");
                    
                    foreach (var meshFilter in meshFilters)
                    {
                        if (meshFilter.sharedMesh == null)
                        {
                            Debug.Log($"[RoomColliderSetup] Skipping {meshFilter.gameObject.name} - no sharedMesh");
                            continue;
                        }

                        // Add or get MeshCollider
                        var meshCollider = meshFilter.gameObject.GetComponent<MeshCollider>();
                        if (meshCollider == null)
                        {
                            meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
                        }
                        meshCollider.sharedMesh = meshFilter.sharedMesh;
                        meshCollider.enabled = true;
                        meshCollider.convex = false; // Non-convex for accurate collision

                        // Set layer and tag
                        meshFilter.gameObject.layer = m_roomMeshLayer;
                        if (!string.IsNullOrEmpty(m_roomMeshTag))
                        {
                            try { meshFilter.gameObject.tag = m_roomMeshTag; }
                            catch { /* Tag might not exist */ }
                        }

                        totalColliders++;

                        // Add debug visualization
                        if (m_showDebugVisualization)
                        {
                            EnableDebugVisualization(meshFilter);
                        }

                        Debug.Log($"[RoomColliderSetup] GlobalMesh collider added: {meshFilter.gameObject.name}, " +
                                  $"vertices: {meshFilter.sharedMesh.vertexCount}, layer: {m_roomMeshLayer}");
                    }
                }

                // Set up anchor colliders (walls, floor, ceiling)
                foreach (var anchor in room.Anchors)
                {
                    if (anchor == null) continue;

                    anchor.gameObject.layer = m_roomMeshLayer;

                    var colliders = anchor.GetComponentsInChildren<Collider>(true);
                    foreach (var collider in colliders)
                    {
                        collider.enabled = true;
                        collider.gameObject.layer = m_roomMeshLayer;
                        
                        if (!string.IsNullOrEmpty(m_roomMeshTag))
                        {
                            try { collider.gameObject.tag = m_roomMeshTag; }
                            catch { /* Tag might not exist */ }
                        }

                        totalColliders++;
                    }
                }
            }

            m_collidersSetup = true;
            Debug.Log($"[RoomColliderSetup] Setup complete. Total colliders enabled: {totalColliders}");
        }

        private void EnableDebugVisualization(MeshFilter meshFilter)
        {
            var renderer = meshFilter.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = meshFilter.gameObject.AddComponent<MeshRenderer>();
            }

            // Create debug material if not assigned
            if (m_debugMaterial == null)
            {
                m_debugMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                m_debugMaterial.color = m_debugColor;
                m_debugMaterial.SetFloat("_Surface", 1); // Transparent
                m_debugMaterial.SetFloat("_Blend", 0);   // Alpha blend
                m_debugMaterial.SetFloat("_AlphaClip", 0);
                m_debugMaterial.renderQueue = 3000; // Transparent queue
                m_debugMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m_debugMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m_debugMaterial.SetInt("_ZWrite", 0);
                m_debugMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            renderer.material = m_debugMaterial;
            renderer.enabled = true;

            Debug.Log($"[RoomColliderSetup] Debug visualization enabled for: {meshFilter.gameObject.name}");
        }

        /// <summary>
        /// Force re-setup of colliders (useful after room updates).
        /// </summary>
        public void ForceResetup()
        {
            m_collidersSetup = false;
            SetupRoomColliders();
        }

        /// <summary>
        /// Toggle debug visualization at runtime.
        /// </summary>
        public void ToggleDebugVisualization(bool enable)
        {
            m_showDebugVisualization = enable;

            if (MRUK.Instance == null) return;

            foreach (var room in MRUK.Instance.Rooms)
            {
                if (room?.GlobalMeshAnchor == null) continue;

                var renderers = room.GlobalMeshAnchor.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var renderer in renderers)
                {
                    renderer.enabled = enable;
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!m_showDebugVisualization || MRUK.Instance == null) return;

            Gizmos.color = m_debugColor;

            foreach (var room in MRUK.Instance.Rooms)
            {
                if (room == null) continue;

                // Draw room bounds
                var bounds = room.GetRoomBounds();
                Gizmos.DrawWireCube(bounds.center, bounds.size);

                // Draw anchor positions
                foreach (var anchor in room.Anchors)
                {
                    if (anchor == null) continue;
                    Gizmos.DrawWireSphere(anchor.transform.position, 0.1f);
                }
            }
        }
#endif
    }
}
