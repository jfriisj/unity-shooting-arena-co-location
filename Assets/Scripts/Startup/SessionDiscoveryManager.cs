// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using Meta.XR.Samples;

namespace MRMotifs.SharedActivities.Startup
{
    /// <summary>
    /// Manages session discovery and manual network session creation/joining.
    /// Replaces the automatic matchmaking with explicit host/client selection.
    /// 
    /// Host: Creates a new session with a unique room name
    /// Client: Lists available sessions and joins selected one
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class SessionDiscoveryManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        [Header("Session Settings")]
        [SerializeField] private string m_sessionPrefix = "ShootingGame_";
        [SerializeField] private int m_maxPlayersPerSession = 8;
        [SerializeField] private float m_sessionListRefreshInterval = 2f;

        [Header("References")]
        [SerializeField] private NetworkRunner m_runnerPrefab;

        // State
        private NetworkRunner m_runner;
        private List<SessionInfo> m_availableSessions = new List<SessionInfo>();
        private bool m_isRefreshing = false;
        private string m_currentSessionName;
        private bool m_isHost = false;

        // Events
        public event Action<List<SessionInfo>> SessionListChanged;
        public event Action<NetworkRunner> OnSessionJoined;
        public event Action<NetworkRunner> OnSessionCreated;
        public event Action<string> OnError;

        // Properties
        public bool IsHost => m_isHost;
        public string CurrentSessionName => m_currentSessionName;
        public List<SessionInfo> AvailableSessions => m_availableSessions;
        public NetworkRunner Runner => m_runner;

        private void Start()
        {
            // Find the runner prefab from the Auto Matchmaking building block if not assigned
            if (m_runnerPrefab == null)
            {
                var bootstrap = FindAnyObjectByType<FusionBootstrap>();
                if (bootstrap != null)
                {
                    m_runnerPrefab = bootstrap.RunnerPrefab;
                    Debug.Log("[SessionDiscovery] Found NetworkRunner prefab from FusionBootstrap");
                }
            }
        }

        #region Public API

