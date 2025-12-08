// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using System;
using System.Linq;
using System.Threading.Tasks;
using Meta.XR.Samples;
using Meta.XR.MRUtilityKit;

namespace MRMotifs.ColocatedExperiences.Colocation
{
    /// <summary>
    /// Handles sharing and loading of room mesh data across co-located players.
    /// 
    /// Two alignment modes are supported:
    /// 1. LocalRoomWithAnchorAlignment (Recommended): Both devices scan their own room,
    ///    alignment is done via Shared Spatial Anchor. More stable.
    /// 2. SharedRoomFromHost: Host shares their room mesh to clients. 
    ///    Can have alignment issues if floor poses don't match perfectly.
    /// </summary>
    [MetaCodeSample("MRMotifs-ColocatedExperiences")]
    public class RoomSharingMotif : NetworkBehaviour
    {
        public enum AlignmentMode
        {
            /// <summary>
            /// Both devices load their own room scan, align via shared spatial anchor.
            /// More stable - each device uses its own room mesh which fits better.
            /// </summary>
            LocalRoomWithAnchorAlignment,
            
            /// <summary>
            /// Host shares their room mesh to clients via Space Sharing API.
            /// Can have alignment issues if the floor pose doesn't match.
            /// </summary>
            SharedRoomFromHost
        }
        
        [Header("Room Sharing Settings")]
        [Tooltip("Alignment mode for co-located room mesh.")]
        [SerializeField] private AlignmentMode m_alignmentMode = AlignmentMode.LocalRoomWithAnchorAlignment;
        
        [Tooltip("Enable room mesh sharing for consistent collisions across devices.")]
        [SerializeField] private bool m_enableRoomSharing = true;

        [Tooltip("Enable GlobalMesh colliders after room is loaded.")]
        [SerializeField] private bool m_enableGlobalMeshColliders = true;

        [Tooltip("Time to wait for MRUK to be ready before sharing room.")]
        [SerializeField] private float m_mrukWaitTimeout = 10f;
        
        [Tooltip("For LocalRoomWithAnchorAlignment mode: wait for anchor alignment before loading room.")]
        [SerializeField] private float m_waitForAlignmentTimeout = 20f;

        /// <summary>
        /// The shared anchor group ID used for room sharing.
        /// </summary>
        [Networked] private NetworkString<_64> SharedGroupId { get; set; }

        /// <summary>
        /// Whether the host has shared their room.
        /// </summary>
        [Networked] private NetworkBool RoomShared { get; set; }

        /// <summary>
        /// Room UUIDs (comma-separated for multiple rooms).
        /// </summary>
        [Networked] private NetworkString<_512> NetworkedRoomUuids { get; set; }

        /// <summary>
        /// Alignment room UUID (the room used for coordinate alignment).
        /// </summary>
        [Networked] private NetworkString<_64> AlignmentRoomUuid { get; set; }

        /// <summary>
        /// Floor anchor pose for alignment (serialized as "posX,posY,posZ,rotX,rotY,rotZ,rotW").
        /// </summary>
        [Networked] private NetworkString<_128> NetworkedFloorPose { get; set; }

        /// <summary>
        /// Flag indicating room data is ready for guests.
        /// </summary>
        [Networked] private NetworkBool RoomDataReady { get; set; }

        private MRUK m_mruk;

