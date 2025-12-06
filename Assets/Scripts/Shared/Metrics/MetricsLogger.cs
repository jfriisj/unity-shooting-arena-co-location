// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.Samples;

namespace MRMotifs.SharedActivities.Metrics
{
    /// <summary>
    /// Collects and logs performance and collaboration metrics for research data collection.
    /// Saves data to CSV files on the device for later extraction.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class MetricsLogger : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Logging Settings")]
        [Tooltip("Interval in seconds between metric samples.")]
        [SerializeField] private float m_logInterval = 1.0f;
        
        [Tooltip("Enable/disable metrics logging.")]
        [SerializeField] private bool m_enableLogging = true;
        
        [Tooltip("Session identifier. Auto-generated from timestamp if empty.")]
        [SerializeField] private string m_sessionId = "";
        
        [Tooltip("Show debug UI overlay with live metrics.")]
        [SerializeField] private bool m_showDebugUI = true;
        
        [Header("Performance Thresholds")]
        [Tooltip("Target frame rate (FPS).")]
        [SerializeField] private float m_targetFps = 90f;
        
        [Tooltip("Maximum acceptable network latency in ms.")]
        [SerializeField] private float m_maxLatencyMs = 75f;
        
        [Tooltip("Maximum acceptable calibration error in mm.")]
        [SerializeField] private float m_maxCalibrationErrorMm = 10f;
        
        [Header("References")]
        [Tooltip("Reference to CalibrationAccuracyTracker for alignment data.")]
        [SerializeField] private CalibrationAccuracyTracker m_calibrationTracker;
        
        #endregion
        
        #region Private Fields
        
        private string m_headsetId;
        private string m_metricsFilePath;
        private string m_metadataFilePath;
        private StringBuilder m_csvBuffer;
        private List<MetricEntry> m_pendingMetrics;
        
        private float m_lastLogTime;
        private float m_sessionStartTime;
        private int m_totalMetricsLogged;
        private bool m_isInitialized;
        
        // FPS tracking with averaging
        private float m_fpsAccumulator;
        private int m_fpsFrameCount;
        private float m_lastFpsUpdateTime;
        private float m_currentFps;
        private const float FPS_UPDATE_INTERVAL = 0.5f;
        
        // Device metrics
        private float m_batteryTemperature = 25f;
        
        // Network state
        private NetworkRunner m_networkRunner;
        private int m_participantCount;
        private string m_sceneState = "Offline";
        
        // Auto-save settings
        private const float AUTO_SAVE_INTERVAL = 60f;
        private float m_lastAutoSaveTime;
        
        #endregion
        
        #region Data Structures
        
        [Serializable]
        private struct MetricEntry
        {
            public string sessionId;
            public string headsetId;
            public int participantCount;
            public float timestampSec;
            public float frameRateFps;
            public float networkLatencyMs;
            public float calibrationErrorMm;
            public float batteryTempC;
            public float batteryLevel;
            public string sceneState;
            
            public string ToCsvLine()
            {
                return $"{sessionId},{headsetId},{participantCount},{timestampSec:F2}," +
                       $"{frameRateFps:F1},{networkLatencyMs:F1},{calibrationErrorMm:F2}," +
                       $"{batteryTempC:F1},{batteryLevel:F0},{sceneState}";
            }
        }
        
        [Serializable]
        private class SessionMetadata
        {
            public string sessionId;
            public string headsetId;
            public string deviceName;
            public string deviceModel;
            public string startTime;
            public string endTime;
            public float durationMinutes;
            public int totalMetrics;
            public string unityVersion;
            public string appVersion;
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Generate unique headset ID (hash of device identifier)
            m_headsetId = GenerateHeadsetId();
            
            // Generate session ID if not provided
            if (string.IsNullOrEmpty(m_sessionId))
            {
                m_sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            }
            
            m_csvBuffer = new StringBuilder();
            m_pendingMetrics = new List<MetricEntry>();
        }
        
        private void Start()
        {
            if (!m_enableLogging)
            {
                Debug.Log("[MetricsLogger] Logging is disabled.");
                return;
            }
            
            InitializeLogging();
            
            // Find calibration tracker if not assigned
            if (m_calibrationTracker == null)
                m_calibrationTracker = FindAnyObjectByType<CalibrationAccuracyTracker>();
        }
        
