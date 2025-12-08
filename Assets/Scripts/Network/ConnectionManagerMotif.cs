// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;

namespace MRMotifs.SharedActivities.Network
{
    /// <summary>
    /// Monitors the network connection and handles application pause/resume.
    /// Automatically restarts the game if the connection is lost during standby.
    /// </summary>
    public class ConnectionManagerMotif : MonoBehaviour, INetworkRunnerCallbacks
    {
        private NetworkRunner m_runner;

        private void Awake()
        {
            // Find existing runner
            m_runner = FindAnyObjectByType<NetworkRunner>();
        }

        private void Start()
        {
            if (m_runner != null)
            {
                m_runner.AddCallbacks(this);
            }
            else
            {
                StartCoroutine(FindRunner());
            }
            
            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator FindRunner()
        {
            while (m_runner == null)
            {
                m_runner = FindAnyObjectByType<NetworkRunner>();
                if (m_runner != null)
                {
                    m_runner.AddCallbacks(this);
                }
                yield return new WaitForSeconds(1f);
            }
        }

        private void OnDestroy()
        {
            if (m_runner != null)
            {
                m_runner.RemoveCallbacks(this);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            Debug.Log($"[ConnectionManager] OnApplicationPause: {pauseStatus}");
            
            if (!pauseStatus) // Resuming
            {
                StartCoroutine(CheckConnectionAfterResume());
            }
        }

        private IEnumerator CheckConnectionAfterResume()
        {
            // Wait a moment for network stack to wake up
            yield return new WaitForSeconds(1.0f);

            if (m_runner == null || !m_runner.IsRunning)
            {
                Debug.LogWarning("[ConnectionManager] Network disconnected after resume. Reloading scene...");
                RestartGame();
            }
            else
            {
                Debug.Log("[ConnectionManager] Network still connected after resume.");
            }
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Debug.LogWarning($"[ConnectionManager] Disconnected from server: {reason}. Reloading scene...");
            RestartGame();
        }
        
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
             Debug.LogWarning($"[ConnectionManager] Connection failed: {reason}. Reloading scene...");
             RestartGame();
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.LogWarning($"[ConnectionManager] Runner shutdown: {shutdownReason}. Reloading scene...");
            RestartGame();
        }

        private void RestartGame()
        {
            // Remove callbacks to prevent multiple restarts
            if (m_runner != null)
            {
                m_runner.RemoveCallbacks(this);
            }
            
            // Destroy the runner to ensure clean state
            if (m_runner != null)
            {
                Destroy(m_runner.gameObject);
            }

            // Reload the current scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        #region INetworkRunnerCallbacks Empty Implementations
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        #endregion
    }
}
#endif
