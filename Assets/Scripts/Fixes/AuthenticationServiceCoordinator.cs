// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System.Threading.Tasks;
using Meta.XR.Samples;

namespace MRMotifs.Fixes
{
    /// <summary>
    /// Ensures only one authentication service initializes and prevents duplicate signin attempts.
    /// This fixes the "Invalid state for this operation. The player is already signing in" error.
    /// Attach to a single GameObject in your scene.
    /// </summary>
    [MetaCodeSample("MRMotifs-Fixes")]
    public class AuthenticationServiceCoordinator : MonoBehaviour
    {
        [Header("Authentication Settings")]
        [Tooltip("Enable debug logging for authentication process")]
        public bool enableDebugLogging = true;
        
        [Tooltip("Maximum time to wait for authentication (seconds)")]
        public float authenticationTimeout = 30f;
        
        private static bool s_isInitializing = false;
        private static bool s_isInitialized = false;
        private static AuthenticationServiceCoordinator s_instance;
        
        /// <summary>
        /// Check if Unity Services and Authentication are ready.
        /// </summary>
        public static bool IsAuthenticationReady
        {
            get
            {
                return s_isInitialized && 
                       UnityServices.State == ServicesInitializationState.Initialized &&
                       AuthenticationService.Instance.IsSignedIn;
            }
        }
        
        private async void Awake()
        {
            // Ensure only one coordinator exists
            if (s_instance != null && s_instance != this)
            {
                if (enableDebugLogging)
                    Debug.Log($"[AuthCoordinator] Destroying duplicate coordinator on {gameObject.name}");
                Destroy(this);
                return;
            }
            
            s_instance = this;
            
            // Prevent multiple initialization attempts
            if (s_isInitializing || s_isInitialized)
            {
                if (enableDebugLogging)
                    Debug.Log($"[AuthCoordinator] Authentication already in progress or complete");
                return;
            }
            
            await InitializeAuthenticationSafe();
        }
        
        private async Task InitializeAuthenticationSafe()
        {
            s_isInitializing = true;
            
            try
            {
                if (enableDebugLogging)
                    Debug.Log("[AuthCoordinator] Starting Unity Services initialization");
                
                // Initialize Unity Services if not already done
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    await UnityServices.InitializeAsync();
                }
                
                if (enableDebugLogging)
                    Debug.Log($"[AuthCoordinator] Unity Services state: {UnityServices.State}");
                
                // Sign in anonymously if not already signed in
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    if (enableDebugLogging)
                        Debug.Log("[AuthCoordinator] Signing in anonymously");
                        
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
                
                s_isInitialized = true;
                
                if (enableDebugLogging)
                    Debug.Log($"[AuthCoordinator] Authentication complete. Player ID: {AuthenticationService.Instance.PlayerId}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AuthCoordinator] Authentication failed: {ex.Message}");
            }
            finally
            {
                s_isInitializing = false;
            }
        }
        
        private void OnDestroy()
        {
            if (s_instance == this)
                s_instance = null;
        }
        
        /// <summary>
        /// For debugging - shows current authentication status
        /// </summary>
        [ContextMenu("Show Authentication Status")]
        public void ShowAuthenticationStatus()
        {
            Debug.Log($"[AuthCoordinator] Status:\n" +
                     $"- Unity Services: {UnityServices.State}\n" +
                     $"- Is Signed In: {AuthenticationService.Instance.IsSignedIn}\n" +
                     $"- Player ID: {(AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : "Not signed in")}\n" +
                     $"- Is Initializing: {s_isInitializing}\n" +
                     $"- Is Initialized: {s_isInitialized}");
        }
    }
}