        private void Update()
        {
            if (!m_enableLogging || !m_isInitialized) return;
            
            UpdateFps();
            UpdateNetworkState();
            UpdateBatteryTemperature();
            
            // Log metrics at interval
            if (Time.time - m_lastLogTime >= m_logInterval)
            {
                LogMetrics();
                m_lastLogTime = Time.time;
            }
            
            // Auto-save periodically
            if (Time.time - m_lastAutoSaveTime >= AUTO_SAVE_INTERVAL)
            {
                SavePendingMetrics();
                m_lastAutoSaveTime = Time.time;
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && m_isInitialized)
            {
                // Save when app is paused
                SavePendingMetrics();
                SaveMetadata();
            }
        }
        
        private void OnApplicationQuit()
        {
            if (m_isInitialized)
            {
                SavePendingMetrics();
                SaveMetadata();
                Debug.Log($"[MetricsLogger] Session ended. Total metrics logged: {m_totalMetricsLogged}");
            }
        }
        
        private void OnGUI()
        {
            if (!m_showDebugUI || !m_isInitialized) return;
            DrawDebugUI();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeLogging()
        {
            // Create metrics directory
            string metricsDir = Path.Combine(Application.persistentDataPath, "metrics");
            if (!Directory.Exists(metricsDir))
            {
                Directory.CreateDirectory(metricsDir);
            }
            
            // Create file paths
            string fileName = $"session_{m_sessionId}_{m_headsetId}";
            m_metricsFilePath = Path.Combine(metricsDir, $"{fileName}.csv");
            m_metadataFilePath = Path.Combine(metricsDir, $"{fileName}_metadata.json");
            
            // Write CSV header - essential metrics only per research requirements
            string header = "session_id,headset_id,participant_count,timestamp_sec," +
                           "frame_rate_fps,network_latency_ms,calibration_error_mm," +
                           "battery_temp_c,battery_level,scene_state";
            
            File.WriteAllText(m_metricsFilePath, header + "\n");
            
            m_sessionStartTime = Time.time;
            m_lastLogTime = Time.time;
            m_lastAutoSaveTime = Time.time;
            m_isInitialized = true;
            
            Debug.Log($"[MetricsLogger] Session: {m_sessionId}");
            Debug.Log($"[MetricsLogger] Headset ID: {m_headsetId}");
            Debug.Log($"[MetricsLogger] Metrics file: {m_metricsFilePath}");
        }
        
        private string GenerateHeadsetId()
        {
            // Create a short unique ID from device identifier
            string deviceId = SystemInfo.deviceUniqueIdentifier;
            int hash = deviceId.GetHashCode();
            return $"H_{Mathf.Abs(hash) % 10000:D4}";
        }
        
        #endregion
        
        #region Metrics Collection
        
        private void LogMetrics()
        {
            float timestamp = Time.time - m_sessionStartTime;
            
            var entry = new MetricEntry
            {
                sessionId = m_sessionId,
                headsetId = m_headsetId,
                participantCount = m_participantCount,
                timestampSec = timestamp,
                frameRateFps = m_currentFps,
                networkLatencyMs = GetNetworkLatency(),
                calibrationErrorMm = GetCalibrationError(),
                batteryTempC = m_batteryTemperature,
                batteryLevel = GetBatteryLevel(),
                sceneState = m_sceneState
            };
            
            m_pendingMetrics.Add(entry);
            m_totalMetricsLogged++;
        }
        
        private void UpdateFps()
        {
            m_fpsAccumulator += 1f / Time.unscaledDeltaTime;
            m_fpsFrameCount++;
            
            if (Time.time - m_lastFpsUpdateTime >= FPS_UPDATE_INTERVAL)
            {
                m_currentFps = m_fpsAccumulator / m_fpsFrameCount;
                m_fpsAccumulator = 0f;
                m_fpsFrameCount = 0;
                m_lastFpsUpdateTime = Time.time;
            }
        }
        
        private void UpdateNetworkState()
        {
            // Use NetworkRunner.Instances for more reliable runner detection
            // This handles cases where runner is in DontDestroyOnLoad or dynamically instantiated
            if (m_networkRunner == null || !m_networkRunner.IsRunning)
            {
                // Try to find a running NetworkRunner from Fusion's internal list
                foreach (var runner in NetworkRunner.Instances)
                {
                    if (runner != null && runner.IsRunning)
                    {
                        m_networkRunner = runner;
                        Debug.Log($"[MetricsLogger] Found running NetworkRunner via Instances: State={runner.State}, Mode={runner.GameMode}");
                        break;
                    }
                }
                
                // Fallback to FindAnyObjectByType if Instances is empty
                if (m_networkRunner == null)
                {
                    m_networkRunner = FindAnyObjectByType<NetworkRunner>();
                    if (m_networkRunner != null)
                    {
                        Debug.Log($"[MetricsLogger] Found NetworkRunner via FindAnyObjectByType: IsRunning={m_networkRunner.IsRunning}, State={m_networkRunner.State}");
                    }
                }
            }
            
            if (m_networkRunner != null && m_networkRunner.IsRunning)
            {
                m_participantCount = m_networkRunner.SessionInfo?.PlayerCount ?? 0;
                
                if (m_networkRunner.IsServer || m_networkRunner.IsSharedModeMasterClient)
                    m_sceneState = "Host";
                else
                    m_sceneState = "Client";
            }
            else
            {
                m_participantCount = 0;
                m_sceneState = "NotConnected"; // Fusion session not yet established
            }
        }
        
        private void UpdateBatteryTemperature()
        {
            // Try to get actual battery temperature via Android API
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var intent = currentActivity.Call<AndroidJavaObject>("registerReceiver", null, 
                    new AndroidJavaObject("android.content.IntentFilter", "android.intent.action.BATTERY_CHANGED")))
                {
                    if (intent != null)
                    {
                        int temp = intent.Call<int>("getIntExtra", "temperature", 0);
                        m_batteryTemperature = temp / 10f; // Android reports in tenths of degrees
                    }
                }
            }
            catch (System.Exception)
            {
                // Fallback to estimation if Android API fails
                EstimateBatteryTemperature();
            }
#else
            EstimateBatteryTemperature();
#endif
        }
        
        private void EstimateBatteryTemperature()
        {
            // Estimate temperature based on thermal throttling behavior
            float throttleMultiplier = Time.timeScale < 1f ? 0.1f : 0f;
            float fpsDropFactor = m_currentFps < m_targetFps * 0.9f ? 0.05f : 0f;
            
            // Slowly increase temperature during use
            float targetTemp = 30f + (throttleMultiplier + fpsDropFactor) * 15f;
            m_batteryTemperature = Mathf.Lerp(m_batteryTemperature, targetTemp, Time.deltaTime * 0.1f);
            m_batteryTemperature = Mathf.Clamp(m_batteryTemperature, 25f, 45f);
        }
        
        private float GetNetworkLatency()
        {
            // Use Fusion's built-in RTT measurement
            // GetPlayerRtt returns RTT in seconds, convert to milliseconds
            if (m_networkRunner != null && m_networkRunner.IsRunning)
            {
                // Get RTT to the SharedModeMasterClient (host)
                // In Shared Mode, all clients measure RTT to the cloud relay
                double rttSeconds = m_networkRunner.GetPlayerRtt(m_networkRunner.LocalPlayer);
                return (float)(rttSeconds * 1000.0); // Convert to ms
            }
            return 0f;
        }
        
        private float GetCalibrationError()
        {
            if (m_calibrationTracker != null)
            {
                return m_calibrationTracker.GetCurrentError();
            }
            
            // Fallback: try to find ColocationManager
            var colocationManager = FindAnyObjectByType<MRMotifs.ColocatedExperiences.Colocation.ColocationManager>();
            if (colocationManager != null)
            {
                return colocationManager.GetCurrentCalibrationError();
            }
            
            return 0f;
        }
        
        private float GetBatteryLevel()
        {
            float level = SystemInfo.batteryLevel;
            return level >= 0 ? level * 100f : 100f;
        }
        
        #endregion
        
        #region Data Persistence
        
        private void SavePendingMetrics()
        {
            if (m_pendingMetrics.Count == 0) return;
            
            m_csvBuffer.Clear();
            foreach (var entry in m_pendingMetrics)
            {
                m_csvBuffer.AppendLine(entry.ToCsvLine());
            }
            
            try
            {
                File.AppendAllText(m_metricsFilePath, m_csvBuffer.ToString());
                Debug.Log($"[MetricsLogger] Saved {m_pendingMetrics.Count} metrics to file.");
                m_pendingMetrics.Clear();
            }
            catch (Exception e)
            {
                Debug.LogError($"[MetricsLogger] Failed to save metrics: {e.Message}");
            }
        }
        
        private void SaveMetadata()
        {
            var metadata = new SessionMetadata
            {
                sessionId = m_sessionId,
                headsetId = m_headsetId,
                deviceName = SystemInfo.deviceName,
                deviceModel = SystemInfo.deviceModel,
                startTime = DateTime.Now.AddSeconds(-(Time.time - m_sessionStartTime)).ToString("o"),
                endTime = DateTime.Now.ToString("o"),
                durationMinutes = (Time.time - m_sessionStartTime) / 60f,
                totalMetrics = m_totalMetricsLogged,
                unityVersion = Application.unityVersion,
                appVersion = Application.version
            };
            
            try
            {
                string json = JsonUtility.ToJson(metadata, true);
                File.WriteAllText(m_metadataFilePath, json);
                Debug.Log($"[MetricsLogger] Saved metadata to: {m_metadataFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MetricsLogger] Failed to save metadata: {e.Message}");
            }
        }
        
        #endregion
        
        #region Debug UI
        
        private void DrawDebugUI()
        {
            float padding = 10f;
            float width = 420f;
            float height = 200f;
            
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 18,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(10, 10, 10, 10)
            };
            
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                richText = true
            };
            
            Rect rect = new Rect(padding, padding, width, height);
            GUI.Box(rect, "", boxStyle);
            
            GUILayout.BeginArea(new Rect(padding + 10, padding + 5, width - 20, height - 10));
            
            GUILayout.Label($"<b>[MetricsLogger]</b> Session: {m_sessionId}", labelStyle);
            
            string fpsColor = m_currentFps >= m_targetFps ? "green" : "yellow";
            string latencyColor = GetNetworkLatency() <= m_maxLatencyMs ? "green" : "yellow";
            
            GUILayout.Label($"<color={fpsColor}>FPS: {m_currentFps:F1}</color> | " +
                           $"<color={latencyColor}>Latency: {GetNetworkLatency():F1}ms</color> | " +
                           $"Calibration: {GetCalibrationError():F2}mm", labelStyle);
            
            GUILayout.Label($"Participants: {m_participantCount} | " +
                           $"State: {m_sceneState} | " +
                           $"Battery: {GetBatteryLevel():F0}%", labelStyle);
            
            GUILayout.Label($"Temp: {m_batteryTemperature:F1}°C | " +
                           $"Metrics: {m_totalMetricsLogged}", labelStyle);
            
            GUILayout.EndArea();
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Manually trigger a metrics save.
        /// </summary>
        public void ForceSave()
        {
            SavePendingMetrics();
            SaveMetadata();
        }
        
        /// <summary>
        /// Get the current session ID.
        /// </summary>
        public string GetSessionId() => m_sessionId;
        
        /// <summary>
        /// Get total metrics logged this session.
        /// </summary>
        public int GetTotalMetricsLogged() => m_totalMetricsLogged;
        
        /// <summary>
        /// Get the path to the metrics CSV file.
        /// </summary>
        public string GetMetricsFilePath() => m_metricsFilePath;
        
        /// <summary>
        /// Set a custom session ID. Must be called before Start().
        /// </summary>
        public void SetSessionId(string sessionId)
        {
            if (!m_isInitialized)
            {
                m_sessionId = sessionId;
            }
            else
            {
                Debug.LogWarning("[MetricsLogger] Cannot change session ID after initialization.");
            }
        }
        
        /// <summary>
        /// Enable or disable debug UI.
        /// </summary>
        public void SetDebugUIEnabled(bool enabled)
        {
            m_showDebugUI = enabled;
        }
        
        #endregion
    }
}
#endif
