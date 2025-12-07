// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using Meta.XR.MRUtilityKit;
using Meta.XR.Samples;

namespace MRMotifs.ColocatedExperiences.Colocation
{
    /// <summary>
    /// Automatic colocation startup following Meta's official architecture:
    /// 
    /// Smart Auto-Start Flow:
    /// 1. On Start, try to discover nearby sessions (3 second timeout)
    /// 2. If session found → become CLIENT
    /// 3. If no session found → become HOST
    /// 
    /// Host Flow:
    /// 1. Start Bluetooth advertisement → get groupUuid
    /// 2. Scan room (MRUK)
    /// 3. Start Photon session with groupUuid as session name
    /// 4. Share room via Space Sharing API
    /// 
    /// Client Flow:
    /// 1. Discover host via Bluetooth → get groupUuid
    /// 2. Join Photon session using groupUuid
    /// 3. Load shared room via Space Sharing API
    /// 
    /// Based on: https://developers.meta.com/horizon/documentation/unity/unity-colocation-discovery
    /// </summary>
    [MetaCodeSample("MRMotifs-ColocatedExperiences")]
    public class ColocationStartup : MonoBehaviour, IColocationStartup
    {
        #region Serialized Fields
        
        [Header("UI References (Optional)")]
        [SerializeField] private TMP_Text m_statusText;
        
        [Header("Prefabs")]
        [SerializeField] private NetworkRunner m_networkRunnerPrefab;
        
        [Header("Settings")]
        [Tooltip("How long to wait when discovering nearby sessions before becoming Host")]
        [SerializeField] private float m_discoveryWaitTime = 3f;
        
        [Header("XR Simulator / Editor Testing")]
        [Tooltip("Force a specific role when testing in Editor (Bluetooth doesn't work in simulator)")]
        [SerializeField] private SimulatorMode m_simulatorMode = SimulatorMode.AutoFirstHost;
        
        [Tooltip("Shared session name for simulator testing (both editors must use the same name)")]
        [SerializeField] private string m_simulatorSessionName = "ShootingGame_Dev";
        
        #endregion
        
        #region Enums
        
        public enum SimulatorMode
        {
            /// <summary>First editor becomes Host, subsequent ones become Client</summary>
            AutoFirstHost,
            /// <summary>Force this editor to be Host</summary>
            ForceHost,
            /// <summary>Force this editor to be Client</summary>
            ForceClient
        }
        
        #endregion

        #region Private Fields
        
        private Guid m_groupUuid;
        private NetworkRunner m_runner;
        private MRUK m_mruk;
        private bool m_isHost;
        private TaskCompletionSource<bool> m_discoveryTcs;
        private static bool s_isFirstInstance = true;
        
        // State tracking
        private ColocationState m_state = ColocationState.Initializing;
        
        #endregion

        #region Events
        
        public event Action OnColocationReady;
        public event Action<string> OnColocationFailed;
        
        #endregion

        #region Properties
        
        public bool IsHost => m_isHost;
        public Guid GroupUuid => m_groupUuid;
        public ColocationState State => m_state;
        public bool IsReady => m_state == ColocationState.Ready;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            m_mruk = MRUK.Instance;
        }

        private async void Start()
        {
            UpdateStatus("Initializing colocation...");
            
            // Smart Auto-Start: Try to discover, if nothing found become Host
            await SmartStart();
        }

        private void OnDestroy()
        {
            // Cleanup
            OVRColocationSession.ColocationSessionDiscovered -= OnSessionDiscovered;
        }
        
        #endregion

        #region Smart Auto-Start
        