        public override void Spawned()
        {
            base.Spawned();
            
            Debug.Log($"[RoomSharing] ========== SPAWNED ==========");
            Debug.Log($"[RoomSharing] Runner: {(Runner != null ? "exists" : "NULL")}");
            Debug.Log($"[RoomSharing] Runner.IsRunning: {(Runner != null ? Runner.IsRunning.ToString() : "N/A")}");
            Debug.Log($"[RoomSharing] m_enableRoomSharing: {m_enableRoomSharing}");
            Debug.Log($"[RoomSharing] m_alignmentMode: {m_alignmentMode}");

            if (!m_enableRoomSharing)
            {
                Debug.Log("[RoomSharing] Room sharing is disabled.");
                return;
            }

            m_mruk = MRUK.Instance;
            Debug.Log($"[RoomSharing] MRUK.Instance: {(m_mruk != null ? "exists" : "NULL")}");

            // Use IsSharedModeMasterClient for Shared Mode
            bool isMasterClient = Runner != null && Runner.IsSharedModeMasterClient;
            Debug.Log($"[RoomSharing] Spawned - IsMasterClient: {isMasterClient}");

            if (m_alignmentMode == AlignmentMode.LocalRoomWithAnchorAlignment)
            {
                // Both devices scan their own room, align via shared spatial anchor
                // This is handled by SharedSpatialAnchorManager, not room sharing
                LoadLocalRoomAsync(isMasterClient);
            }
            else
            {
                // SharedRoomFromHost mode - original behavior
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
        }
        
        /// <summary>
        /// LocalRoomWithAnchorAlignment mode: Both devices load their own room scan.
        /// Alignment is done via shared spatial anchor (handled separately).
        /// For guests, we wait for anchor alignment to complete before loading the room.
        /// </summary>
        private async void LoadLocalRoomAsync(bool isMasterClient)
        {
            Debug.Log($"[RoomSharing] ========== LOCAL ROOM MODE ({(isMasterClient ? "HOST" : "GUEST")}) ==========");
            
            // Ensure MRUK instance is available
            if (m_mruk == null)
            {
                m_mruk = MRUK.Instance;
            }
            
            if (m_mruk == null)
            {
                Debug.LogError("[RoomSharing] MRUK Instance is null. Cannot load scene.");
                return;
            }
            
            // For guests, wait for anchor alignment to complete before loading room
            // This ensures the OVRCameraRig is properly positioned before MRUK loads
            if (!isMasterClient)
            {
                Debug.Log("[RoomSharing] Guest: Waiting for anchor alignment before loading room...");
                var colocationManager = FindAnyObjectByType<ColocationManager>();
                
                float alignmentWait = 0f;
                while (colocationManager == null || !colocationManager.HasCalibrated)
                {
                    await Task.Delay(500);
                    alignmentWait += 0.5f;
                    
                    if (colocationManager == null)
                    {
                        colocationManager = FindAnyObjectByType<ColocationManager>();
                    }
                    
                    if (alignmentWait % 5f < 0.5f)
                    {
                        Debug.Log($"[RoomSharing] Guest: Still waiting for alignment... ({alignmentWait}s elapsed)");
                    }
                    
                    if (alignmentWait > m_waitForAlignmentTimeout)
                    {
                        Debug.LogWarning("[RoomSharing] Guest: Timeout waiting for alignment. Loading room anyway.");
                        break;
                    }
                }
                
                if (colocationManager != null && colocationManager.HasCalibrated)
                {
                    Debug.Log($"[RoomSharing] Guest: Anchor alignment complete! Calibration error: {colocationManager.GetCurrentCalibrationError():F2}mm");
                }
            }
            
            // Check if room is already loaded (from previous space setup)
            var existingRoom = m_mruk.GetCurrentRoom();
            if (existingRoom != null)
            {
                Debug.Log($"[RoomSharing] Room already exists! UUID: {existingRoom.Anchor.Uuid}");
                Debug.Log("[RoomSharing] Skipping LoadSceneFromDevice to avoid space setup UI.");
            }
            else
            {
                // Both host and guest load their own room from device
                Debug.Log("[RoomSharing] No existing room found. Loading room from device (local scan)...");
                var loadResult = await m_mruk.LoadSceneFromDevice();
                Debug.Log($"[RoomSharing] LoadSceneFromDevice result: {loadResult}");
                
                // Wait for MRUK room to be available
                float elapsed = 0f;
                while (m_mruk.GetCurrentRoom() == null)
                {
                    await Task.Delay(500);
                    elapsed += 0.5f;
                    
                    if (elapsed > m_mrukWaitTimeout)
                    {
                        Debug.LogWarning("[RoomSharing] Timeout waiting for MRUK room.");
                        return;
                    }
                }
            }
            
            var room = m_mruk.GetCurrentRoom();
            Debug.Log($"[RoomSharing] Room loaded! Anchor UUID: {room.Anchor.Uuid}");
            Debug.Log($"[RoomSharing] Room has FloorAnchor: {(room.FloorAnchor != null ? "yes" : "NO")}");
            Debug.Log($"[RoomSharing] Room has GlobalMeshAnchor: {(room.GlobalMeshAnchor != null ? "yes" : "NO")}");
            Debug.Log($"[RoomSharing] Total anchors in room: {room.Anchors?.Count ?? 0}");
            
            // Enable GlobalMesh colliders
            if (m_enableGlobalMeshColliders)
            {
                EnableGlobalMeshColliders();
                
                // For guests, also schedule a delayed retry to ensure colliders are enabled
                if (!isMasterClient)
                {
                    _ = RetryEnableCollidersAsync();
                }
            }
            
            // Set RoomDataReady if we're the master - lets guests know things are set up
            if (isMasterClient)
            {
                RoomDataReady = true;
                Debug.Log("[RoomSharing] Host: RoomDataReady = true (local mode, no room sharing needed)");
            }
            
            Debug.Log("[RoomSharing] ========== LOCAL ROOM LOAD COMPLETE ==========");
        }

        private async void ShareRoomAsync()
        {
            Debug.Log("[RoomSharing] Host: Loading room from device...");

            // Ensure MRUK instance is available
            if (m_mruk == null)
            {
                m_mruk = MRUK.Instance;
            }

            // Host needs to load scene from device first
            if (m_mruk != null)
            {
                // Explicitly load from device for host
                var loadResult = await m_mruk.LoadSceneFromDevice();
                Debug.Log($"[RoomSharing] Host: LoadSceneFromDevice result: {loadResult}");
            }
            else
            {
                Debug.LogError("[RoomSharing] MRUK Instance is null. Cannot load scene.");
                return;
            }

            Debug.Log("[RoomSharing] Host: Waiting for MRUK room to be ready...");

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

            Debug.Log($"[RoomSharing] Host: Room found! Anchor UUID: {room.Anchor.Uuid}");
            Debug.Log($"[RoomSharing] Host: Room has FloorAnchor: {(room.FloorAnchor != null ? "yes" : "NO")}");
            Debug.Log($"[RoomSharing] Host: Room has GlobalMeshAnchor: {(room.GlobalMeshAnchor != null ? "yes" : "NO")}");
            Debug.Log($"[RoomSharing] Host: Total anchors in room: {room.Anchors?.Count ?? 0}");
            Debug.Log("[RoomSharing] Host: Attempting to share...");

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

                // Get floor anchor pose for alignment
                var floorPose = new Pose(
                    room.FloorAnchor.transform.position,
                    room.FloorAnchor.transform.rotation
                );

                // Share the room(s) - use all rooms for large space support
                var roomsToShare = m_mruk.Rooms.ToList();
                var shareResult = await MRUK.Instance.ShareRoomsAsync(roomsToShare, groupId);

                if (shareResult.Success)
                {
                    // Sync data to guests via Networked properties
                    var roomUuids = roomsToShare.Select(r => r.Anchor.Uuid.ToString());
                    NetworkedRoomUuids = string.Join(",", roomUuids);
                    AlignmentRoomUuid = room.Anchor.Uuid.ToString();
                    NetworkedFloorPose = SerializePose(floorPose);
                    SharedGroupId = groupId.ToString();
                    RoomDataReady = true;
                    RoomShared = true;
                    
                    Debug.Log($"[RoomSharing] ========== HOST SENDING SPACE DATA TO CLIENTS ==========");
                    Debug.Log($"[RoomSharing] Host: ShareRoomsAsync SUCCESS");
                    Debug.Log($"[RoomSharing] Host: NetworkedRoomUuids = {NetworkedRoomUuids}");
                    Debug.Log($"[RoomSharing] Host: AlignmentRoomUuid = {AlignmentRoomUuid}");
                    Debug.Log($"[RoomSharing] Host: NetworkedFloorPose = {NetworkedFloorPose}");
                    Debug.Log($"[RoomSharing] Host: SharedGroupId = {SharedGroupId}");
                    Debug.Log($"[RoomSharing] Host: RoomDataReady = {RoomDataReady}");
                    Debug.Log($"[RoomSharing] Host: {roomsToShare.Count} room(s) shared successfully!");

                    // Enable GlobalMesh colliders
                    if (m_enableGlobalMeshColliders)
                    {
                        EnableGlobalMeshColliders();
                    }
                }
                else
                {
                    Debug.LogError($"[RoomSharing] Host: ShareRoomsAsync FAILED! Result: {shareResult}");
                    Debug.LogError($"[RoomSharing] Host: RoomDataReady remains FALSE - clients will timeout");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoomSharing] Error sharing room: {ex.Message}");
                Debug.LogError($"[RoomSharing] Stack trace: {ex.StackTrace}");
            }
        }

