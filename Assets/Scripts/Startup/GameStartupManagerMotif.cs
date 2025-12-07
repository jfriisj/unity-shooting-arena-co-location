// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using System;
using System.Collections;
using UnityEngine;
using Fusion;
using Meta.XR.MRUtilityKit;
using Meta.XR.Samples;
using Meta.XR.MultiplayerBlocks.Fusion;
using MRMotifs.ColocatedExperiences.Colocation;

namespace MRMotifs.SharedActivities.Startup
{
    /// <summary>
    /// Startup state machine that tracks the initialization flow.
    /// </summary>
    public enum StartupState
    {
        Initializing,
        RoleSelection,        // NEW: User chooses Host or Client
        CheckingRoomScan,
        PromptingRoomScan,
        CreatingSession,
        JoiningSession,
        WaitingForAnchor,
        LocalizingAnchor,
        SharingRoom,
        LoadingRoom,
        Ready,
        Error
    }

    /// <summary>
    /// Orchestrates the entire game startup flow, clearly differentiating between Host and Client roles.
    /// Gates avatar spawning until all prerequisites are complete and provides clear visual feedback.
    /// 
    /// NEW: Now starts with Role Selection modal where user explicitly chooses Host or Client.
    /// 
    /// Host Flow:
    /// 1. Role Selection → Choose HOST
    /// 2. Room Scan → Prompt if needed
    /// 3. Create Network Session
    /// 4. Create Anchor → Share with group
    /// 5. Share Room Mesh
    /// 6. READY → Spawn avatar
    /// 
    /// Client Flow:
    /// 1. Role Selection → Choose CLIENT → Select session from list
    /// 2. Join Session → "Connecting..."
    /// 3. Wait for Anchor → "Waiting..."
    /// 4. Localize Anchor → "Aligning..."
    /// 5. Load Room Mesh → "Loading room"
    /// 6. READY → Spawn avatar
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class GameStartupManagerMotif : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private StartupFlowConfig m_config;

        [Header("References")]
        [SerializeField] private StartupModalUI m_modalUI;
        [SerializeField] private RoleSelectionModalUI m_roleSelectionUI;
        [SerializeField] private SessionDiscoveryManager m_sessionDiscovery;

        [Header("Role Selection")]
        [SerializeField] private bool m_requireRoleSelection = true;
        [SerializeField] private bool m_disableAutoMatchmaking = true;

        // State
        private StartupState m_currentState = StartupState.Initializing;
        private bool m_isHost = false;
        private bool m_roleSelected = false;
        private string m_selectedSessionName;
        private float m_stateStartTime;
        private string m_statusMessage = "Initializing...";
        private string m_errorMessage = "";
        private float m_progress = 0f;

        // Cached references
        private NetworkRunner m_networkRunner;
        private MRUK m_mruk;
        private RoomScanManager m_roomScanManager;
        private SharedSpatialAnchorManager m_ssaManager;
        private RoomSharingMotif m_roomSharing;
        private FusionBootstrap m_fusionBootstrap;
        private bool m_startupComplete = false;

        // Events
        public event Action<StartupState> OnStateChanged;
        public event Action OnStartupComplete;
        public event Action<string> OnError;

        // Public properties
        public bool IsHost => m_isHost;
        public StartupState CurrentState => m_currentState;
        public string StatusMessage => m_statusMessage;
        public string ErrorMessage => m_errorMessage;
        public float Progress => m_progress;
        public bool IsStartupComplete => m_startupComplete;
        public StartupFlowConfig Config => m_config;
        public bool RoleSelected => m_roleSelected;

        private void Awake()
        {
            // Subscribe to network events
            FusionBBEvents.OnSceneLoadDone += OnNetworkSessionReady;
        }

        private void Start()
        {
            if (m_config == null)
            {
                m_config = Resources.Load<StartupFlowConfig>("StartupFlowConfig");
            }

            if (m_config == null)
            {
                Debug.LogWarning("[GameStartup] No StartupFlowConfig found, using default values");
                m_config = ScriptableObject.CreateInstance<StartupFlowConfig>();
            }

            // Auto-find UI components if not assigned
            if (m_modalUI == null)
            {
                m_modalUI = FindAnyObjectByType<StartupModalUI>();
            }

            if (m_roleSelectionUI == null)
            {
                m_roleSelectionUI = FindAnyObjectByType<RoleSelectionModalUI>();
            }

            if (m_sessionDiscovery == null)
            {
                m_sessionDiscovery = FindAnyObjectByType<SessionDiscoveryManager>();
            }

            // Find and optionally disable auto-matchmaking
            m_fusionBootstrap = FindAnyObjectByType<FusionBootstrap>();
            if (m_fusionBootstrap != null && m_disableAutoMatchmaking)
            {
                // Disable automatic start - we'll trigger it manually after role selection
                m_fusionBootstrap.StartMode = FusionBootstrap.StartModes.Manual;
                Log("Disabled auto-matchmaking - waiting for role selection");
            }

            // Setup role selection UI events
            if (m_roleSelectionUI != null)
            {
                m_roleSelectionUI.OnHostSelected += OnHostRoleSelected;
                m_roleSelectionUI.OnSessionSelected += OnSessionSelected;
            }

            SetState(StartupState.Initializing);

            // If role selection is required, show the modal
            if (m_requireRoleSelection)
            {
                StartCoroutine(WaitForPlatformThenShowRoleSelection());
            }
            else
            {
                Log("Role selection disabled - waiting for auto-matchmaking...");
            }
        }

