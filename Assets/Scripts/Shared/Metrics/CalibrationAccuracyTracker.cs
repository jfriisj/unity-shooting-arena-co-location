// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using UnityEngine;
using System;
using Meta.XR.Samples;

namespace MRMotifs.SharedActivities.Metrics
{
    /// <summary>
    /// Tracks spatial calibration accuracy for colocation.
    /// Monitors drift and alignment errors over time.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class CalibrationAccuracyTracker : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Tracking Settings")]
        [Tooltip("Interval in seconds between calibration checks.")]
        [SerializeField] private float m_checkInterval = 1.0f;
        
        [Tooltip("Error threshold in mm that triggers a warning.")]
        [SerializeField] private float m_warningThresholdMm = 10f;
        
        [Tooltip("Error threshold in mm that triggers an error/recalibration suggestion.")]
        [SerializeField] private float m_errorThresholdMm = 25f;
        
        [Header("Debug")]
        [Tooltip("Log calibration status to console.")]
        [SerializeField] private bool m_debugLogging = false;
        
        #endregion
        
        #region Private Fields
        
        private MRMotifs.ColocatedExperiences.Colocation.ColocationManager m_colocationManager;
        private OVRCameraRig m_cameraRig;
        
        private bool m_hasCalibrated;
        private Vector3 m_initialAnchorPosition;
        private Quaternion m_initialAnchorRotation;
        private float m_currentErrorMm;
        private float m_maxErrorMm;
        private float m_averageErrorMm;
        private int m_errorSampleCount;
        private float m_errorSum;
        
        private float m_lastCheckTime;
        private int m_recalibrationCount;
        private DateTime m_lastCalibrationTime;
        
        // Events for external listeners
        public event Action<float> OnCalibrationErrorUpdated;
        public event Action<float> OnWarningThresholdExceeded;
        public event Action<float> OnErrorThresholdExceeded;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            m_colocationManager = FindAnyObjectByType<MRMotifs.ColocatedExperiences.Colocation.ColocationManager>();
            m_cameraRig = FindAnyObjectByType<OVRCameraRig>();
            
