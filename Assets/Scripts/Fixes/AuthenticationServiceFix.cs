using UnityEngine;
using System.Collections;

namespace ArenaDrone.Fixes
{
    /// <summary>
    /// Prevents authentication service conflicts between multiple building blocks
    /// Fixes "Invalid state for this operation. The player is already signing in." errors
    /// </summary>
    [DefaultExecutionOrder(-2000)] // Execute before all other networking components
    public class AuthenticationServiceFix : MonoBehaviour
    {
        private static bool s_authenticationInitialized = false;
        private static bool s_isInitializing = false;

        private void Awake()
        {
            // Start authentication coordination
            StartCoroutine(CoordinateAuthentication());
        }

        private IEnumerator CoordinateAuthentication()
        {
            // Wait for Unity Services to be available
            yield return new WaitUntil(() => Unity.Services.Core.UnityServices.State == Unity.Services.Core.ServicesInitializationState.Initialized);

            // Prevent multiple authentication attempts
            if (s_isInitializing || s_authenticationInitialized) 
            {
                Debug.Log("[AuthenticationServiceFix] Authentication already initialized or in progress, skipping");
                yield break;
            }

            s_isInitializing = true;

            // Only initialize authentication if not already signed in
            if (!Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("[AuthenticationServiceFix] Starting authentication process");
                
                // Sign in anonymously
                bool authenticationSucceeded = false;
                yield return StartCoroutine(SignInAnonymouslyCoroutine((success) => 
                {
                    authenticationSucceeded = success;
                }));
                
                if (authenticationSucceeded)
                {
                    s_authenticationInitialized = true;
                    Debug.Log("[AuthenticationServiceFix] Authentication completed successfully");
                }
                else
                {
                    Debug.LogError("[AuthenticationServiceFix] Authentication failed");
                }
            }
            else
            {
                Debug.Log("[AuthenticationServiceFix] User already signed in");
                s_authenticationInitialized = true;
            }

            s_isInitializing = false;
        }

        private IEnumerator SignInAnonymouslyCoroutine(System.Action<bool> onComplete)
        {
            bool signInComplete = false;
            bool signInSucceeded = false;
            string errorMessage = null;

            Unity.Services.Authentication.AuthenticationService.Instance.SignInAnonymouslyAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    errorMessage = task.Exception?.GetBaseException().Message;
                    signInSucceeded = false;
                }
                else
                {
                    signInSucceeded = true;
                }
                signInComplete = true;
            });

            // Wait for sign in to complete
            yield return new WaitUntil(() => signInComplete);

            if (!string.IsNullOrEmpty(errorMessage))
            {
                Debug.LogError($"[AuthenticationServiceFix] Sign in error: {errorMessage}");
            }

            onComplete?.Invoke(signInSucceeded);
        }

        /// <summary>
        /// Public method for other components to check if authentication is ready
        /// </summary>
        public static bool IsAuthenticationReady()
        {
            return s_authenticationInitialized && Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn;
        }

        /// <summary>
        /// Wait for authentication to be ready
        /// </summary>
        public static IEnumerator WaitForAuthentication()
        {
            yield return new WaitUntil(() => IsAuthenticationReady());
        }
    }
}