        private IEnumerator WaitForPlatformThenShowRoleSelection()
        {
            // Wait a moment for platform to initialize
            yield return new WaitForSeconds(1f);

            // Hide the startup modal until role is selected
            if (m_modalUI != null)
            {
                m_modalUI.Hide();
            }

            // Show role selection modal
            SetState(StartupState.RoleSelection);
            m_statusMessage = "Choose your role";
            
            if (m_roleSelectionUI != null)
            {
                m_roleSelectionUI.Show();
                Log("Showing role selection modal");
            }
            else
            {
                Log("Warning: RoleSelectionModalUI not found - creating session as host by default");
                OnHostRoleSelected();
            }
        }

        private void OnDestroy()
        {
            FusionBBEvents.OnSceneLoadDone -= OnNetworkSessionReady;
            UnsubscribeFromEvents();

            // Unsubscribe from role selection events
            if (m_roleSelectionUI != null)
            {
                m_roleSelectionUI.OnHostSelected -= OnHostRoleSelected;
                m_roleSelectionUI.OnSessionSelected -= OnSessionSelected;
            }
        }

        private void Update()
        {
            // Check for timeout in current state
            if (m_currentState != StartupState.Ready && m_currentState != StartupState.Error)
            {
                float elapsed = Time.time - m_stateStartTime;
                float timeout = GetTimeoutForState(m_currentState);

                if (elapsed > timeout)
                {
                    HandleTimeout();
                }
            }
        }

        #region Role Selection Events

        private void OnHostRoleSelected()
        {
            Log("HOST role selected by user");
            m_isHost = true;
            m_roleSelected = true;

            // Hide role selection UI
            if (m_roleSelectionUI != null)
            {
                m_roleSelectionUI.Hide();
            }

            // Show startup modal
            if (m_modalUI != null)
            {
                m_modalUI.Show();
            }

            // Start the host flow (room scan first, then network)
            StartCoroutine(HostFlowWithRoleSelectionCoroutine());
        }

        private void OnSessionSelected(string sessionName)
        {
            Log($"CLIENT selected session: {sessionName}");
            m_isHost = false;
            m_roleSelected = true;
            m_selectedSessionName = sessionName;

            // Hide role selection UI
            if (m_roleSelectionUI != null)
            {
                m_roleSelectionUI.Hide();
            }

            // Show startup modal
            if (m_modalUI != null)
            {
                m_modalUI.Show();
            }

            // Start the client flow (join the selected session)
            StartCoroutine(ClientFlowWithRoleSelectionCoroutine(sessionName));
        }