        /// <summary>
        /// Smart startup flow:
        /// - In Editor/Simulator: Use hardcoded session name (Bluetooth doesn't work)
        /// - On Device: Use Bluetooth discovery
        /// </summary>
        private async Task SmartStart()
        {
            try
            {
#if UNITY_EDITOR
                // XR Simulator mode - Bluetooth doesn't work in Editor
                await SimulatorStart();
#else
                // Real device - use Bluetooth discovery
                await DeviceStart();
#endif
            }
            catch (Exception ex)
            {
                HandleError($"Smart start failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Simulator/Editor startup - uses hardcoded session name since Bluetooth doesn't work.
        /// </summary>
        private async Task SimulatorStart()
        {
            Debug.Log($"[ColocationStartup] Running in EDITOR - using simulator mode: {m_simulatorMode}");
            UpdateStatus($"Editor mode: {m_simulatorMode}");
            
            // Generate a deterministic groupUuid from session name
            m_groupUuid = GenerateGuidFromString(m_simulatorSessionName);
            
            bool shouldBeHost;
            
            switch (m_simulatorMode)
            {
                case SimulatorMode.ForceHost:
                    shouldBeHost = true;
                    break;
                case SimulatorMode.ForceClient:
                    shouldBeHost = false;
                    break;
                case SimulatorMode.AutoFirstHost:
                default:
                    // First instance becomes Host, subsequent become Client
                    // This uses a static flag - first editor to run claims Host
                    shouldBeHost = s_isFirstInstance;
                    s_isFirstInstance = false;
                    
                    // Also try to detect if a Photon session exists
                    // If we can quickly connect, someone else is already Host
                    Debug.Log("[ColocationStartup] AutoFirstHost: Checking for existing session...");
                    var existingSession = await TryFindExistingPhotonSession(m_simulatorSessionName);
                    if (existingSession)
                    {
                        Debug.Log("[ColocationStartup] Found existing session - becoming CLIENT");
                        shouldBeHost = false;
                    }
                    break;
            }
            
            if (shouldBeHost)
            {
                m_isHost = true;
                Debug.Log($"[ColocationStartup] SIMULATOR: Starting as HOST with session '{m_simulatorSessionName}'");
                await StartHostFlowSimulator();
            }
            else
            {
                m_isHost = false;
                Debug.Log($"[ColocationStartup] SIMULATOR: Starting as CLIENT with session '{m_simulatorSessionName}'");
                await StartClientFlowSimulator();
            }
        }

        /// <summary>
        /// Real device startup - uses Bluetooth discovery.
        /// </summary>
        private async Task DeviceStart()
        {
            SetState(ColocationState.DiscoveringSession);
            UpdateStatus("Looking for nearby sessions...");
            
            // Try to discover for a short time
            var discovered = await TryDiscoverNearbySession(m_discoveryWaitTime);
            
            if (discovered)
            {
                // Found a session - become Client
                m_isHost = false;
                Debug.Log("[ColocationStartup] Session discovered - starting as CLIENT");
                await StartClientFlow();
            }
            else
            {
                // No session found - become Host
                m_isHost = true;
                Debug.Log("[ColocationStartup] No session found - starting as HOST");
                await StartHostFlow();
            }
        }
        
        /// <summary>
        /// Generate a deterministic GUID from a string (for simulator testing).
        /// </summary>
        private Guid GenerateGuidFromString(string input)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return new Guid(hash);
            }
        }
        
        /// <summary>
        /// Try to find if a Photon session already exists (for auto-detection in simulator).
        /// </summary>
        private async Task<bool> TryFindExistingPhotonSession(string sessionName)
        {
            try
            {
                // Create a temporary runner to check if session exists
                var tempRunner = Instantiate(m_networkRunnerPrefab);
                tempRunner.name = "NetworkRunner (SessionCheck)";
                
                var startGameArgs = new StartGameArgs
                {
                    GameMode = GameMode.Shared,
                    SessionName = sessionName,
                    SceneManager = tempRunner.gameObject.AddComponent<NetworkSceneManagerDefault>()
                };
                
                // Try to join with a short timeout
                var joinTask = tempRunner.StartGame(startGameArgs);
                var timeoutTask = Task.Delay(2000); // 2 second timeout
                
                var completedTask = await Task.WhenAny(joinTask, timeoutTask);
                
                if (completedTask == joinTask && joinTask.Result.Ok)
                {
                    // Session exists! Check if we're the only one or if someone else is hosting
                    bool hasOtherPlayers = tempRunner.SessionInfo?.PlayerCount > 1;
                    
                    if (hasOtherPlayers)
                    {
                        // Someone else is already there - we should be client
                        // Keep this runner as our actual runner
                        m_runner = tempRunner;
                        m_runner.name = "NetworkRunner (Client)";
                        return true;
                    }
                    else
                    {
                        // We're the first one - become host
                        // Keep this runner
                        m_runner = tempRunner;
                        m_runner.name = "NetworkRunner (Host)";
                        return false;
                    }
                }
                else
                {
                    // Session doesn't exist or timed out
                    await tempRunner.Shutdown();
                    Destroy(tempRunner.gameObject);
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
        
        #endregion

        #region Simulator Flows (Editor Only)
        
#if UNITY_EDITOR
        /// <summary>
        /// Host flow for simulator - skips Bluetooth, uses MRUK synthetic environment.
        /// </summary>
        private async Task StartHostFlowSimulator()
        {
            try
            {
                // Skip Bluetooth - use predetermined session name
                Debug.Log($"[ColocationStartup] SIMULATOR Host: Using session '{m_simulatorSessionName}'");
                
                // Scan room from MRUK synthetic environment
                SetState(ColocationState.ScanningRoom);
                UpdateStatus("Loading synthetic room...");
                
                if (m_mruk != null && m_mruk.GetCurrentRoom() == null)
                {
                    var loadResult = await m_mruk.LoadSceneFromDevice();
                    Debug.Log($"[ColocationStartup] SIMULATOR Host: MRUK load result: {loadResult}");
                }
                
                // Start Photon if not already started by session check
                if (m_runner == null)
                {
                    SetState(ColocationState.StartingNetwork);
                    UpdateStatus("Starting network...");
                    await StartPhotonSession(m_simulatorSessionName);
                }
                
                Debug.Log("[ColocationStartup] SIMULATOR Host: Photon session ready");
                
                // Skip Space Sharing in simulator
                SetState(ColocationState.Ready);
                UpdateStatus("Ready - Simulator Host");
                
                OnColocationReady?.Invoke();
            }
            catch (Exception ex)
            {
                HandleError($"Simulator host error: {ex.Message}");
            }
        }

        /// <summary>
        /// Client flow for simulator - skips Bluetooth, joins by session name.
        /// </summary>
        private async Task StartClientFlowSimulator()
        {
            try
            {
                Debug.Log($"[ColocationStartup] SIMULATOR Client: Joining session '{m_simulatorSessionName}'");
                
                // Join Photon if not already joined by session check
                if (m_runner == null)
                {
                    SetState(ColocationState.JoiningNetwork);
                    UpdateStatus("Joining session...");
                    await JoinPhotonSession(m_simulatorSessionName);
                }
                
                Debug.Log("[ColocationStartup] SIMULATOR Client: Joined Photon session");
                
                // Load synthetic room from MRUK
                SetState(ColocationState.LoadingRoom);
                UpdateStatus("Loading room...");
                
                if (m_mruk != null && m_mruk.GetCurrentRoom() == null)
                {
                    var loadResult = await m_mruk.LoadSceneFromDevice();
                    Debug.Log($"[ColocationStartup] SIMULATOR Client: MRUK load result: {loadResult}");
                }
                
                // Skip Space Sharing alignment in simulator
                SetState(ColocationState.Ready);
                UpdateStatus("Ready - Simulator Client");
                
                OnColocationReady?.Invoke();
            }
            catch (Exception ex)
            {
                HandleError($"Simulator client error: {ex.Message}");
            }
        }
#endif
        
        #endregion

        #region Host Flow
        
        /// <summary>
        /// Host Flow (per Meta documentation):
        /// 1. Start Bluetooth advertisement → get groupUuid
        /// 2. Scan room (if needed)
        /// 3. Start Photon session with groupUuid as name
        /// 4. Share room via Space Sharing API
        /// </summary>
        private async Task StartHostFlow()
        {
            try
            {
                // Step 1: Start Bluetooth advertisement
                SetState(ColocationState.AdvertisingSession);
                UpdateStatus("Starting session advertisement...");
                
                var advertisementResult = await OVRColocationSession.StartAdvertisementAsync(null);
                
                if (!advertisementResult.Success)
                {
                    HandleError($"Failed to start advertisement: {advertisementResult.Status}");
                    return;
                }
                
                m_groupUuid = advertisementResult.Value;
                Debug.Log($"[ColocationStartup] Host: Advertisement started. GroupUuid: {m_groupUuid}");
                
                // Step 2: Scan room
                SetState(ColocationState.ScanningRoom);
                UpdateStatus("Scanning room...");
                
                if (m_mruk.GetCurrentRoom() == null)
                {
                    var loadResult = await m_mruk.LoadSceneFromDevice();
                    if (loadResult != MRUK.LoadDeviceResult.Success)
                    {
                        HandleError($"Failed to load room: {loadResult}");
                        return;
                    }
                }
                
                Debug.Log("[ColocationStartup] Host: Room scan complete");
                
                // Step 3: Start Photon session using groupUuid as session name
                SetState(ColocationState.StartingNetwork);
                UpdateStatus("Starting network...");
                
                await StartPhotonSession(m_groupUuid.ToString());
                
                Debug.Log("[ColocationStartup] Host: Photon session started");
                
                // Step 4: Share room via Space Sharing
                SetState(ColocationState.SharingRoom);
                UpdateStatus("Sharing room data...");
                
                await ShareRoom();
                
                Debug.Log("[ColocationStartup] Host: Room shared successfully");
                
                // Done!
                SetState(ColocationState.Ready);
                UpdateStatus("Ready - Waiting for players");
                
                OnColocationReady?.Invoke();
            }
            catch (Exception ex)
            {
                HandleError($"Host flow error: {ex.Message}");
            }
        }
        
        #endregion

        #region Client Flow
        
        /// <summary>
        /// Client Flow (per Meta documentation):
        /// 1. Discover nearby host via Bluetooth → get groupUuid
        /// 2. Join Photon session using groupUuid
        /// 3. Load shared room via Space Sharing API
        /// </summary>
        private async Task StartClientFlow()
        {
            try
            {
                // groupUuid already discovered in SmartStart
                Debug.Log($"[ColocationStartup] Client: Using discovered GroupUuid: {m_groupUuid}");
                
                // Step 1: Join Photon session
                SetState(ColocationState.JoiningNetwork);
                UpdateStatus("Joining session...");
                
                await JoinPhotonSession(m_groupUuid.ToString());
                
                Debug.Log("[ColocationStartup] Client: Joined Photon session");
                
                // Step 2: Load shared room
                SetState(ColocationState.LoadingRoom);
                UpdateStatus("Loading shared room...");
                
                await LoadSharedRoom();
                
                Debug.Log("[ColocationStartup] Client: Shared room loaded");
                
                // Done!
                SetState(ColocationState.Ready);
                UpdateStatus("Ready!");
                
                OnColocationReady?.Invoke();
            }
            catch (Exception ex)
            {
                HandleError($"Client flow error: {ex.Message}");
            }
        }
        
        #endregion

        #region Bluetooth Discovery
        
        /// <summary>
        /// Try to discover nearby sessions with a short timeout.
        /// Returns true if a session was found, false otherwise.
        /// </summary>
        private async Task<bool> TryDiscoverNearbySession(float timeoutSeconds)
        {
            m_discoveryTcs = new TaskCompletionSource<bool>();
            
            // Register callback before starting discovery
            OVRColocationSession.ColocationSessionDiscovered += OnSessionDiscovered;
            
            var discoveryResult = await OVRColocationSession.StartDiscoveryAsync();
            
            if (!discoveryResult.Success)
            {
                OVRColocationSession.ColocationSessionDiscovered -= OnSessionDiscovered;
                Debug.LogWarning($"[ColocationStartup] Discovery failed: {discoveryResult.Status}");
                return false;
            }
            
            // Wait for discovery or timeout
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
            var completedTask = await Task.WhenAny(m_discoveryTcs.Task, timeoutTask);
            
            // Stop discovery and cleanup
            await OVRColocationSession.StopDiscoveryAsync();
            OVRColocationSession.ColocationSessionDiscovered -= OnSessionDiscovered;
            
            if (completedTask == timeoutTask)
            {
                Debug.Log("[ColocationStartup] No sessions discovered within timeout");
                return false;
            }
            
            return m_discoveryTcs.Task.Result;
        }

        /// <summary>
        /// Callback when a colocation session is discovered via Bluetooth.
        /// Per Meta docs: receives the advertisement UUID and metadata.
        /// </summary>
        private void OnSessionDiscovered(OVRColocationSession.Data session)
        {
            m_groupUuid = session.AdvertisementUuid;
            Debug.Log($"[ColocationStartup] Discovered session with UUID: {m_groupUuid}");
            m_discoveryTcs?.TrySetResult(true);
        }
        
        #endregion

        #region Photon Networking
        
        /// <summary>
        /// Start Photon session as Host using groupUuid as session name.
        /// Per Meta docs: Use the advertisement UUID as the session identifier.
        /// </summary>
        private async Task StartPhotonSession(string sessionName)
        {
            m_runner = Instantiate(m_networkRunnerPrefab);
            m_runner.name = "NetworkRunner (Host)";
            
            var startGameArgs = new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = sessionName, // groupUuid from Colocation Discovery!
                SceneManager = m_runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
            };
            
            var result = await m_runner.StartGame(startGameArgs);
            
            if (!result.Ok)
            {
                throw new Exception($"Failed to start Photon: {result.ShutdownReason}");
            }
        }

        /// <summary>
        /// Join Photon session as Client using groupUuid.
        /// Per Meta docs: Use the discovered UUID to join the correct session.
        /// </summary>
        private async Task JoinPhotonSession(string sessionName)
        {
            m_runner = Instantiate(m_networkRunnerPrefab);
            m_runner.name = "NetworkRunner (Client)";
            
            var startGameArgs = new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = sessionName, // Same groupUuid discovered from Host!
                SceneManager = m_runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
            };
            
            var result = await m_runner.StartGame(startGameArgs);
            
            if (!result.Ok)
            {
                throw new Exception($"Failed to join Photon: {result.ShutdownReason}");
            }
        }
        
        #endregion

        #region Space Sharing
        
        /// <summary>
        /// Share the Host's room via Space Sharing API.
        /// Per Meta docs: Use ShareRoomsAsync with the groupUuid from Colocation Discovery.
        /// </summary>
        private async Task ShareRoom()
        {
            var room = m_mruk.GetCurrentRoom();
            if (room == null)
            {
                throw new Exception("No room available to share");
            }
            
            // Share room using groupUuid from Colocation Discovery
            var shareResult = await m_mruk.ShareRoomsAsync(new[] { room }, m_groupUuid);
            
            if (!shareResult.Success || shareResult.Status != OVRAnchor.ShareResult.Success)
            {
                throw new Exception($"Failed to share room: {shareResult.Status}");
            }
            
            Debug.Log($"[ColocationStartup] Room shared with group UUID: {m_groupUuid}");
        }

        /// <summary>
        /// Load the Host's shared room on the Client.
        /// Per Meta docs: Use LoadSceneFromSharedRooms with the groupUuid for automatic alignment.
        /// </summary>
        private async Task LoadSharedRoom()
        {
            // Load shared room using groupUuid - MRUK handles coordinate alignment automatically
            var loadResult = await m_mruk.LoadSceneFromSharedRooms(
                roomUuids: null,        // Load any room shared to this group
                groupUuid: m_groupUuid, // groupUuid from Colocation Discovery
                alignmentData: null     // MRUK handles alignment automatically
            );
            
            if (loadResult != MRUK.LoadDeviceResult.Success)
            {
                throw new Exception($"Failed to load shared room: {loadResult}");
            }
            
            Debug.Log("[ColocationStartup] Shared room loaded and aligned");
        }
        
        #endregion

        #region Helpers
        
        private void UpdateStatus(string message)
        {
            Debug.Log($"[ColocationStartup] {message}");
            if (m_statusText != null)
                m_statusText.text = message;
        }

        private void SetState(ColocationState newState)
        {
            m_state = newState;
            Debug.Log($"[ColocationStartup] State: {newState}");
        }

        private void HandleError(string error)
        {
            Debug.LogError($"[ColocationStartup] ERROR: {error}");
            SetState(ColocationState.Error);
            UpdateStatus($"Error: {error}");
            OnColocationFailed?.Invoke(error);
        }
        
        #endregion
    }

    /// <summary>
    /// Colocation state enum tracking the startup flow.
    /// Based on Meta's official Colocation Discovery architecture.
    /// </summary>
    public enum ColocationState
    {
        Initializing,          // Initial state
        DiscoveringSession,    // Looking for nearby sessions
        AdvertisingSession,    // Host: Broadcasting via Bluetooth
        ScanningRoom,          // Host: MRUK room scan
        StartingNetwork,       // Host: Starting Photon
        JoiningNetwork,        // Client: Joining Photon
        SharingRoom,           // Host: Sharing room via Space Sharing
        LoadingRoom,           // Client: Loading shared room
        Ready,                 // All setup complete
        Error                  // Error occurred
    }
}
#endif
