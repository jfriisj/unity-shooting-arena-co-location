// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using Meta.XR.Samples;

namespace MRMotifs.SharedActivities.ShootingSample
{
    /// <summary>
    /// Disables the Quest Guardian boundary system to allow free movement in passthrough mode.
    /// This is useful for co-located MR experiences where physical space awareness comes from
    /// the real environment visible through passthrough, not from the virtual boundary.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class BoundaryDisablerMotif : MonoBehaviour
    {
        [Tooltip("If true, the boundary will be suppressed when passthrough is active.")]
        [SerializeField] private bool m_suppressBoundary = true;

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

        /// <summary>
        /// Suppresses the Guardian boundary system, allowing free movement.
        /// </summary>
        public void SuppressBoundary()
        {
            if (OVRManager.boundary != null)
            {
                // Suppress the boundary visibility
                OVRManager.boundary.SetVisible(false);
                Debug.Log("[BoundaryDisablerMotif] Guardian boundary suppressed for free movement");
            }

            // Request boundary-less mode via OVRManager
            // This tells the system we want unbounded movement (like in a large play area)
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                // Use OVRPlugin to request unbounded tracking mode
                OVRPlugin.SetBoundaryVisible(false);
                Debug.Log("[BoundaryDisablerMotif] OVRPlugin boundary visibility set to false");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[BoundaryDisablerMotif] Could not set boundary visibility: {e.Message}");
            }
#endif
        }

        /// <summary>
        /// Restores the Guardian boundary system.
        /// </summary>
        public void RestoreBoundary()
        {
            if (OVRManager.boundary != null)
            {
                OVRManager.boundary.SetVisible(true);
                Debug.Log("[BoundaryDisablerMotif] Guardian boundary restored");
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                OVRPlugin.SetBoundaryVisible(true);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[BoundaryDisablerMotif] Could not restore boundary visibility: {e.Message}");
            }
#endif
        }

        private void OnDisable()
        {
            // Optionally restore boundary when disabled
            // RestoreBoundary();
        }
    }
}
