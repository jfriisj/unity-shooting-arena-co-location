// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using UnityEngine;
using Meta.XR.Samples;

namespace MRMotifs.ColocatedExperiences.Colocation
{
    [MetaCodeSample("MRMotifs-ColocatedExperiences")]
    public class ColocationManager : MonoBehaviour
    {
        private Transform m_cameraRigTransform;
        private Vector3 m_preAlignmentPosition;
        private Quaternion m_preAlignmentRotation;
        private float m_currentCalibrationError = 0f;
        private Vector3 m_anchorPosition;
        private bool m_hasCalibrated = false;
        
        // Reference position for drift tracking (camera rig position after calibration)
        private Vector3 m_referenceRigPosition;
        private bool m_isHost = false;

        private void Awake()
        {
            m_cameraRigTransform = FindAnyObjectByType<OVRCameraRig>().transform;
        }

        /// <summary>
        /// Called by host when they create an anchor at their position.
        /// Registers the host as calibrated for drift tracking purposes.
        /// </summary>
        /// <param name="anchorPosition">World position where the anchor was created.</param>
        public void RegisterHostCalibration(Vector3 anchorPosition)
        {
            m_anchorPosition = anchorPosition;
            m_referenceRigPosition = m_cameraRigTransform.position;
            m_isHost = true;
            m_hasCalibrated = true;
            m_currentCalibrationError = 0f; // Host has 0 initial error (they define the origin)
            
            Debug.Log($"Motif: Host calibration registered. Anchor at {anchorPosition}, Rig at {m_referenceRigPosition}");
            
            // Notify CalibrationAccuracyTracker if present
            var calibrationTracker = FindAnyObjectByType<MRMotifs.SharedActivities.Metrics.CalibrationAccuracyTracker>();
            if (calibrationTracker != null)
            {
                calibrationTracker.OnCalibrationComplete(m_anchorPosition, Quaternion.identity);
            }
        }

        /// <summary>
        /// Aligns the player's tracking space and camera rig to the specified anchor.
        /// Called by clients when they localize to the shared anchor.
        /// </summary>
        /// <param name="anchor">The spatial anchor to align to.</param>
        public void AlignUserToAnchor(OVRSpatialAnchor anchor)
        {
            if (!anchor || !anchor.Localized)
            {
                Debug.LogError("Motif: Invalid or un-localized anchor. Cannot align.");
                return;
            }

            var anchorTransform = anchor.transform;

            // Store pre-alignment state for error calculation
            m_preAlignmentPosition = m_cameraRigTransform.position;
            m_preAlignmentRotation = m_cameraRigTransform.rotation;
            m_anchorPosition = anchorTransform.position;

            // Perform alignment
            m_cameraRigTransform.position = anchorTransform.InverseTransformPoint(Vector3.zero);
            m_cameraRigTransform.eulerAngles = new Vector3(0, -anchorTransform.eulerAngles.y, 0);

            // Store reference position for drift tracking
            m_referenceRigPosition = m_cameraRigTransform.position;
            m_isHost = false;
            m_hasCalibrated = true;
            
            // Initial calibration error is 0 - we just aligned to the anchor
            // The "error" was the pre-alignment offset, but that's now corrected
            m_currentCalibrationError = 0f;

            Debug.Log($"Motif: Client alignment complete. Rig moved from {m_preAlignmentPosition} to {m_referenceRigPosition}");
            
            // Notify CalibrationAccuracyTracker if present
            var calibrationTracker = FindAnyObjectByType<MRMotifs.SharedActivities.Metrics.CalibrationAccuracyTracker>();
            if (calibrationTracker != null)
            {
                calibrationTracker.OnCalibrationComplete(m_anchorPosition, anchorTransform.rotation);
            }
        }

        /// <summary>
        /// Gets the current calibration error in millimeters.
        /// Returns 0 if no calibration has been performed yet.
        /// </summary>
        public float GetCurrentCalibrationError()
        {
            return m_currentCalibrationError;
        }

        /// <summary>
        /// Validates calibration by measuring drift from reference position.
        /// Tracks how much the camera rig has drifted since calibration.
        /// </summary>
        public float ValidateCalibration()
        {
            if (!m_hasCalibrated)
            {
                return 0f;
            }

            // Measure drift from reference position (horizontal only)
            Vector3 currentPosition = m_cameraRigTransform.position;
            Vector3 driftDelta = currentPosition - m_referenceRigPosition;
            driftDelta.y = 0; // Ignore vertical drift (sitting/standing)
            float driftError = driftDelta.magnitude * 1000f; // Convert to millimeters

            m_currentCalibrationError = driftError;
            return driftError;
        }

        /// <summary>
        /// Resets calibration tracking state.
        /// </summary>
        public void ResetCalibration()
        {
            m_currentCalibrationError = 0f;
            m_hasCalibrated = false;
            m_isHost = false;
            Debug.Log("Motif: Calibration reset.");
        }
        
        /// <summary>
        /// Returns true if this user has completed calibration.
        /// </summary>
        public bool IsCalibrated()
        {
            return m_hasCalibrated;
        }
        
        /// <summary>
        /// Returns true if this user is the host (anchor creator).
        /// </summary>
        public bool IsHost()
        {
            return m_isHost;
        }

        /// <summary>
        /// Gets calibration status information for debugging.
        /// </summary>
        public string GetCalibrationStatus()
        {
            if (!m_hasCalibrated)
                return "No calibration performed yet.";

            string role = m_isHost ? "Host" : "Client";
            return $"Role: {role}\n" +
                   $"Drift Error: {m_currentCalibrationError:F2}mm\n" +
                   $"Status: {(m_currentCalibrationError < 10f ? "PASS" : "WARNING")}\n" +
                   $"Reference Position: {m_referenceRigPosition}";
        }
    }
}
#endif