        /// <summary>
        /// Host flow that starts with room scan BEFORE network session creation.
        /// This is the new flow when role selection is used.
        /// </summary>
        private IEnumerator HostFlowWithRoleSelectionCoroutine()
        {
            Log("Starting HOST flow (with role selection)...");

            // Step 1: Room Scan (before network)
            SetState(StartupState.CheckingRoomScan);
            m_statusMessage = "Checking room scan...";
            m_progress = 0.1f;
            UpdateUI();

            yield return new WaitForSeconds(0.5f);

            // Ensure MRUK is available
            m_mruk = MRUK.Instance;

            // Check if room is already scanned
            bool hasRoomScan = false;
            if (m_mruk != null && m_mruk.GetCurrentRoom() != null)
            {
                hasRoomScan = true;
                Log($"Room scan already loaded - Room: {m_mruk.GetCurrentRoom().name}");
            }

            if (!hasRoomScan)
            {
                SetState(StartupState.PromptingRoomScan);
                m_statusMessage = "Room scan required. Starting Space Setup...";
                UpdateUI();

                // Find RoomScanManager
                m_roomScanManager = FindAnyObjectByType<RoomScanManager>();

                if (m_roomScanManager != null)
                {
                    m_roomScanManager.RequestRoomScan();

                    // Wait for scan to complete
                    float timeout = m_config.StepTimeout;
                    float elapsed = 0f;
                    while (!m_roomScanManager.IsSceneLoaded && elapsed < timeout)
                    {
                        yield return new WaitForSeconds(0.5f);
                        elapsed += 0.5f;

                        if (elapsed % 5f < 0.5f)
                        {
                            Log($"Waiting for room scan... ({elapsed:F1}s / {timeout}s)");
                        }
                    }

                    if (!m_roomScanManager.IsSceneLoaded)
                    {
                        SetError("Room scan failed or was cancelled. Please try again.");
                        yield break;
                    }
                }
                else if (m_mruk != null)
                {
                    Log("Loading scene from device via MRUK...");
                    var loadTask = m_mruk.LoadSceneFromDevice();
                    yield return new WaitUntil(() => loadTask.IsCompleted);

                    if (loadTask.Result != MRUK.LoadDeviceResult.Success)
                    {
                        SetError($"Failed to load room scan: {loadTask.Result}");
                        yield break;
                    }
                    Log("Scene loaded from device successfully");
                }
                else
                {
                    SetError("Neither RoomScanManager nor MRUK available for room scanning");
                    yield break;
                }
            }

            m_progress = 0.25f;
            Log("Room scan complete - now creating network session");

            // Step 2: Create Network Session
            SetState(StartupState.CreatingSession);
            m_statusMessage = "Creating game session...";
            UpdateUI();

            // Use SessionDiscoveryManager if available, otherwise fall back to FusionBootstrap
            if (m_sessionDiscovery != null)
            {
                var createTask = m_sessionDiscovery.CreateSessionAsHost();
                yield return new WaitUntil(() => createTask.IsCompleted);

                if (!createTask.Result)
                {
                    SetError("Failed to create game session");
                    yield break;
                }

                m_networkRunner = m_sessionDiscovery.Runner;
                Log($"Session created: {m_sessionDiscovery.CurrentSessionName}");
            }
            else if (m_fusionBootstrap != null)
            {
                // Use FusionBootstrap to start
                m_fusionBootstrap.StartSharedClient();
                
                // Wait for network to be ready
                float timeout = m_config.NetworkTimeout;
                float elapsed = 0f;
                while (m_networkRunner == null && elapsed < timeout)
                {
                    yield return new WaitForSeconds(0.5f);
                    elapsed += 0.5f;
                }

                if (m_networkRunner == null)
                {
                    SetError("Failed to create network session");
                    yield break;
                }
            }
            else
            {
                SetError("No SessionDiscoveryManager or FusionBootstrap found");
                yield break;
            }

            m_progress = 0.4f;

            // The rest of the host flow will continue via OnNetworkSessionReady
            // But since we already know we're the host, we can proceed
            
            // Wait for colocation to be established
            m_ssaManager = FindAnyObjectByType<SharedSpatialAnchorManager>();
            
            float sessionTimeout = m_config.NetworkTimeout;
            float sessionElapsed = 0f;
            while ((m_ssaManager == null || !m_ssaManager.IsColocationEstablished) && sessionElapsed < sessionTimeout)
            {
                yield return new WaitForSeconds(0.5f);
                sessionElapsed += 0.5f;
                
                if (m_ssaManager == null)
                {
                    m_ssaManager = FindAnyObjectByType<SharedSpatialAnchorManager>();
                }
            }

            if (m_ssaManager == null || !m_ssaManager.IsColocationEstablished)
            {
                SetError("Failed to create colocation session");
                yield break;
            }

            m_progress = 0.6f;
            Log($"Colocation session established. GroupId: {m_ssaManager.SharedAnchorGroupId}");

            // Register host calibration
            var colocationManager = FindAnyObjectByType<ColocationManager>();
            if (colocationManager != null && !colocationManager.IsAligned)
            {
                colocationManager.RegisterHostCalibration(Vector3.zero);
                Log("Host calibration registered");
            }

            // Step 3: Share Room
            if (m_config.EnableRoomSharing)
            {
                SetState(StartupState.SharingRoom);
                m_statusMessage = "Sharing room with other players...";
                UpdateUI();

                m_roomSharing = FindAnyObjectByType<RoomSharingMotif>();

                float roomWaitTime = 0f;
                float maxRoomWait = 10f;
                while (m_roomSharing == null && roomWaitTime < maxRoomWait)
                {
                    yield return new WaitForSeconds(0.5f);
                    roomWaitTime += 0.5f;
                    m_roomSharing = FindAnyObjectByType<RoomSharingMotif>();
                }

                if (m_roomSharing != null)
                {
                    float timeout = m_config.RoomLoadTimeout;
                    float elapsed = 0f;
                    while (!m_roomSharing.IsRoomShared && elapsed < timeout)
                    {
                        yield return new WaitForSeconds(0.5f);
                        elapsed += 0.5f;
                    }

                    if (m_roomSharing.IsRoomShared)
                    {
                        m_progress = 0.8f;
                        Log("Room shared successfully");
                    }
                    else
                    {
                        Log("WARNING: Room sharing timed out");
                    }
                }
            }

            m_progress = 0.9f;

            // Step 4: Ready!
            CompleteStartup();
        }

