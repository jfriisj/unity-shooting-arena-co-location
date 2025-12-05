// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using Meta.XR.Samples;

namespace MRMotifs.SharedActivities.ShootingSample
{
    /// <summary>
    /// Disables the Quest Guardian boundary system to allow free movement in passthrough mode.
    /// Uses the Contextual Boundaryless API (OVRManager.shouldBoundaryVisibilityBeSuppressed) which
    /// properly suppresses the boundary when passthrough is active.
    /// 
    /// Requirements:
    /// - OVRManager Quest Features > Boundary Visibility Support must be set to "Supported" or "Required"
    /// - Passthrough Support must not be "None"
    /// - An OVRPassthroughLayer must be active for boundary suppression to work
    /// 
    /// For fully boundaryless apps (no VR segments at all), add to AndroidManifest.xml instead:
    /// <uses-feature android:name="com.oculus.feature.BOUNDARYLESS_APP" android:required="true"/>
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class BoundaryDisablerMotif : MonoBehaviour
    {
        [Tooltip("If true, the boundary will be suppressed when passthrough is active.")]
        [SerializeField] private bool m_suppressBoundary = true;

        [Tooltip("Reference to the OVRManager (will auto-find if not set).")]
        [SerializeField] private OVRManager m_ovrManager;

        [Tooltip("Reference to the OVRPassthroughLayer (required for boundary suppression).")]
        [SerializeField] private OVRPassthroughLayer m_passthroughLayer;

        private void Awake()
        {
            // Find OVRManager if not assigned
            if (m_ovrManager == null)
            {
                m_ovrManager = FindAnyObjectByType<OVRManager>();
            }

            // Find OVRPassthroughLayer if not assigned
            if (m_passthroughLayer == null)
            {
                m_passthroughLayer = FindAnyObjectByType<OVRPassthroughLayer>();
            }

            // Subscribe to boundary visibility changes
            OVRManager.BoundaryVisibilityChanged += OnBoundaryVisibilityChanged;
        }

        private void Start()
        {
            if (m_suppressBoundary)
            {
                SuppressBoundary();
            }
        }

        private void OnEnable()
        {
            if (m_suppressBoundary)
            {
                SuppressBoundary();
            }
        }

        private void OnDestroy()
        {
            OVRManager.BoundaryVisibilityChanged -= OnBoundaryVisibilityChanged;
        }

        private void OnBoundaryVisibilityChanged(OVRPlugin.BoundaryVisibility boundaryVisibility)
        {
            bool isSuppressed = boundaryVisibility == OVRPlugin.BoundaryVisibility.Suppressed;
            Debug.Log($"[BoundaryDisablerMotif] Boundary visibility changed: {boundaryVisibility} (suppressed: {isSuppressed})");
        }

        /// <summary>
        /// Suppresses the Guardian boundary system using the Contextual Boundaryless API.
        /// This allows players to walk everywhere without boundary interruptions.
        /// </summary>
        public void SuppressBoundary()
        {
            if (m_ovrManager == null)
            {
                Debug.LogError("[BoundaryDisablerMotif] OVRManager not found! Cannot suppress boundary.");
                return;
            }

            // Ensure passthrough is enabled (required for boundary suppression)
            if (m_passthroughLayer != null && !m_passthroughLayer.enabled)
            {
                m_passthroughLayer.enabled = true;
                Debug.Log("[BoundaryDisablerMotif] Enabled OVRPassthroughLayer for boundary suppression");
            }

            // Use the proper Contextual Boundaryless API
            // This requests the system to suppress the boundary while passthrough is active
            m_ovrManager.shouldBoundaryVisibilityBeSuppressed = true;
            Debug.Log("[BoundaryDisablerMotif] Requested boundary suppression via Contextual Boundaryless API");

            // Check if suppression is actually active
            if (m_ovrManager.isBoundaryVisibilitySuppressed)
            {
                Debug.Log("[BoundaryDisablerMotif] Boundary is now suppressed - players can walk everywhere!");
            }
            else
            {
                Debug.LogWarning("[BoundaryDisablerMotif] Boundary suppression requested but not yet active. " +
                    "Ensure Passthrough Support is enabled in OVRManager Quest Features and " +
                    "Boundary Visibility Support is set to 'Supported' or 'Required'.");
            }
        }

        /// <summary>
        /// Restores the Guardian boundary system.
        /// </summary>
        public void RestoreBoundary()
        {
            if (m_ovrManager != null)
            {
                m_ovrManager.shouldBoundaryVisibilityBeSuppressed = false;
                Debug.Log("[BoundaryDisablerMotif] Boundary suppression disabled - Guardian restored");
            }
        }

        private void OnDisable()
        {
            // Optionally restore boundary when disabled
            // RestoreBoundary();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_suppressBoundary)
            {
                Debug.Log("[BoundaryDisablerMotif] Note: For boundary suppression to work, ensure:\n" +
                    "1. OVRManager > Quest Features > Boundary Visibility Support = 'Supported' or 'Required'\n" +
                    "2. OVRManager > Quest Features > Passthrough Support != 'None'\n" +
                    "3. An active OVRPassthroughLayer component exists in the scene");
            }
        }
#endif
    }
}
