// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using System;
using System.Threading.Tasks;
using Meta.XR.Samples;
using Meta.XR.MRUtilityKit;

namespace MRMotifs.ColocatedExperiences.Colocation
{
    /// <summary>
    /// Handles sharing and loading of room mesh data across co-located players.
    /// When the host creates the colocation session, their room is shared.
    /// Guests load the shared room to ensure consistent collision geometry.
    /// </summary>
    [MetaCodeSample("MRMotifs-ColocatedExperiences")]
    public class RoomSharingMotif : NetworkBehaviour
    {
        [Header("Room Sharing Settings")]
        [Tooltip("Enable room mesh sharing for consistent collisions across devices.")]
        [SerializeField] private bool m_enableRoomSharing = true;

        [Tooltip("Enable GlobalMesh colliders after room is loaded.")]
        [SerializeField] private bool m_enableGlobalMeshColliders = true;

        [Tooltip("Time to wait for MRUK to be ready before sharing room.")]
        [SerializeField] private float m_mrukWaitTimeout = 10f;

        /// <summary>
        /// The shared anchor group ID used for room sharing.
        /// </summary>
        [Networked] private NetworkString<_64> SharedGroupId { get; set; }

        /// <summary>
        /// Whether the host has shared their room.
        /// </summary>
        [Networked] private NetworkBool RoomShared { get; set; }

        private MRUK m_mruk;
        private bool m_roomLoaded = false;

        public override void Spawned()
        {
            base.Spawned();

            if (!m_enableRoomSharing)
            {
                Debug.Log("[RoomSharing] Room sharing is disabled.");
                return;
            }

            m_mruk = MRUK.Instance;

            // Use IsSharedModeMasterClient for Shared Mode
            bool isMasterClient = Runner != null && Runner.IsSharedModeMasterClient;
            Debug.Log($"[RoomSharing] Spawned - IsMasterClient: {isMasterClient}");

            if (isMasterClient)
            {
                // Host: Share room after MRUK is ready
                ShareRoomAsync();
            }
            else
            {
                // Guest: Wait for room to be shared, then load it
                WaitForSharedRoomAsync();
            }
        }

