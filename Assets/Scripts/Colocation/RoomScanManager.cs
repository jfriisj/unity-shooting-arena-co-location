// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Threading.Tasks;
using Fusion;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace MRMotifs.ColocatedExperiences.Colocation
{
    /// <summary>
    /// Manages the room scanning flow for the host player.
    /// 
    /// Flow:
    /// 1. On startup, checks if a scene model exists
    /// 2. If no scene exists OR user requests rescan, prompts Space Setup
    /// 3. Loads the scene model from device
    /// 4. Scene is automatically persisted by Meta Horizon OS for fast reuse
    /// 
    /// The saved scene can be shared with other users via RoomSharingMotif.
    /// </summary>
    public class RoomScanManager : NetworkBehaviour
    {
        [Header("Scan Settings")]
        [SerializeField]
        [Tooltip("Automatically prompt for room scan if no scene exists")]
        private bool m_autoPromptScanIfNeeded = true;

        [SerializeField]
        [Tooltip("Show UI to allow user to rescan their room")]
        private bool m_allowRescan = true;

        [SerializeField]
        [Tooltip("Timeout in seconds when waiting for scene to load")]
        private float m_loadTimeout = 30f;

        [Header("Events")]
        [SerializeField]
        private UnityEngine.Events.UnityEvent m_onScanStarted;

        [SerializeField]
        private UnityEngine.Events.UnityEvent m_onScanCompleted;

        [SerializeField]
        private UnityEngine.Events.UnityEvent m_onSceneLoaded;

        [SerializeField]
        private UnityEngine.Events.UnityEvent m_onScanFailed;

        // Public events for script access
        public event Action OnScanStarted;
        public event Action OnScanCompleted;
        public event Action OnSceneLoaded;
        public event Action<string> OnScanFailed;

        private MRUK m_mruk;
        private bool m_isScanning = false;
        private bool m_sceneLoaded = false;

        /// <summary>
        /// Returns true if a scene has been successfully loaded
        /// </summary>
        public bool IsSceneLoaded => m_sceneLoaded;

        /// <summary>
        /// Returns true if currently in a scanning flow
        /// </summary>
        public bool IsScanning => m_isScanning;

        public override void Spawned()
        {
            base.Spawned();

            // Only the host/master client manages room scanning
            if (!Runner.IsSharedModeMasterClient)
            {
                Debug.Log("[RoomScan] Not the host - room scanning disabled");
                return;
            }

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            Debug.Log("[RoomScan] Host: Initializing room scan manager...");

            // Wait for MRUK to be available
            m_mruk = await WaitForMRUKAsync();
            if (m_mruk == null)
            {
                HandleError("MRUK not available");
                return;
            }

            // Subscribe to MRUK events
            m_mruk.SceneLoadedEvent.AddListener(OnMRUKSceneLoaded);

            // Check if scene already exists and try to load it
            await TryLoadExistingSceneAsync();
        }

        private async Task<MRUK> WaitForMRUKAsync()
        {
            float elapsed = 0f;
            while (MRUK.Instance == null && elapsed < 10f)
            {
                await Task.Delay(100);
                elapsed += 0.1f;
            }
            return MRUK.Instance;
        }

        private async Task TryLoadExistingSceneAsync()
        {
            Debug.Log("[RoomScan] Attempting to load existing scene from device...");

            var result = await m_mruk.LoadSceneFromDevice();

            if (result == MRUK.LoadDeviceResult.Success)
            {
                Debug.Log("[RoomScan] Existing scene loaded successfully!");
                m_sceneLoaded = true;
                OnSceneLoaded?.Invoke();
                m_onSceneLoaded?.Invoke();
            }
            else if (result == MRUK.LoadDeviceResult.NoScenePermission)
            {
                HandleError("Spatial data permission not granted. Please enable in Settings.");
            }
            else if (result == MRUK.LoadDeviceResult.NoRoomsFound)
            {
                Debug.Log("[RoomScan] No existing scene found.");

                if (m_autoPromptScanIfNeeded)
                {
                    Debug.Log("[RoomScan] Auto-prompting user for room scan...");
                    await RequestRoomScanAsync();
                }
                else
                {
                    Debug.Log("[RoomScan] Waiting for manual scan request.");
                }
            }
            else
            {
                HandleError($"Failed to load scene: {result}");
            }
        }

        /// <summary>
        /// Request the user to scan their room.
        /// This will pause the app and launch the system Space Setup flow.
        /// </summary>
        public async void RequestRoomScan()
        {
            await RequestRoomScanAsync();
        }

        private async Task RequestRoomScanAsync()
        {
            if (m_isScanning)
            {
                Debug.LogWarning("[RoomScan] Already scanning, ignoring request.");
                return;
            }

            m_isScanning = true;
            Debug.Log("[RoomScan] Requesting Space Setup...");

            OnScanStarted?.Invoke();
            m_onScanStarted?.Invoke();

            try
            {
                // Request Space Setup - this will pause the app
                var success = await OVRScene.RequestSpaceSetup();

                if (success)
                {
                    Debug.Log("[RoomScan] Space Setup completed successfully.");
                    OnScanCompleted?.Invoke();
                    m_onScanCompleted?.Invoke();

                    // Load the newly created scene
                    await LoadSceneAfterScanAsync();
                }
                else
                {
                    Debug.LogWarning("[RoomScan] Space Setup was cancelled or failed.");
                    HandleError("Room scan was cancelled");
                }
            }
            catch (Exception e)
            {
                HandleError($"Space Setup error: {e.Message}");
            }
            finally
            {
                m_isScanning = false;
            }
        }

        private async Task LoadSceneAfterScanAsync()
        {
            Debug.Log("[RoomScan] Loading scene after scan...");

            // Give the system a moment to finalize the scene
            await Task.Delay(500);

            var result = await m_mruk.LoadSceneFromDevice();

            if (result == MRUK.LoadDeviceResult.Success)
            {
                Debug.Log("[RoomScan] Scene loaded successfully after scan!");
                m_sceneLoaded = true;
                OnSceneLoaded?.Invoke();
                m_onSceneLoaded?.Invoke();
            }
            else
            {
                HandleError($"Failed to load scene after scan: {result}");
            }
        }

        private void OnMRUKSceneLoaded()
        {
            Debug.Log("[RoomScan] MRUK SceneLoadedEvent fired.");
            m_sceneLoaded = true;

            // Log room info
            var rooms = m_mruk.Rooms;
            Debug.Log($"[RoomScan] Loaded {rooms?.Count ?? 0} room(s)");

            if (rooms != null)
            {
                foreach (var room in rooms)
                {
                    var anchors = room.Anchors;
                    Debug.Log($"[RoomScan]   Room '{room.name}': {anchors?.Count ?? 0} anchors");
                }
            }
        }

        private void HandleError(string message)
        {
            Debug.LogError($"[RoomScan] Error: {message}");
            OnScanFailed?.Invoke(message);
            m_onScanFailed?.Invoke();
        }

        /// <summary>
        /// Allows the user to rescan their room, replacing the existing scene.
        /// </summary>
        public async void RescanRoom()
        {
            if (!m_allowRescan)
            {
                Debug.LogWarning("[RoomScan] Rescan is not allowed.");
                return;
            }

            Debug.Log("[RoomScan] User requested room rescan...");

            // Clear existing scene data from MRUK
            if (m_mruk != null)
            {
                m_mruk.ClearScene();
            }

            m_sceneLoaded = false;
            await RequestRoomScanAsync();
        }

        private void OnDestroy()
        {
            if (m_mruk != null)
            {
                m_mruk.SceneLoadedEvent.RemoveListener(OnMRUKSceneLoaded);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Debug: Request Room Scan")]
        private void DebugRequestScan()
        {
            RequestRoomScan();
        }

        [ContextMenu("Debug: Rescan Room")]
        private void DebugRescan()
        {
            RescanRoom();
        }
#endif
    }
}
