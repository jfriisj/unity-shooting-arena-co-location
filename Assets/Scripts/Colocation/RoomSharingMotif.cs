// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Meta.XR.Samples;
using Meta.XR.MRUtilityKit;

namespace MRMotifs.ColocatedExperiences.Colocation
{
    /// <summary>
    /// Handles sharing and loading of room mesh data across co-located players using MRUK Space Sharing API.
    /// When the host creates the colocation session, their room scan is shared with all guests.
    /// Guests load the shared room to ensure consistent collision geometry and coordinate frame alignment.
    /// 
    /// Flow:
    /// Host: MRUK.LoadSceneFromDevice() → ShareRoomsAsync() → Sync alignment data via Fusion
    /// Guest: Receive alignment data → MRUK.LoadSceneFromSharedRooms() with alignmentData
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
        [SerializeField] private float m_mrukWaitTimeout = 15f;

        [Tooltip("Time to wait for colocation session to be established.")]
        [SerializeField] private float m_colocationWaitTimeout = 30f;

        // Networked properties for room alignment data
        [Networked] private NetworkString<_64> SharedGroupIdString { get; set; }
        [Networked] private NetworkString<_64> SharedRoomUuidString { get; set; }
        [Networked] private Vector3 HostFloorPosition { get; set; }
        [Networked] private Quaternion HostFloorRotation { get; set; }
        [Networked] private NetworkBool RoomShared { get; set; }

        private MRUK m_mruk;
        private SharedSpatialAnchorManager m_ssaManager;
        private RoomScanManager m_roomScanManager;
        private bool m_roomLoaded = false;
        private bool m_isProcessing = false;
        private bool m_roomSharedLocally = false;

        // Events for startup flow
        public event Action OnRoomShared;
        public event Action OnRoomLoaded;
        public event Action<string> OnRoomSharingFailed;

        /// <summary>
        /// Returns true if the host has successfully shared the room.
        /// </summary>
        public bool IsRoomShared => m_roomSharedLocally || RoomShared;

        /// <summary>
        /// Returns true if the guest has successfully loaded the shared room.
        /// </summary>
        public bool IsRoomLoaded => m_roomLoaded;

        public override void Spawned()
        {
            base.Spawned();

            if (!m_enableRoomSharing)
            {
                Debug.Log("[RoomSharing] Room sharing is disabled.");
                return;
            }

            m_mruk = MRUK.Instance;
            m_ssaManager = FindAnyObjectByType<SharedSpatialAnchorManager>();
            m_roomScanManager = GetComponent<RoomScanManager>();

            bool isMasterClient = Runner != null && Runner.IsSharedModeMasterClient;
            Debug.Log($"[RoomSharing] Spawned - IsMasterClient: {isMasterClient}");

            if (isMasterClient)
            {
                StartHostRoomSharing();
            }
            else
            {
                StartGuestRoomLoading();
            }
        }

        #region Host Flow

        private void StartHostRoomSharing()
        {
            if (m_ssaManager == null)
            {
                Debug.LogError("[RoomSharing] SharedSpatialAnchorManager not found. Cannot share room.");
                return;
            }

            // If RoomScanManager exists, wait for it to complete first
            if (m_roomScanManager != null && !m_roomScanManager.IsSceneLoaded)
            {
                Debug.Log("[RoomSharing] Host: Waiting for RoomScanManager to complete...");
                m_roomScanManager.OnSceneLoaded += OnRoomScanComplete;
                m_roomScanManager.OnScanFailed += OnRoomScanFailed;
                return;
            }

            // Subscribe to colocation session established event
            if (m_ssaManager.IsColocationEstablished)
            {
                // Already established, start sharing
                ShareRoomAsync(m_ssaManager.SharedAnchorGroupId);
            }
            else
            {
                m_ssaManager.OnColocationSessionEstablished += OnColocationEstablishedForHost;
                Debug.Log("[RoomSharing] Host: Waiting for colocation session to be established...");
            }
        }