        /// <summary>
        /// Client flow that joins a specific session selected by the user.
        /// </summary>
        private IEnumerator ClientFlowWithRoleSelectionCoroutine(string sessionName)
        {
            Log($"Starting CLIENT flow - joining session: {sessionName}");

            // Step 1: Join Session
            SetState(StartupState.JoiningSession);
            m_statusMessage = $"Joining {sessionName}...";
            m_progress = 0.1f;
            UpdateUI();

            // Use SessionDiscoveryManager to join
            if (m_sessionDiscovery != null)
            {
                var joinTask = m_sessionDiscovery.JoinSession(sessionName);
                yield return new WaitUntil(() => joinTask.IsCompleted);

                if (!joinTask.Result)
                {
                    SetError($"Failed to join session: {sessionName}");
                    yield break;
                }

                m_networkRunner = m_sessionDiscovery.Runner;
                Log($"Joined session: {sessionName}");
            }
            else if (m_fusionBootstrap != null)
            {
                // Set the room name and start
                m_fusionBootstrap.DefaultRoomName = sessionName;
                m_fusionBootstrap.StartSharedClient();

                // Wait for network to be ready
                float timeout = m_config.NetworkTimeout;
                float elapsed = 0f;
                while (m_networkRunner == null && elapsed < timeout)
                {
                    yield return new WaitForSeconds(0.5f);
                    elapsed += 0.5f;
                }

                if (m_networkRunner == null)
                {
                    SetError("Failed to join network session");
                    yield break;
                }
            }
            else
            {
                SetError("No SessionDiscoveryManager or FusionBootstrap found");
                yield break;
            }

            m_progress = 0.25f;

            // Step 2: Wait for Anchor
            SetState(StartupState.WaitingForAnchor);
            m_statusMessage = "Waiting for host anchor...";
            UpdateUI();

            m_ssaManager = FindAnyObjectByType<SharedSpatialAnchorManager>();

            float anchorTimeout = m_config.AnchorWaitTimeout;
            float anchorElapsed = 0f;
            while ((m_ssaManager == null || !m_ssaManager.IsColocationEstablished) && anchorElapsed < anchorTimeout)
            {
                yield return new WaitForSeconds(0.5f);
                anchorElapsed += 0.5f;
                
                if (m_ssaManager == null)
                {
                    m_ssaManager = FindAnyObjectByType<SharedSpatialAnchorManager>();
                }
            }

            if (m_ssaManager == null || !m_ssaManager.IsColocationEstablished)
            {
                SetError("Failed to discover host anchor");
                yield break;
            }

            m_progress = 0.4f;
            Log($"Anchor discovered. GroupId: {m_ssaManager.SharedAnchorGroupId}");

            // Step 3: Localize Anchor
            SetState(StartupState.LocalizingAnchor);
            m_statusMessage = "Aligning to shared space...";
            UpdateUI();

            var colocationManager = FindAnyObjectByType<ColocationManager>();
            if (colocationManager != null)
            {
                float timeout = m_config.AnchorWaitTimeout;
                float elapsed = 0f;
                while (!colocationManager.IsAligned && elapsed < timeout)
                {
                    yield return new WaitForSeconds(0.5f);
                    elapsed += 0.5f;
                }

                if (!colocationManager.IsAligned)
                {
                    SetError("Failed to align to shared space");
                    yield break;
                }
            }

            m_progress = 0.6f;
            Log("Anchor localized and aligned");

            // Step 4: Load Room
            if (m_config.EnableRoomSharing)
            {
                SetState(StartupState.LoadingRoom);
                m_statusMessage = "Loading shared room...";
                UpdateUI();

                m_roomSharing = FindAnyObjectByType<RoomSharingMotif>();

                float roomWaitTime = 0f;
                float maxRoomWait = 10f;
                while (m_roomSharing == null && roomWaitTime < maxRoomWait)
                {
                    yield return new WaitForSeconds(0.5f);
                    roomWaitTime += 0.5f;
                    m_roomSharing = FindAnyObjectByType<RoomSharingMotif>();
                }

                if (m_roomSharing != null)
                {
                    float timeout = m_config.RoomLoadTimeout;
                    float elapsed = 0f;
                    while (!m_roomSharing.IsRoomLoaded && elapsed < timeout)
                    {
                        yield return new WaitForSeconds(0.5f);
                        elapsed += 0.5f;
                    }

                    if (m_roomSharing.IsRoomLoaded)
                    {
                        m_progress = 0.8f;
                        Log("Room loaded successfully");
                    }
                    else
                    {
                        Log("WARNING: Room loading timed out");
                        EnableFallbackColliders();
                    }
                }
                else
                {
                    Log("WARNING: RoomSharingMotif not found");
                    EnableFallbackColliders();
                }
            }

            m_progress = 0.9f;

            // Step 5: Ready!
            CompleteStartup();
        }

        #endregion

        #region Network Events

        private void OnNetworkSessionReady(NetworkRunner runner)
        {
            m_networkRunner = runner;
            m_isHost = runner.IsSharedModeMasterClient;

            Log($"Network session ready - IsHost: {m_isHost}");

            // If role was already selected, the flow is already running
            if (m_roleSelected)
            {
                Log("Role already selected - flow is already in progress");
                return;
            }

            // Find required components
            m_mruk = MRUK.Instance;
            m_roomScanManager = FindAnyObjectByType<RoomScanManager>();
            m_ssaManager = FindAnyObjectByType<SharedSpatialAnchorManager>();
            m_roomSharing = FindAnyObjectByType<RoomSharingMotif>();

            SubscribeToEvents();

            // Start the appropriate flow (legacy path when role selection is disabled)
            if (m_isHost)
            {
                StartCoroutine(HostFlowCoroutine());
            }
            else
            {
                StartCoroutine(ClientFlowCoroutine());
            }
        }