        private async void WaitForSharedRoomAsync()
        {
            Debug.Log("[RoomSharing] ========== GUEST WAITING ==========" );
            Debug.Log($"[RoomSharing] Guest: RoomDataReady initial value: {RoomDataReady}");
            Debug.Log($"[RoomSharing] Guest: MRUK.Instance: {(MRUK.Instance != null ? "exists" : "NULL")}");

            // Wait for RoomDataReady flag to be true
            float elapsed = 0f;
            while (!RoomDataReady)
            {
                await Task.Delay(500);
                elapsed += 0.5f;
                
                // Log progress every 5 seconds
                if (elapsed % 5f < 0.5f)
                {
                    Debug.Log($"[RoomSharing] Guest: Still waiting... ({elapsed}s elapsed, RoomDataReady={RoomDataReady})");
                }

                if (elapsed > 30f)
                {
                    Debug.LogWarning("[RoomSharing] Timeout waiting for shared room. Loading room from device as fallback...");
                    
                    // Fallback: Load room from device since host didn't share in time
                    if (m_mruk == null)
                    {
                        m_mruk = MRUK.Instance;
                    }
                    
                    Debug.Log($"[RoomSharing] Guest fallback: MRUK.Instance: {(m_mruk != null ? "exists" : "NULL")}");
                    
                    if (m_mruk != null)
                    {
                        Debug.Log("[RoomSharing] Guest fallback: Calling LoadSceneFromDevice()...");
                        var loadResult = await m_mruk.LoadSceneFromDevice();
                        Debug.Log($"[RoomSharing] Guest fallback: LoadSceneFromDevice result: {loadResult}");
                        
                        // Wait a moment for MRUK to process
                        await Task.Delay(1000);
                        
                        // Check if room is now available
                        var room = m_mruk.GetCurrentRoom();
                        Debug.Log($"[RoomSharing] Guest fallback: Room after load: {(room != null ? "exists" : "NULL")}");
                        if (room != null)
                        {
                            Debug.Log($"[RoomSharing] Guest fallback: GlobalMeshAnchor: {(room.GlobalMeshAnchor != null ? "exists" : "NULL")}");
                            Debug.Log($"[RoomSharing] Guest fallback: Anchors count: {room.Anchors?.Count ?? 0}");
                        }
                    }
                    else
                    {
                        Debug.LogError("[RoomSharing] Guest fallback: MRUK is NULL, cannot load room!");
                    }
                    
                    EnableGlobalMeshColliders();
                    return;
                }
            }

            Debug.Log("[RoomSharing] ========== GUEST RECEIVED SPACE DATA FROM HOST ==========");
            Debug.Log($"[RoomSharing] Guest: RoomDataReady became TRUE!");
            Debug.Log($"[RoomSharing] Guest: SharedGroupId = {SharedGroupId}");
            Debug.Log($"[RoomSharing] Guest: NetworkedRoomUuids = {NetworkedRoomUuids}");
            Debug.Log($"[RoomSharing] Guest: AlignmentRoomUuid = {AlignmentRoomUuid}");
            Debug.Log($"[RoomSharing] Guest: NetworkedFloorPose = {NetworkedFloorPose}");

            try
            {
                if (string.IsNullOrEmpty(SharedGroupId.ToString()))
                {
                    Debug.LogError("[RoomSharing] Shared group ID is empty.");
                    return;
                }

                var groupId = Guid.Parse(SharedGroupId.ToString());
                
                // Parse networked room data
                var roomUuidStrings = NetworkedRoomUuids.ToString().Split(',');
                var roomUuids = roomUuidStrings.Where(s => !string.IsNullOrEmpty(s))
                    .Select(Guid.Parse).ToList();
                
                var alignmentRoomUuid = Guid.Parse(AlignmentRoomUuid.ToString());
                var floorPose = DeserializePose(NetworkedFloorPose.ToString());

                // Build alignment data tuple
                var alignmentData = (alignmentRoomUuid, floorPose);
                
                Debug.Log($"[RoomSharing] Guest: Parsed {roomUuids.Count} room UUID(s)");
                Debug.Log($"[RoomSharing] Guest: Calling LoadSceneFromSharedRooms with groupId: {groupId}");
                Debug.Log($"[RoomSharing] Guest: Alignment room: {alignmentRoomUuid}, Floor pose: {floorPose.position}");
                
                // Clear current scene first
                if (m_mruk != null)
                {
                    m_mruk.ClearScene();
                }

                // Load from shared rooms WITH alignment
                var loadResult = await MRUK.Instance.LoadSceneFromSharedRooms(
                    roomUuids: roomUuids,
                    groupUuid: groupId,
                    alignmentData: alignmentData,
                    removeMissingRooms: true
                );

                Debug.Log("[RoomSharing] Guest: LoadSceneFromSharedRooms completed!");
                
                // Verify room was loaded
                var loadedRoom = m_mruk?.GetCurrentRoom();
                Debug.Log($"[RoomSharing] Guest: Room after load: {(loadedRoom != null ? "exists" : "NULL")}");
                if (loadedRoom != null)
                {
                    Debug.Log($"[RoomSharing] Guest: Room UUID: {loadedRoom.Anchor.Uuid}");
                    Debug.Log($"[RoomSharing] Guest: GlobalMeshAnchor: {(loadedRoom.GlobalMeshAnchor != null ? "exists" : "NULL")}");
                    Debug.Log($"[RoomSharing] Guest: Anchors count: {loadedRoom.Anchors?.Count ?? 0}");
                }

                // Enable GlobalMesh colliders
                if (m_enableGlobalMeshColliders)
                {
                    EnableGlobalMeshColliders();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoomSharing] Error loading shared room: {ex.Message}");
                Debug.LogError($"[RoomSharing] Stack trace: {ex.StackTrace}");
                // Fallback: Use local room data
                Debug.Log("[RoomSharing] Guest: Falling back to EnableGlobalMeshColliders without shared room");
                EnableGlobalMeshColliders();
            }
        }

