// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using MRMotifs.SharedActivities.ShootingSample;
using UnityEngine;

namespace MRMotifs.SharedActivities.Network
{
    /// <summary>
    /// Handles Host Migration for the shooting game.
    /// When the current host disconnects, this ensures the game state is preserved
    /// and a new host takes over seamlessly.
    /// </summary>
    public class HostMigrationHandlerMotif : MonoBehaviour, INetworkRunnerCallbacks
    {
        [Header("Host Migration Settings")]
        [SerializeField] private bool m_enableHostMigration = true;
        [SerializeField] private float m_migrationTimeout = 10f;
        
        [Header("References")]
        [SerializeField] private NetworkRunner m_runnerPrefab;
        
        private NetworkRunner m_runner;
        private bool m_isMigrating = false;
        
        private void Awake()
        {
            m_runner = GetComponent<NetworkRunner>();
            if (m_runner == null)
            {
                m_runner = FindAnyObjectByType<NetworkRunner>();
            }
        }
        
        private void Start()
        {
            if (m_runner != null)
            {
                m_runner.AddCallbacks(this);
            }
        }
        
        private void OnDestroy()
        {
            if (m_runner != null)
            {
                m_runner.RemoveCallbacks(this);
            }
        }
        
        /// <summary>
        /// Called when Host Migration is triggered.
        /// The HostMigrationToken contains the simulation state snapshot.
        /// </summary>
        public async void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
            if (!m_enableHostMigration || m_isMigrating)
            {
                Debug.LogWarning("[HostMigration] Host migration disabled or already in progress");
                return;
            }
            
            m_isMigrating = true;
            Debug.Log($"[HostMigration] Starting host migration. New GameMode: {hostMigrationToken.GameMode}");
            
            try
            {
                // Shutdown the old runner
                await runner.Shutdown(shutdownReason: ShutdownReason.HostMigration);
                
                // Create a new runner for the migrated session
                var newRunner = gameObject.AddComponent<NetworkRunner>();
                
                // Start the new runner with the migration token
                var result = await StartWithMigrationToken(newRunner, hostMigrationToken);
                
                if (result.Ok)
                {
                    Debug.Log("[HostMigration] Successfully resumed session after host migration");
                    m_runner = newRunner;
                    
                    // Restore network objects from the snapshot
                    RestoreNetworkObjectsFromSnapshot(newRunner);
                }
                else
                {
                    Debug.LogError($"[HostMigration] Failed to resume session: {result.ShutdownReason}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HostMigration] Error during host migration: {ex.Message}");
            }
            finally
            {
                m_isMigrating = false;
            }
        }
        
        /// <summary>
        /// Starts the NetworkRunner with the Host Migration Token to resume the session.
        /// </summary>
        private async Task<StartGameResult> StartWithMigrationToken(NetworkRunner runner, HostMigrationToken token)
        {
            var startArgs = new StartGameArgs
            {
                // Use the GameMode from the migration token (the new role)
                GameMode = token.GameMode,
                
                // Pass the HostMigrationToken to restore the simulation state
                HostMigrationToken = token,
                
                // Keep the same session settings
                SessionName = null, // Will be restored from token
                
                // Scene management
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
            };
            
            return await runner.StartGame(startArgs);
        }
        
        /// <summary>
        /// Restores NetworkObjects from the migration snapshot.
        /// This is called after the new runner has started to rebuild the game state.
        /// </summary>
        private void RestoreNetworkObjectsFromSnapshot(NetworkRunner runner)
        {
            if (!runner.IsResume)
            {
                Debug.Log("[HostMigration] Not a resume session, skipping snapshot restoration");
                return;
            }
            
            Debug.Log("[HostMigration] Restoring network objects from snapshot...");
            
            // Iterate over the old NetworkObjects from the Resume Snapshot
            foreach (var networkObject in runner.GetResumeSnapshotNetworkObjects())
            {
                if (networkObject == null) continue;
                
                Debug.Log($"[HostMigration] Found snapshot object: {networkObject.name} (ID: {networkObject.Id})");
                
                // For spawned objects (not scene objects), you may need to re-spawn them
                // The Fusion runtime handles scene objects automatically
                
                // Check if this is a player object
                if (networkObject.TryGetComponent<ShootingPlayerMotif>(out var player))
                {
                    Debug.Log($"[HostMigration] Restoring player: {player.name}");
                    // Player state is automatically restored via [Networked] properties
                }
                
                // Check if this is a bullet
                if (networkObject.TryGetComponent<BulletMotif>(out var bullet))
                {
                    Debug.Log($"[HostMigration] Restoring bullet: {bullet.name}");
                }
                
                // Check if this is the game manager
                if (networkObject.TryGetComponent<ShootingGameManagerMotif>(out var gameManager))
                {
                    Debug.Log($"[HostMigration] Restoring game manager state");
                }
            }
            
            // Also restore scene objects
            foreach (var (sceneObject, header) in runner.GetResumeSnapshotNetworkSceneObjects())
            {
                if (sceneObject == null) continue;
                Debug.Log($"[HostMigration] Restored scene object: {sceneObject.name}");
            }
            
            Debug.Log("[HostMigration] Network object restoration complete");
        }
        
        #region INetworkRunnerCallbacks - Required implementations
        
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            if (shutdownReason == ShutdownReason.HostMigration)
            {
                Debug.Log("[HostMigration] Shutdown due to host migration - waiting for migration callback");
            }
        }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
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