            if (m_colocationManager == null)
            {
                Debug.LogWarning("[CalibrationAccuracyTracker] ColocationManager not found. Some features will be limited.");
            }
        }
        
        private void Update()
        {
            if (Time.time - m_lastCheckTime >= m_checkInterval)
            {
                UpdateCalibrationError();
                m_lastCheckTime = Time.time;
            }
        }
        
        #endregion
        
        #region Calibration Tracking
        
        private void UpdateCalibrationError()
        {
            if (!m_hasCalibrated) return;
            
            // Get current error from ColocationManager if available
            if (m_colocationManager != null)
            {
                m_currentErrorMm = m_colocationManager.ValidateCalibration();
            }
            else
            {
                // Fallback: estimate drift from camera rig movement
                m_currentErrorMm = EstimateDriftError();
            }
            
            // Update statistics
            m_maxErrorMm = Mathf.Max(m_maxErrorMm, m_currentErrorMm);
            m_errorSum += m_currentErrorMm;
            m_errorSampleCount++;
            m_averageErrorMm = m_errorSum / m_errorSampleCount;
            
            // Fire events
            OnCalibrationErrorUpdated?.Invoke(m_currentErrorMm);
            
            if (m_currentErrorMm >= m_errorThresholdMm)
            {
                OnErrorThresholdExceeded?.Invoke(m_currentErrorMm);
                
                if (m_debugLogging)
                {
                    Debug.LogError($"[CalibrationAccuracyTracker] ERROR: Calibration error {m_currentErrorMm:F2}mm exceeds threshold {m_errorThresholdMm}mm");
                }
            }
            else if (m_currentErrorMm >= m_warningThresholdMm)
            {
                OnWarningThresholdExceeded?.Invoke(m_currentErrorMm);
                
                if (m_debugLogging)
                {
                    Debug.LogWarning($"[CalibrationAccuracyTracker] WARNING: Calibration error {m_currentErrorMm:F2}mm exceeds threshold {m_warningThresholdMm}mm");
                }
            }
        }
        
        private float EstimateDriftError()
        {
            if (m_cameraRig == null) return 0f;
            
            // This is a simplified drift estimation
            // In a real colocation scenario, we would compare against the shared anchor
            Vector3 currentPosition = m_cameraRig.trackingSpace.position;
            Vector3 delta = currentPosition - m_initialAnchorPosition;
            
            // Only consider horizontal drift (ignore vertical as user may be sitting/standing)
            delta.y = 0;
            
            return delta.magnitude * 1000f; // Convert to mm
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Called when colocation calibration is complete.
        /// Records the initial anchor position for drift tracking.
        /// </summary>
        /// <param name="anchorPosition">World position of the calibration anchor.</param>
        /// <param name="anchorRotation">World rotation of the calibration anchor.</param>
        public void OnCalibrationComplete(Vector3 anchorPosition, Quaternion anchorRotation)
        {
            m_initialAnchorPosition = anchorPosition;
            m_initialAnchorRotation = anchorRotation;
            m_hasCalibrated = true;
            m_lastCalibrationTime = DateTime.Now;
            
            // Reset error tracking
            m_currentErrorMm = 0f;
            m_maxErrorMm = 0f;
            m_averageErrorMm = 0f;
            m_errorSampleCount = 0;
            m_errorSum = 0f;
            
            if (m_debugLogging)
            {
                Debug.Log($"[CalibrationAccuracyTracker] Calibration complete. Anchor at {anchorPosition}");
            }
        }
        
        /// <summary>
        /// Called when colocation calibration is complete (simplified version).
        /// </summary>
        public void OnCalibrationComplete()
        {
            if (m_cameraRig != null)
            {
                OnCalibrationComplete(m_cameraRig.trackingSpace.position, m_cameraRig.trackingSpace.rotation);
            }
            else
            {
                OnCalibrationComplete(Vector3.zero, Quaternion.identity);
            }
        }
        
        /// <summary>
        /// Called when recalibration is triggered.
        /// </summary>
        public void OnRecalibration()
        {
            m_recalibrationCount++;
            
            if (m_debugLogging)
            {
                Debug.Log($"[CalibrationAccuracyTracker] Recalibration #{m_recalibrationCount} triggered at error {m_currentErrorMm:F2}mm");
            }
            
            // Don't reset calibration here - OnCalibrationComplete will be called with new anchor
        }
        
        /// <summary>
        /// Get the current calibration error in millimeters.
        /// </summary>
        public float GetCurrentError()
        {
            return m_currentErrorMm;
        }
        
        /// <summary>
        /// Get the maximum observed calibration error in millimeters.
        /// </summary>
        public float GetMaxError()
        {
            return m_maxErrorMm;
        }
        
        /// <summary>
        /// Get the average calibration error in millimeters.
        /// </summary>
        public float GetAverageError()
        {
            return m_averageErrorMm;
        }
        
        /// <summary>
        /// Get the number of recalibrations performed this session.
        /// </summary>
        public int GetRecalibrationCount()
        {
            return m_recalibrationCount;
        }
        
        /// <summary>
        /// Check if calibration has been performed.
        /// </summary>
        public bool IsCalibrated()
        {
            return m_hasCalibrated;
        }
        
        /// <summary>
        /// Get time since last calibration.
        /// </summary>
        public TimeSpan GetTimeSinceCalibration()
        {
            if (!m_hasCalibrated) return TimeSpan.Zero;
            return DateTime.Now - m_lastCalibrationTime;
        }
        
        /// <summary>
        /// Get detailed calibration statistics as a formatted string.
        /// </summary>
        public string GetStatisticsString()
        {
            if (!m_hasCalibrated)
            {
                return "Not calibrated";
            }
            
            return $"Current: {m_currentErrorMm:F2}mm | " +
                   $"Avg: {m_averageErrorMm:F2}mm | " +
                   $"Max: {m_maxErrorMm:F2}mm | " +
                   $"Recalibrations: {m_recalibrationCount}";
        }
        
        /// <summary>
        /// Check if current error is within acceptable limits.
        /// </summary>
        public bool IsWithinLimits()
        {
            return m_currentErrorMm < m_warningThresholdMm;
        }
        
        /// <summary>
        /// Get calibration quality as a percentage (100% = perfect, 0% = at error threshold).
        /// </summary>
        public float GetCalibrationQuality()
        {
            if (!m_hasCalibrated) return 0f;
            
            float quality = 1f - (m_currentErrorMm / m_errorThresholdMm);
            return Mathf.Clamp01(quality) * 100f;
        }
        
        /// <summary>
        /// Reset all tracking statistics.
        /// </summary>
        public void ResetStatistics()
        {
            m_maxErrorMm = m_currentErrorMm;
            m_averageErrorMm = m_currentErrorMm;
            m_errorSampleCount = 1;
            m_errorSum = m_currentErrorMm;
            m_recalibrationCount = 0;
            
            Debug.Log("[CalibrationAccuracyTracker] Statistics reset.");
        }
        
        #endregion
    }
}
#endif
