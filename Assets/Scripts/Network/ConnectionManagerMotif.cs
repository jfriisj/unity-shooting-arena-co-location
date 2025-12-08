// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace MRMotifs.Shooting.Network
{
    /// <summary>
    /// Monitors the network connection and handles application pause/resume.
    /// Automatically restarts the game if the connection is lost during standby.
    /// Converted from Photon Fusion to Unity NGO.
    /// </summary>
    public class ConnectionManagerMotif : MonoBehaviour
    {
        private static ConnectionManagerMotif s_instance;

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }
            s_instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Subscribe to NGO connection events
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
                NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;
            }
            else
            {
                StartCoroutine(WaitForNetworkManager());
            }
        }

        private IEnumerator WaitForNetworkManager()
        {
            while (NetworkManager.Singleton == null)
            {
                yield return new WaitForSeconds(1f);
            }
            
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
            NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
                NetworkManager.Singleton.OnTransportFailure -= OnTransportFailure;
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

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.LogWarning("[ConnectionManager] Network disconnected after resume. Reloading scene...");
                RestartGame();
            }
            else
            {
                Debug.Log("[ConnectionManager] Network still connected after resume.");
            }
        }

        private void OnClientDisconnect(ulong clientId)
        {
            // Only react if we (the local client) got disconnected
            if (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId)
            {
                Debug.LogWarning($"[ConnectionManager] Disconnected from server. Reloading scene...");
                RestartGame();
            }
        }

        private void OnTransportFailure()
        {
            Debug.LogWarning("[ConnectionManager] Transport failure. Reloading scene...");
            RestartGame();
        }

        private void RestartGame()
        {
            // Shutdown network manager
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
                NetworkManager.Singleton.OnTransportFailure -= OnTransportFailure;
                NetworkManager.Singleton.Shutdown();
            }

            // Reload the current scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