        /// <summary>
        /// Create a new session as host.
        /// Generates a unique session name and starts the network.
        /// </summary>
        public async Task<bool> CreateSessionAsHost(string customSessionName = null)
        {
            m_isHost = true;
            
            // Generate unique session name if not provided
            m_currentSessionName = customSessionName ?? GenerateSessionName();
            
            Debug.Log($"[SessionDiscovery] Creating session as HOST: {m_currentSessionName}");

            try
            {
                // Create a new NetworkRunner
                m_runner = CreateRunner();

                var startGameArgs = new StartGameArgs
                {
                    GameMode = GameMode.Shared,
                    SessionName = m_currentSessionName,
                    PlayerCount = m_maxPlayersPerSession,
                    SceneManager = m_runner.GetComponent<NetworkSceneManagerDefault>()
                };

                var result = await m_runner.StartGame(startGameArgs);

                if (result.Ok)
                {
                    Debug.Log($"[SessionDiscovery] Session created successfully: {m_currentSessionName}");
                    OnSessionCreated?.Invoke(m_runner);
                    return true;
                }
                else
                {
                    Debug.LogError($"[SessionDiscovery] Failed to create session: {result.ShutdownReason}");
                    OnError?.Invoke($"Failed to create session: {result.ShutdownReason}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionDiscovery] Exception creating session: {ex.Message}");
                OnError?.Invoke($"Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Join an existing session by name.
        /// </summary>
        public async Task<bool> JoinSession(string sessionName)
        {
            m_isHost = false;
            m_currentSessionName = sessionName;

            Debug.Log($"[SessionDiscovery] Joining session as CLIENT: {sessionName}");

            try
            {
                // Create a new NetworkRunner
                m_runner = CreateRunner();

                var startGameArgs = new StartGameArgs
                {
                    GameMode = GameMode.Shared,
                    SessionName = sessionName,
                    SceneManager = m_runner.GetComponent<NetworkSceneManagerDefault>()
                };

                var result = await m_runner.StartGame(startGameArgs);

                if (result.Ok)
                {
                    Debug.Log($"[SessionDiscovery] Joined session successfully: {sessionName}");
                    OnSessionJoined?.Invoke(m_runner);
                    return true;
                }
                else
                {
                    Debug.LogError($"[SessionDiscovery] Failed to join session: {result.ShutdownReason}");
                    OnError?.Invoke($"Failed to join session: {result.ShutdownReason}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionDiscovery] Exception joining session: {ex.Message}");
                OnError?.Invoke($"Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Refresh the list of available sessions.
        /// </summary>
        public void RefreshSessionList()
        {
            if (m_isRefreshing) return;
            StartCoroutine(RefreshSessionListCoroutine());
        }

        /// <summary>
        /// Disconnect from current session.
        /// </summary>
        public async Task Disconnect()
        {
            if (m_runner != null && m_runner.IsRunning)
            {
                await m_runner.Shutdown();
                m_runner = null;
            }
            
            m_isHost = false;
            m_currentSessionName = null;
        }

        #endregion

        #region Private Methods

        private NetworkRunner CreateRunner()
        {
            NetworkRunner runner;

            if (m_runnerPrefab != null)
            {
                runner = Instantiate(m_runnerPrefab);
            }
            else
            {
                var runnerGO = new GameObject("NetworkRunner");
                runner = runnerGO.AddComponent<NetworkRunner>();
                runnerGO.AddComponent<NetworkSceneManagerDefault>();
            }

            runner.AddCallbacks(this);
            return runner;
        }

        private string GenerateSessionName()
        {
            // Generate a unique, readable session name
            // Format: ShootingGame_XXXX where XXXX is a random 4-character code
            string code = Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
            return $"{m_sessionPrefix}{code}";
        }

        private IEnumerator RefreshSessionListCoroutine()
        {
            m_isRefreshing = true;

            // Create a temporary runner to query sessions
            var queryRunner = CreateRunner();

            var startArgs = new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = "query_" + Guid.NewGuid().ToString().Substring(0, 8),
                SceneManager = queryRunner.GetComponent<NetworkSceneManagerDefault>()
            };

            // We need to connect briefly to get the session list
            // This is a limitation of Fusion - can't query sessions without connecting

            // For now, we'll use a different approach - just show instructions
            Debug.Log("[SessionDiscovery] Session discovery requires connecting to lobby. Using session name entry.");

            m_isRefreshing = false;
            
            // Clean up query runner
            if (queryRunner != null)
            {
                Destroy(queryRunner.gameObject);
            }

            yield return null;
        }

        #endregion

        #region INetworkRunnerCallbacks

        public void OnConnectedToServer(NetworkRunner runner)
        {
            Debug.Log("[SessionDiscovery] Connected to server");
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            Debug.LogError($"[SessionDiscovery] Connect failed: {reason}");
            OnError?.Invoke($"Connection failed: {reason}");
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            request.Accept();
        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) 
        {
            Debug.Log($"[SessionDiscovery] Disconnected: {reason}");
        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) 
        {
            Debug.Log($"[SessionDiscovery] Player joined: {player}");
        }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) 
        {
            Debug.Log($"[SessionDiscovery] Player left: {player}");
        }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            Debug.Log($"[SessionDiscovery] Session list updated: {sessionList.Count} sessions");
            
            // Filter to only show our game sessions
            m_availableSessions.Clear();
            foreach (var session in sessionList)
            {
                if (session.Name.StartsWith(m_sessionPrefix))
                {
                    m_availableSessions.Add(session);
                }
            }

            SessionListChanged?.Invoke(m_availableSessions);

            // Update UI if available
            var roleSelectionUI = FindAnyObjectByType<RoleSelectionModalUI>();
            if (roleSelectionUI != null)
            {
                roleSelectionUI.UpdateSessionList(m_availableSessions);
            }
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.Log($"[SessionDiscovery] Shutdown: {shutdownReason}");
        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

        #endregion
    }
}
#endif