        private void SubscribeToEvents()
        {
            if (m_roomScanManager != null)
            {
                m_roomScanManager.OnSceneLoaded += OnRoomScanComplete;
                m_roomScanManager.OnScanFailed += OnRoomScanFailed;
            }

            if (m_ssaManager != null)
            {
                m_ssaManager.OnColocationSessionEstablished += OnColocationEstablished;
            }

            if (m_roomSharing != null)
            {
                m_roomSharing.OnRoomShared += OnRoomShared;
                m_roomSharing.OnRoomLoaded += OnRoomLoaded;
                m_roomSharing.OnRoomSharingFailed += OnRoomSharingFailed;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (m_roomScanManager != null)
            {
                m_roomScanManager.OnSceneLoaded -= OnRoomScanComplete;
                m_roomScanManager.OnScanFailed -= OnRoomScanFailed;
            }

            if (m_ssaManager != null)
            {
                m_ssaManager.OnColocationSessionEstablished -= OnColocationEstablished;
            }

            if (m_roomSharing != null)
            {
                m_roomSharing.OnRoomShared -= OnRoomShared;
                m_roomSharing.OnRoomLoaded -= OnRoomLoaded;
                m_roomSharing.OnRoomSharingFailed -= OnRoomSharingFailed;
            }
        }

        #endregion

        #region Host Flow

        private IEnumerator HostFlowCoroutine()
        {
            Log("Starting HOST flow...");

            // Step 1: Check Room Scan
            SetState(StartupState.CheckingRoomScan);
            m_statusMessage = "Checking room scan...";
            UpdateUI();

            yield return new WaitForSeconds(0.5f);

            // Ensure MRUK is available
            if (m_mruk == null)
            {
                m_mruk = MRUK.Instance;
            }

            // Check if room is already scanned
            bool hasRoomScan = false;
            if (m_mruk != null && m_mruk.GetCurrentRoom() != null)
            {
                hasRoomScan = true;
                Log($"Room scan already loaded - Room: {m_mruk.GetCurrentRoom().name}");
            }
            else if (m_roomScanManager != null && m_roomScanManager.IsSceneLoaded)
            {
                hasRoomScan = true;
                Log("Room scan loaded via RoomScanManager");
            }

            if (!hasRoomScan)
            {
                if (m_config.AutoPromptRoomScan)
                {
                    SetState(StartupState.PromptingRoomScan);
                    m_statusMessage = "Room scan required. Starting Space Setup...";
                    UpdateUI();

                    // Request room scan
                    if (m_roomScanManager != null)
                    {
                        m_roomScanManager.RequestRoomScan();
                        
                        // Wait for scan to complete
                        float timeout = m_config.StepTimeout;
                        float elapsed = 0f;
                        while (!m_roomScanManager.IsSceneLoaded && elapsed < timeout)
                        {
                            yield return new WaitForSeconds(0.5f);
                            elapsed += 0.5f;
                            
                            if (elapsed % 5f < 0.5f)
                            {
                                Log($"Waiting for room scan... ({elapsed:F1}s / {timeout}s)");
                            }
                        }

                        if (!m_roomScanManager.IsSceneLoaded)
                        {
                            SetError("Room scan failed or was cancelled. Please try again.");
                            yield break;
                        }
                    }
                    else if (m_mruk != null)
                    {
                        // Try loading directly via MRUK
                        Log("Loading scene from device via MRUK...");
                        var loadTask = m_mruk.LoadSceneFromDevice();
                        yield return new WaitUntil(() => loadTask.IsCompleted);
                        
                        if (loadTask.Result != MRUK.LoadDeviceResult.Success)
                        {
                            SetError($"Failed to load room scan: {loadTask.Result}");
                            yield break;
                        }
                        Log("Scene loaded from device successfully");
                    }
                    else
                    {
                        SetError("Neither RoomScanManager nor MRUK available for room scanning");
                        yield break;
                    }
                }
                else
                {
                    SetState(StartupState.PromptingRoomScan);
                    m_statusMessage = "Room scan required. Please scan your room.";
                    UpdateUI();
                    
                    // Wait for manual scan with timeout
                    float timeout = m_config.StepTimeout;
                    float elapsed = 0f;
                    while (elapsed < timeout)
                    {
                        if ((m_mruk != null && m_mruk.GetCurrentRoom() != null) ||
                            (m_roomScanManager != null && m_roomScanManager.IsSceneLoaded))
                        {
                            break;
                        }
                        yield return new WaitForSeconds(0.5f);
                        elapsed += 0.5f;
                    }

                    if (!((m_mruk != null && m_mruk.GetCurrentRoom() != null) ||
                          (m_roomScanManager != null && m_roomScanManager.IsSceneLoaded)))
                    {
                        SetError("Room scan timed out. Please scan your room and restart.");
                        yield break;
                    }
                }
            }

            m_progress = 0.2f;
            Log("Room scan complete");

            // Step 2: Creating Session (handled by network building blocks)
            SetState(StartupState.CreatingSession);
            m_statusMessage = "Creating game session...";
            UpdateUI();

            // Dynamically find SSA manager if not cached
            if (m_ssaManager == null)
            {
                m_ssaManager = FindAnyObjectByType<SharedSpatialAnchorManager>();
            }

            // Wait for colocation to be established with timeout
            float sessionTimeout = m_config.NetworkTimeout;
            float sessionElapsed = 0f;
            while ((m_ssaManager == null || !m_ssaManager.IsColocationEstablished) && sessionElapsed < sessionTimeout)
            {
                yield return new WaitForSeconds(0.5f);
                sessionElapsed += 0.5f;
                
                if (m_ssaManager == null)
                {
                    m_ssaManager = FindAnyObjectByType<SharedSpatialAnchorManager>();
                }
                
                if (sessionElapsed % 5f < 0.5f)
                {
                    Log($"Waiting for colocation session... ({sessionElapsed:F1}s / {sessionTimeout}s)");
                }
            }

            if (m_ssaManager == null || !m_ssaManager.IsColocationEstablished)
            {
                SetError("Failed to create colocation session. Please check your network connection.");
                yield break;
            }

            m_progress = 0.4f;
            Log($"Colocation session established. GroupId: {m_ssaManager.SharedAnchorGroupId}");

            // Register host calibration with ColocationManager
            var colocationManager = FindAnyObjectByType<ColocationManager>();
            if (colocationManager != null && !colocationManager.IsAligned)
            {
                // Host is aligned at their current position (they define the origin)
                colocationManager.RegisterHostCalibration(Vector3.zero);
                Log("Host calibration registered");
            }

            // Step 3: Share Room (if enabled)
            if (m_config.EnableRoomSharing)
            {
                SetState(StartupState.SharingRoom);
                m_statusMessage = "Sharing room with other players...";
                UpdateUI();

                // Dynamically find RoomSharingMotif if not cached
                if (m_roomSharing == null)
                {
                    m_roomSharing = FindAnyObjectByType<RoomSharingMotif>();
                }

                // Wait for RoomSharingMotif to spawn
                float roomWaitTime = 0f;
                float maxRoomWait = 10f;
                while (m_roomSharing == null && roomWaitTime < maxRoomWait)
                {
                    yield return new WaitForSeconds(0.5f);
                    roomWaitTime += 0.5f;
                    m_roomSharing = FindAnyObjectByType<RoomSharingMotif>();
                }

                if (m_roomSharing != null)
                {
                    // Subscribe to events if we just found it
                    m_roomSharing.OnRoomShared -= OnRoomShared;
                    m_roomSharing.OnRoomShared += OnRoomShared;
                    m_roomSharing.OnRoomSharingFailed -= OnRoomSharingFailed;
                    m_roomSharing.OnRoomSharingFailed += OnRoomSharingFailed;

                    // Wait for room to be shared (with timeout)
                    float timeout = m_config.RoomLoadTimeout;
                    float elapsed = 0f;
                    while (!m_roomSharing.IsRoomShared && elapsed < timeout)
                    {
                        yield return new WaitForSeconds(0.5f);
                        elapsed += 0.5f;
                        
                        if (elapsed % 5f < 0.5f)
                        {
                            Log($"Sharing room... ({elapsed:F1}s / {timeout}s)");
                        }
                    }

                    if (m_roomSharing.IsRoomShared)
                    {
                        m_progress = 0.8f;
                        Log("Room shared successfully - clients can now load it");
                    }
                    else
                    {
                        Log($"WARNING: Room sharing timed out after {timeout}s. Continuing without shared room.");
                        m_progress = 0.8f;
                    }
                }
                else
                {
                    Log("WARNING: RoomSharingMotif not found. Room sharing skipped.");
                    m_progress = 0.8f;
                }
            }
            else
            {
                Log("Room sharing disabled in config");
                m_progress = 0.8f;
            }

            // Step 4: Ready
            CompleteStartup();
        }

        #endregion

        #region Client Flow

        private IEnumerator ClientFlowCoroutine()
        {
            Log("Starting CLIENT flow...");

            // Step 1: Joining Session (already connected)
            SetState(StartupState.JoiningSession);
            m_statusMessage = "Connected to game session...";
            UpdateUI();

            yield return new WaitForSeconds(0.5f);
            m_progress = 0.2f;

            // Step 2: Wait for Anchor
            SetState(StartupState.WaitingForAnchor);
            m_statusMessage = "Waiting for host to share anchor...";
            UpdateUI();

            // Dynamically find SSA manager if not cached
            if (m_ssaManager == null)
            {
                m_ssaManager = FindAnyObjectByType<SharedSpatialAnchorManager>();
            }

            // Wait for colocation to be established (with timeout)
            float anchorTimeout = m_config.AnchorWaitTimeout;
            float anchorElapsed = 0f;
            while ((m_ssaManager == null || !m_ssaManager.IsColocationEstablished) && anchorElapsed < anchorTimeout)
            {
                yield return new WaitForSeconds(0.5f);
                anchorElapsed += 0.5f;
                
                // Keep trying to find the manager
                if (m_ssaManager == null)
                {
                    m_ssaManager = FindAnyObjectByType<SharedSpatialAnchorManager>();
                }
            }

            if (m_ssaManager == null || !m_ssaManager.IsColocationEstablished)
            {
                SetError("Failed to discover host session. Please ensure the host has started.");
                yield break;
            }

            m_progress = 0.4f;
            Log($"Anchor discovered. GroupId: {m_ssaManager.SharedAnchorGroupId}");

            // Step 3: Localizing Anchor
            SetState(StartupState.LocalizingAnchor);
            m_statusMessage = "Aligning to shared space...";
            UpdateUI();

            // Wait for colocation manager to complete alignment
            var colocationManager = FindAnyObjectByType<ColocationManager>();
            if (colocationManager != null)
            {
                float timeout = m_config.AnchorWaitTimeout;
                float elapsed = 0f;
                while (!colocationManager.IsAligned && elapsed < timeout)
                {
                    yield return new WaitForSeconds(0.5f);
                    elapsed += 0.5f;
                    Log($"Waiting for alignment... ({elapsed:F1}s / {timeout}s)");
                }

                if (!colocationManager.IsAligned)
                {
                    SetError("Failed to align to shared space. Anchor may not be visible from your location.");
                    yield break;
                }
            }
            else
            {
                Log("Warning: ColocationManager not found, skipping alignment check");
            }

            m_progress = 0.6f;
            Log("Anchor localized and aligned");

            // Step 4: Load Room (if room sharing is enabled)
            if (m_config.EnableRoomSharing)
            {
                SetState(StartupState.LoadingRoom);
                m_statusMessage = "Loading shared room...";
                UpdateUI();

                // Dynamically find RoomSharingMotif if not cached (it may spawn late)
                if (m_roomSharing == null)
                {
                    m_roomSharing = FindAnyObjectByType<RoomSharingMotif>();
                }

                // Wait for RoomSharingMotif to spawn
                float roomWaitTime = 0f;
                float maxRoomWait = 10f;
                while (m_roomSharing == null && roomWaitTime < maxRoomWait)
                {
                    yield return new WaitForSeconds(0.5f);
                    roomWaitTime += 0.5f;
                    m_roomSharing = FindAnyObjectByType<RoomSharingMotif>();
                    Log($"Waiting for RoomSharingMotif to spawn... ({roomWaitTime:F1}s)");
                }

                if (m_roomSharing != null)
                {
                    // Wait for room to be loaded (with timeout)
                    float timeout = m_config.RoomLoadTimeout;
                    float elapsed = 0f;
                    while (!m_roomSharing.IsRoomLoaded && elapsed < timeout)
                    {
                        yield return new WaitForSeconds(0.5f);
                        elapsed += 0.5f;
                        
                        // Log progress every 5 seconds
                        if (elapsed % 5f < 0.5f)
                        {
                            Log($"Loading shared room... ({elapsed:F1}s / {timeout}s) - IsRoomShared: {m_roomSharing.IsRoomShared}");
                        }
                    }

                    if (m_roomSharing.IsRoomLoaded)
                    {
                        m_progress = 0.8f;
                        Log("Room loaded successfully - colliders should be active");
                    }
                    else
                    {
                        // This is a critical failure - we need the room mesh for proper gameplay
                        Log($"WARNING: Room loading timed out after {timeout}s. IsRoomShared={m_roomSharing.IsRoomShared}");
                        m_statusMessage = "Room loading incomplete - using fallback";
                        
                        // Try to enable GlobalMesh colliders from local MRUK as fallback
                        EnableFallbackColliders();
                        m_progress = 0.8f;
                    }
                }
                else
                {
                    Log("WARNING: RoomSharingMotif not found after waiting. Using local room data.");
                    EnableFallbackColliders();
                    m_progress = 0.8f;
                }
            }
            else
            {
                Log("Room sharing disabled in config");
                m_progress = 0.8f;
            }

            // Step 5: Ready
            CompleteStartup();
        }

        /// <summary>
        /// Fallback method to enable colliders from local MRUK when shared room loading fails.
        /// </summary>
        private void EnableFallbackColliders()
        {
            try
            {
                var mruk = MRUK.Instance;
                if (mruk != null)
                {
                    var room = mruk.GetCurrentRoom();
                    if (room != null)
                    {
                        var globalMesh = room.GlobalMeshAnchor;
                        if (globalMesh != null)
                        {
                            var meshRenderer = globalMesh.GetComponent<MeshRenderer>();
                            if (meshRenderer != null) meshRenderer.enabled = false;

                            var meshCollider = globalMesh.GetComponent<MeshCollider>();
                            if (meshCollider != null)
                            {
                                meshCollider.enabled = true;
                                Log("Fallback: Enabled local GlobalMesh collider");
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log($"Fallback collider setup failed: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers

        private void OnRoomScanComplete()
        {
            Log("Room scan completed (event)");
        }

        private void OnRoomScanFailed(string error)
        {
            Log($"Room scan failed: {error}");
            if (m_currentState == StartupState.PromptingRoomScan || 
                m_currentState == StartupState.CheckingRoomScan)
            {
                SetError($"Room scan failed: {error}");
            }
        }

        private void OnColocationEstablished(Guid groupId)
        {
            Log($"Colocation session established: {groupId}");
        }

        private void OnRoomShared()
        {
            Log("Room shared (event)");
        }

        private void OnRoomLoaded()
        {
            Log("Room loaded (event)");
        }

        private void OnRoomSharingFailed(string error)
        {
            Log($"Room sharing failed: {error}");
            // Don't fail completely, just log and continue
        }

        #endregion

        #region State Management

        private void SetState(StartupState newState)
        {
            if (m_currentState == newState) return;

            Log($"State transition: {m_currentState} → {newState}");
            m_currentState = newState;
            m_stateStartTime = Time.time;

            OnStateChanged?.Invoke(newState);
            UpdateUI();
        }

        private void SetError(string error)
        {
            m_errorMessage = error;
            m_statusMessage = $"Error: {error}";
            Log($"ERROR: {error}");

            SetState(StartupState.Error);
            OnError?.Invoke(error);
            UpdateUI();
        }

        private void CompleteStartup()
        {
            m_progress = 1f;
            m_statusMessage = "Ready!";
            m_startupComplete = true;

            SetState(StartupState.Ready);
            Log("Startup complete - avatar spawning is now allowed");

            OnStartupComplete?.Invoke();
            UpdateUI();

            // Hide modal after delay
            if (m_modalUI != null)
            {
                StartCoroutine(HideModalAfterDelay());
            }
        }

        private IEnumerator HideModalAfterDelay()
        {
            yield return new WaitForSeconds(m_config.ModalHideDelay);
            if (m_modalUI != null)
            {
                m_modalUI.Hide();
            }
        }

        private float GetTimeoutForState(StartupState state)
        {
            return state switch
            {
                StartupState.RoleSelection => float.MaxValue, // No timeout for role selection
                StartupState.WaitingForAnchor => m_config.AnchorWaitTimeout,
                StartupState.LocalizingAnchor => m_config.AnchorWaitTimeout,
                StartupState.SharingRoom => m_config.RoomLoadTimeout,
                StartupState.LoadingRoom => m_config.RoomLoadTimeout,
                StartupState.CreatingSession => m_config.NetworkTimeout,
                StartupState.JoiningSession => m_config.NetworkTimeout,
                _ => m_config.StepTimeout
            };
        }

        private void HandleTimeout()
        {
            // Don't timeout during role selection
            if (m_currentState == StartupState.RoleSelection)
            {
                return;
            }

            string timeoutMessage = m_currentState switch
            {
                StartupState.CheckingRoomScan => "Room scan check timed out",
                StartupState.PromptingRoomScan => "Room scan timed out",
                StartupState.CreatingSession => "Creating session timed out",
                StartupState.JoiningSession => "Joining session timed out",
                StartupState.WaitingForAnchor => "Waiting for anchor timed out",
                StartupState.LocalizingAnchor => "Anchor localization timed out",
                StartupState.SharingRoom => "Room sharing timed out",
                StartupState.LoadingRoom => "Room loading timed out",
                _ => "Operation timed out"
            };

            SetError(timeoutMessage);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually start the host flow. Use when auto-start is disabled.
        /// </summary>
        public void StartHostFlow()
        {
            if (m_networkRunner == null)
            {
                Log("Cannot start host flow - network not ready");
                return;
            }

            m_isHost = true;
            StartCoroutine(HostFlowCoroutine());
        }

        /// <summary>
        /// Manually start the client flow. Use when auto-start is disabled.
        /// </summary>
        public void StartClientFlow()
        {
            if (m_networkRunner == null)
            {
                Log("Cannot start client flow - network not ready");
                return;
            }

            m_isHost = false;
            StartCoroutine(ClientFlowCoroutine());
        }

        /// <summary>
        /// Retry the current step after an error.
        /// </summary>
        public void RetryCurrentStep()
        {
            if (m_currentState != StartupState.Error)
            {
                Log("No error to retry");
                return;
            }

            Log("Retrying startup...");
            m_errorMessage = "";

            if (m_isHost)
            {
                StartCoroutine(HostFlowCoroutine());
            }
            else
            {
                StartCoroutine(ClientFlowCoroutine());
            }
        }

        /// <summary>
        /// Request a room scan from the user (host only).
        /// </summary>
        public void RequestRoomScan()
        {
            if (!m_isHost)
            {
                Log("Only host can request room scan");
                return;
            }

            if (m_roomScanManager != null)
            {
                m_roomScanManager.RequestRoomScan();
            }
        }

        #endregion

        #region UI

        private void UpdateUI()
        {
            if (m_modalUI != null)
            {
                m_modalUI.UpdateState(m_currentState, m_statusMessage, m_progress, m_isHost);
            }
        }

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (m_config != null && m_config.ShowDebugLogs)
            {
                Debug.Log($"[GameStartup] {message}");
            }
        }

        #endregion
    }
}
#endif
