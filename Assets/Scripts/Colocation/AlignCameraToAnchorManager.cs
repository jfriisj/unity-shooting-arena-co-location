// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using UnityEngine;
using System.Threading.Tasks;
using Meta.XR.Samples;
using MRMotifs.Shared;

namespace MRMotifs.ColocatedExperiences.Colocation
{
    /// <summary>
    /// Manages camera alignment to spatial anchor when user recenters HMD.
    /// Based on Discover sample pattern for handling recenter events in co-located experiences.
    /// Critical for maintaining alignment accuracy in multiplayer scenarios.
    /// </summary>
    [MetaCodeSample("MRMotifs-ColocatedExperiences")]
    public class AlignCameraToAnchorManager : MonoBehaviour
    {
        /// <summary>
        /// Reference to the colocation manager that handles alignment.
        /// </summary>
        public ColocationManager ColocationManager { get; set; }

        /// <summary>
        /// Delay in milliseconds before realigning (allows tracking to stabilize).
        /// </summary>
        [SerializeField] private int m_realignDelay = 500;

        private void OnEnable()
        {
            // Subscribe to recenter events
            if (OVRManager.display != null)
            {
                OVRManager.display.RecenteredPose += OnRecenterPose;
            }
            OVRManager.HMDMounted += OnHMDMounted;
            
            DebugLogger.Colocation("AlignCameraToAnchorManager | Subscribed to recenter events", this);
        }

        private void OnDisable()
        {
            // Unsubscribe from recenter events
            if (OVRManager.display != null)
            {
                OVRManager.display.RecenteredPose -= OnRecenterPose;
            }
            OVRManager.HMDMounted -= OnHMDMounted;
            
            DebugLogger.Colocation("AlignCameraToAnchorManager | Unsubscribed from recenter events", this);
        }

        /// <summary>
        /// Called when user recenters the view (hold Oculus button).
        /// </summary>
        private void OnRecenterPose()
        {
            DebugLogger.Colocation("Recenter detected | Triggering realignment", this);
            RealignToAnchor();
        }

        /// <summary>
        /// Called when HMD is mounted (user puts on headset).
        /// </summary>
        private void OnHMDMounted()
        {
            DebugLogger.Colocation("HMD mounted | Triggering realignment", this);
            RealignToAnchor();
        }

        /// <summary>
        /// Realigns the camera rig to the anchor after a delay.
        /// Can be called manually via context menu for testing.
        /// </summary>
        [ContextMenu("Realign to Anchor")]
        public async void RealignToAnchor()
        {
            if (ColocationManager == null)
            {
                DebugLogger.Error("COLOCATION", "ColocationManager is null - cannot realign", this);
                return;
            }

            if (!ColocationManager.HasCalibrated)
            {
                DebugLogger.Colocation("Not yet calibrated | Skipping realignment", this);
                return;
            }

            // Wait for tracking to stabilize after recenter
            // This is especially important when using Link (editor testing)
            await Task.Delay(m_realignDelay);

            // Trigger realignment through ColocationManager
            if (ColocationManager != null && ColocationManager.HasCalibrated)
            {
                ColocationManager.RealignToLastAnchor();
                DebugLogger.Colocation("Realignment triggered successfully", this);
            }
        }
    }
}
#endif