        private async void ShareRoomAsync()
        {
            Debug.Log("[RoomSharing] Host: Waiting for MRUK to be ready...");

            // Wait for MRUK and room to be available
            float elapsed = 0f;
            while (m_mruk == null || m_mruk.GetCurrentRoom() == null)
            {
                await Task.Delay(500);
                elapsed += 0.5f;
                m_mruk = MRUK.Instance;

                if (elapsed > m_mrukWaitTimeout)
                {
                    Debug.LogWarning("[RoomSharing] Timeout waiting for MRUK room. Room sharing skipped.");
                    return;
                }
            }

            var room = m_mruk.GetCurrentRoom();
            if (room == null)
            {
                Debug.LogError("[RoomSharing] No room available to share.");
                return;
            }

            Debug.Log("[RoomSharing] Host: Room found, attempting to share...");

            try
            {
                // Get the colocation group ID from SharedSpatialAnchorManager
                var ssaManager = FindAnyObjectByType<SharedSpatialAnchorManager>();
                if (ssaManager == null)
                {
                    Debug.LogError("[RoomSharing] SharedSpatialAnchorManager not found. Cannot share room.");
                    return;
                }

                // Wait a bit for colocation session to be established
                await Task.Delay(2000);

                // Get the group UUID from advertisement
                // Note: We need to access this from the colocation session
                var groupIdField = typeof(SharedSpatialAnchorManager).GetField("m_sharedAnchorGroupId",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (groupIdField == null)
                {
                    Debug.LogError("[RoomSharing] Could not access sharedAnchorGroupId field.");
                    return;
                }

                var groupId = (Guid)groupIdField.GetValue(ssaManager);
                if (groupId == Guid.Empty)
                {
                    Debug.LogWarning("[RoomSharing] Group ID not yet available. Retrying...");
                    await Task.Delay(3000);
                    groupId = (Guid)groupIdField.GetValue(ssaManager);
                }

                if (groupId == Guid.Empty)
                {
                    Debug.LogError("[RoomSharing] Failed to get valid group ID.");
                    return;
                }

                Debug.Log($"[RoomSharing] Sharing room with group: {groupId}");

                // Share the room
                var shareResult = await room.ShareRoomAsync(groupId);

                if (shareResult.Success)
                {
                    SharedGroupId = groupId.ToString();
                    RoomShared = true;
                    Debug.Log("[RoomSharing] Host: Room shared successfully!");

                    // Enable GlobalMesh colliders
                    if (m_enableGlobalMeshColliders)
                    {
                        EnableGlobalMeshColliders();
                    }
                }
                else
                {
                    Debug.LogError($"[RoomSharing] Failed to share room. Result: {shareResult}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoomSharing] Error sharing room: {ex.Message}");
            }
        }

        private async void WaitForSharedRoomAsync()
        {
            Debug.Log("[RoomSharing] Guest: Waiting for host to share room...");

            // Wait for RoomShared flag to be true
            float elapsed = 0f;
            while (!RoomShared)
            {
                await Task.Delay(500);
                elapsed += 0.5f;

                if (elapsed > 30f)
                {
                    Debug.LogWarning("[RoomSharing] Timeout waiting for shared room. Using local room data.");
                    EnableGlobalMeshColliders();
                    return;
                }
            }

            Debug.Log("[RoomSharing] Guest: Host shared room, loading...");

            try
            {
                if (string.IsNullOrEmpty(SharedGroupId.ToString()))
                {
                    Debug.LogError("[RoomSharing] Shared group ID is empty.");
                    return;
                }

                var groupId = Guid.Parse(SharedGroupId.ToString());
                
                // Load the shared room
                Debug.Log($"[RoomSharing] Loading shared room from group: {groupId}");
                
                // Clear current scene first
                if (m_mruk != null)
                {
                    m_mruk.ClearScene();
                }

                // Load from shared rooms
                // Note: alignmentData is used to transform the room to match local tracking
                await MRUK.Instance.LoadSceneFromSharedRooms(null, groupId, null);

                m_roomLoaded = true;
                Debug.Log("[RoomSharing] Guest: Shared room loaded successfully!");

                // Enable GlobalMesh colliders
                if (m_enableGlobalMeshColliders)
                {
                    EnableGlobalMeshColliders();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoomSharing] Error loading shared room: {ex.Message}");
                // Fallback: Use local room data
                EnableGlobalMeshColliders();
            }
        }

        private void EnableGlobalMeshColliders()
        {
            Debug.Log("[RoomSharing] Enabling GlobalMesh colliders...");

            // Find and enable colliders on GlobalMesh anchors
            var room = m_mruk?.GetCurrentRoom();
            if (room == null)
            {
                Debug.LogWarning("[RoomSharing] No room available for collider setup.");
                return;
            }

            // Try to find GlobalMesh and add colliders
            var globalMeshAnchors = room.GlobalMeshAnchor;
            if (globalMeshAnchors != null)
            {
                var meshFilter = globalMeshAnchors.GetComponentInChildren<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    var meshCollider = meshFilter.gameObject.GetComponent<MeshCollider>();
                    if (meshCollider == null)
                    {
                        meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
                    }
                    meshCollider.sharedMesh = meshFilter.sharedMesh;
                    meshCollider.enabled = true;
                    Debug.Log("[RoomSharing] GlobalMesh collider enabled.");
                }
            }

            // Also enable colliders on room anchors (walls, floor, etc.)
            foreach (var anchor in room.Anchors)
            {
                if (anchor == null) continue;

                var colliders = anchor.GetComponentsInChildren<Collider>(true);
                foreach (var collider in colliders)
                {
                    collider.enabled = true;
                }
            }

            Debug.Log("[RoomSharing] Room colliders setup complete.");
        }
    }
}
#endif