        /// <summary>
        /// Retry enabling colliders multiple times with delays.
        /// This ensures colliders get enabled even if MRUK timing is off.
        /// </summary>
        private async Task RetryEnableCollidersAsync()
        {
            // Wait a bit before first retry
            await Task.Delay(2000);
            
            for (int i = 0; i < 5; i++)
            {
                Debug.Log($"[RoomSharing] Retry enabling colliders attempt {i + 1}/5");
                EnableGlobalMeshColliders();
                
                // Check if we succeeded by looking for colliders
                var room = m_mruk?.GetCurrentRoom();
                if (room?.GlobalMeshAnchor != null)
                {
                    var meshColliders = room.GlobalMeshAnchor.GetComponentsInChildren<MeshCollider>(true);
                    if (meshColliders.Length > 0)
                    {
                        Debug.Log($"[RoomSharing] Retry succeeded! Found {meshColliders.Length} mesh colliders.");
                        return;
                    }
                }
                
                // Wait before next retry
                await Task.Delay(3000);
            }
            
            Debug.LogWarning("[RoomSharing] Retry enabling colliders exhausted all attempts.");
        }

        private void EnableGlobalMeshColliders()
        {
            Debug.Log("[RoomSharing] ========== ENABLING COLLIDERS ==========" );
            Debug.Log($"[RoomSharing] MRUK: {(m_mruk != null ? "exists" : "NULL")}");
            
            // Refresh MRUK reference
            if (m_mruk == null)
            {
                m_mruk = MRUK.Instance;
                Debug.Log($"[RoomSharing] Refreshed MRUK.Instance: {(m_mruk != null ? "exists" : "NULL")}");
            }

            // Find and enable colliders on GlobalMesh anchors
            var room = m_mruk?.GetCurrentRoom();
            if (room == null)
            {
                Debug.LogWarning("[RoomSharing] No room available for collider setup.");
                Debug.Log($"[RoomSharing] MRUK.Rooms count: {m_mruk?.Rooms?.Count ?? 0}");
                return;
            }
            
            Debug.Log($"[RoomSharing] Room found: {room.Anchor.Uuid}");
            Debug.Log($"[RoomSharing] Room.GlobalMeshAnchor: {(room.GlobalMeshAnchor != null ? "exists" : "NULL")}");
            Debug.Log($"[RoomSharing] Room.Anchors count: {room.Anchors?.Count ?? 0}");;

            // Set layer for room mesh (create "RoomMesh" layer in Unity)
            int roomMeshLayer = LayerMask.NameToLayer("RoomMesh");
            Debug.Log($"[RoomSharing] RoomMesh layer index: {roomMeshLayer}");
            if (roomMeshLayer < 0) roomMeshLayer = 0; // Fallback to Default

            int totalColliders = 0;

            // Try to find GlobalMesh and add colliders to ALL mesh children
            var globalMeshAnchor = room.GlobalMeshAnchor;
            if (globalMeshAnchor != null)
            {
                var meshFilters = globalMeshAnchor.GetComponentsInChildren<MeshFilter>(true);
                foreach (var meshFilter in meshFilters)
                {
                    if (meshFilter == null || meshFilter.sharedMesh == null) continue;

                    var meshCollider = meshFilter.gameObject.GetComponent<MeshCollider>();
                    if (meshCollider == null)
                    {
                        meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
                    }
                    meshCollider.sharedMesh = meshFilter.sharedMesh;
                    meshCollider.enabled = true;
                    
                    // Set layer and tag for collision detection
                    meshFilter.gameObject.layer = roomMeshLayer;
                    try { meshFilter.gameObject.tag = "RoomMesh"; }
                    catch { /* Tag might not exist */ }

                    totalColliders++;
                    Debug.Log($"[RoomSharing] GlobalMesh collider enabled: {meshFilter.gameObject.name}, layer: {LayerMask.LayerToName(roomMeshLayer)}");
                }
            }

            // Also enable colliders on room anchors (walls, floor, etc.)
            foreach (var anchor in room.Anchors)
            {
                if (anchor == null) continue;
                anchor.gameObject.layer = roomMeshLayer;
                try { anchor.gameObject.tag = "RoomMesh"; }
                catch { /* Tag might not exist */ }

                var colliders = anchor.GetComponentsInChildren<Collider>(true);
                foreach (var collider in colliders)
                {
                    collider.enabled = true;
                    collider.gameObject.layer = roomMeshLayer;
                    try { collider.gameObject.tag = "RoomMesh"; }
                    catch { /* Tag might not exist */ }
                    totalColliders++;
                }
            }

            Debug.Log($"[RoomSharing] Room colliders setup complete. Total colliders: {totalColliders}");
        }

        /// <summary>
        /// Serialize a Pose to a string format.
        /// </summary>
        private string SerializePose(Pose pose)
        {
            return $"{pose.position.x},{pose.position.y},{pose.position.z}," +
                   $"{pose.rotation.x},{pose.rotation.y},{pose.rotation.z},{pose.rotation.w}";
        }

        /// <summary>
        /// Deserialize a Pose from a string format.
        /// </summary>
        private Pose DeserializePose(string data)
        {
            var parts = data.Split(',').Select(float.Parse).ToArray();
            return new Pose(
                new Vector3(parts[0], parts[1], parts[2]),
                new Quaternion(parts[3], parts[4], parts[5], parts[6])
            );
        }
    }
}
#endif