        private void OnColocationEstablishedForHost(Guid groupId)
        {
            m_ssaManager.OnColocationSessionEstablished -= OnColocationEstablishedForHost;
            ShareRoomAsync(groupId);
        }

        private void OnRoomScanComplete()
        {
            if (m_roomScanManager != null)
            {
                m_roomScanManager.OnSceneLoaded -= OnRoomScanComplete;
                m_roomScanManager.OnScanFailed -= OnRoomScanFailed;
            }
            Debug.Log("[RoomSharing] Host: Room scan complete, proceeding with sharing...");
            StartHostRoomSharing();
        }

        private void OnRoomScanFailed(string error)
        {
            if (m_roomScanManager != null)
            {
                m_roomScanManager.OnSceneLoaded -= OnRoomScanComplete;
                m_roomScanManager.OnScanFailed -= OnRoomScanFailed;
            }
            Debug.LogError($"[RoomSharing] Host: Room scan failed: {error}. Cannot share room.");
        }

        private async void ShareRoomAsync(Guid groupId)
        {
            if (m_isProcessing) return;
            m_isProcessing = true;

            Debug.Log($"[RoomSharing] Host: Starting room sharing with group: {groupId}");

            try
            {
                // Ensure MRUK is available
                m_mruk = MRUK.Instance;
                if (m_mruk == null)
                {
                    Debug.LogError("[RoomSharing] MRUK.Instance is null. Cannot share room.");
                    m_isProcessing = false;
                    return;
                }

                // If no room is loaded yet, trigger loading from device
                if (m_mruk.GetCurrentRoom() == null)
                {
                    Debug.Log("[RoomSharing] Host: No room loaded. Loading scene from device...");
                    var loadResult = await m_mruk.LoadSceneFromDevice();
                    if (loadResult != MRUK.LoadDeviceResult.Success)
                    {
                        Debug.LogError($"[RoomSharing] Failed to load scene from device: {loadResult}");
                        m_isProcessing = false;
                        return;
                    }
                    Debug.Log("[RoomSharing] Host: Scene loaded from device successfully.");
                }

                // Wait for room to be fully available
                float elapsed = 0f;
                while (m_mruk.GetCurrentRoom() == null)
                {
                    await Task.Delay(500);
                    elapsed += 0.5f;

                    if (elapsed > m_mrukWaitTimeout)
                    {
                        Debug.LogWarning("[RoomSharing] Timeout waiting for MRUK room after load. Room sharing skipped.");
                        m_isProcessing = false;
                        return;
                    }
                }

                var room = m_mruk.GetCurrentRoom();
                if (room == null)
                {
                    Debug.LogError("[RoomSharing] No room available to share.");
                    m_isProcessing = false;
                    return;
                }

                // Get floor anchor for alignment data
                var floorAnchor = room.FloorAnchor;
                if (floorAnchor == null)
                {
                    Debug.LogError("[RoomSharing] Room has no floor anchor. Cannot establish alignment.");
                    m_isProcessing = false;
                    return;
                }

                // Get room UUID - OVRAnchor is a struct, not nullable
                var roomUuid = room.Anchor.Uuid;
                if (roomUuid == Guid.Empty)
                {
                    Debug.LogError("[RoomSharing] Room anchor UUID is empty.");
                    m_isProcessing = false;
                    return;
                }

                Debug.Log($"[RoomSharing] Host: Sharing room {roomUuid} with group {groupId}");
                Debug.Log($"[RoomSharing] Floor anchor pose - Pos: {floorAnchor.transform.position}, Rot: {floorAnchor.transform.rotation.eulerAngles}");

                // Share the room using MRUK ShareRoomsAsync
                var shareResult = await m_mruk.ShareRoomsAsync(new[] { room }, groupId);

                // OVRResult has .Success and .Status properties
                if (shareResult.Success && shareResult.Status == OVRAnchor.ShareResult.Success)
                {
                    // Store alignment data in networked properties
                    SharedGroupIdString = groupId.ToString();
                    SharedRoomUuidString = roomUuid.ToString();
                    HostFloorPosition = floorAnchor.transform.position;
                    HostFloorRotation = floorAnchor.transform.rotation;
                    RoomShared = true;
                    m_roomSharedLocally = true;

                    Debug.Log("[RoomSharing] Host: Room shared successfully!");
                    Debug.Log($"[RoomSharing] Alignment data synced - RoomUuid: {roomUuid}, FloorPos: {HostFloorPosition}");

                    // Notify listeners
                    OnRoomShared?.Invoke();

                    // Enable GlobalMesh colliders
                    if (m_enableGlobalMeshColliders)
                    {
                        await Task.Delay(500); // Give MRUK time to process
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
                Debug.LogError($"[RoomSharing] Error sharing room: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                m_isProcessing = false;
            }
        }

        #endregion

        #region Guest Flow

        private void StartGuestRoomLoading()
        {
            if (m_ssaManager == null)
            {
                Debug.LogError("[RoomSharing] SharedSpatialAnchorManager not found. Cannot load shared room.");
                return;
            }

            // Subscribe to colocation session established event
            if (m_ssaManager.IsColocationEstablished)
            {
                WaitForSharedRoomAsync(m_ssaManager.SharedAnchorGroupId);
            }
            else
            {
                m_ssaManager.OnColocationSessionEstablished += OnColocationEstablishedForGuest;
                Debug.Log("[RoomSharing] Guest: Waiting for colocation session to be established...");
            }
        }

        private void OnColocationEstablishedForGuest(Guid groupId)
        {
            m_ssaManager.OnColocationSessionEstablished -= OnColocationEstablishedForGuest;
            WaitForSharedRoomAsync(groupId);
        }

        private async void WaitForSharedRoomAsync(Guid discoveredGroupId)
        {
            if (m_isProcessing) return;
            m_isProcessing = true;

            Debug.Log($"[RoomSharing] Guest: Colocation established with group: {discoveredGroupId}");
            Debug.Log("[RoomSharing] Guest: Waiting for host to share room data...");

            try
            {
                // Wait for RoomShared flag and alignment data
                float elapsed = 0f;
                while (!RoomShared || string.IsNullOrEmpty(SharedRoomUuidString.ToString()))
                {
                    await Task.Delay(500);
                    elapsed += 0.5f;

                    if (elapsed > m_colocationWaitTimeout)
                    {
                        Debug.LogWarning("[RoomSharing] Timeout waiting for shared room. Using local room data.");
                        EnableGlobalMeshColliders();
                        m_isProcessing = false;
                        return;
                    }
                }

                Debug.Log("[RoomSharing] Guest: Received room sharing data from host.");

                // Parse alignment data from networked properties
                var groupId = Guid.Parse(SharedGroupIdString.ToString());
                var roomUuid = Guid.Parse(SharedRoomUuidString.ToString());
                var floorPose = new Pose(HostFloorPosition, HostFloorRotation);

                Debug.Log($"[RoomSharing] Guest: Loading room {roomUuid} from group {groupId}");
                Debug.Log($"[RoomSharing] Guest: Using floor alignment - Pos: {floorPose.position}, Rot: {floorPose.rotation.eulerAngles}");

                // Enable world lock for automatic camera alignment
                m_mruk = MRUK.Instance;
                if (m_mruk != null)
                {
                    m_mruk.EnableWorldLock = true;
                    Debug.Log("[RoomSharing] Guest: EnableWorldLock set to true.");

                    // Clear any existing scene data
                    m_mruk.ClearScene();
                }

                // Create alignment data tuple
                var alignmentData = (
                    alignmentRoomUuid: roomUuid,
                    floorWorldPoseOnHost: floorPose
                );

                // Load the shared room with alignment
                var loadResult = await MRUK.Instance.LoadSceneFromSharedRooms(
                    roomUuids: new[] { roomUuid },
                    groupUuid: groupId,
                    alignmentData: alignmentData,
                    removeMissingRooms: true
                );

                if (loadResult == MRUK.LoadDeviceResult.Success)
                {
                    m_roomLoaded = true;
                    Debug.Log("[RoomSharing] Guest: Shared room loaded and aligned successfully!");

                    // Notify listeners
                    OnRoomLoaded?.Invoke();

                    // Enable GlobalMesh colliders
                    if (m_enableGlobalMeshColliders)
                    {
                        await Task.Delay(500); // Give MRUK time to process
                        EnableGlobalMeshColliders();
                    }
                }
                else
                {
                    Debug.LogError($"[RoomSharing] Guest: Failed to load shared room. Result: {loadResult}");
                    OnRoomSharingFailed?.Invoke($"Failed to load shared room: {loadResult}");
                    // Fallback: enable colliders on local room if available
                    EnableGlobalMeshColliders();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoomSharing] Error loading shared room: {ex.Message}\n{ex.StackTrace}");
                OnRoomSharingFailed?.Invoke($"Error loading shared room: {ex.Message}");
                // Fallback: Use local room data
                EnableGlobalMeshColliders();
            }
            finally
            {
                m_isProcessing = false;
            }
        }

        #endregion

        #region Collider Setup

        private void EnableGlobalMeshColliders()
        {
            Debug.Log("[RoomSharing] Enabling GlobalMesh colliders and renderers...");

            m_mruk = MRUK.Instance;
            var room = m_mruk?.GetCurrentRoom();
            if (room == null)
            {
                Debug.LogWarning("[RoomSharing] No room available for collider setup.");
                return;
            }

            int collidersEnabled = 0;
            int renderersEnabled = 0;

            // Setup GlobalMesh collider and renderer if available
            var globalMeshAnchor = room.GlobalMeshAnchor;
            if (globalMeshAnchor != null)
            {
                var meshFilters = globalMeshAnchor.GetComponentsInChildren<MeshFilter>(true);
                foreach (var meshFilter in meshFilters)
                {
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        // Enable collider
                        var meshCollider = meshFilter.gameObject.GetComponent<MeshCollider>();
                        if (meshCollider == null)
                        {
                            meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
                        }
                        meshCollider.sharedMesh = meshFilter.sharedMesh;
                        meshCollider.enabled = true;
                        collidersEnabled++;

                        // Enable renderer for visibility
                        var meshRenderer = meshFilter.gameObject.GetComponent<MeshRenderer>();
                        if (meshRenderer != null)
                        {
                            meshRenderer.enabled = true;
                            renderersEnabled++;
                        }
                    }
                }
                
                // Activate the entire GlobalMesh hierarchy
                globalMeshAnchor.gameObject.SetActive(true);
                Debug.Log($"[RoomSharing] GlobalMesh colliders: {collidersEnabled}, renderers: {renderersEnabled}");
            }
            else
            {
                Debug.Log("[RoomSharing] No GlobalMeshAnchor found. Using individual anchor colliders.");
            }

            // Enable colliders and renderers on all room anchors (walls, floor, ceiling, furniture, etc.)
            foreach (var anchor in room.Anchors)
            {
                if (anchor == null) continue;

                // Enable colliders
                var colliders = anchor.GetComponentsInChildren<Collider>(true);
                foreach (var collider in colliders)
                {
                    collider.enabled = true;
                    collidersEnabled++;
                }

                // Enable renderers for visibility
                var renderers = anchor.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var renderer in renderers)
                {
                    renderer.enabled = true;
                    renderersEnabled++;
                }
            }

            Debug.Log($"[RoomSharing] Room setup complete. Total colliders: {collidersEnabled}, renderers: {renderersEnabled}");
        }

        #endregion

        private void OnDestroy()
        {
            if (m_ssaManager != null)
            {
                m_ssaManager.OnColocationSessionEstablished -= OnColocationEstablishedForHost;
                m_ssaManager.OnColocationSessionEstablished -= OnColocationEstablishedForGuest;
            }
        }
    }
}
#endif
