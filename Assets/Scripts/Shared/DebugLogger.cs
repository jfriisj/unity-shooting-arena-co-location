// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using Debug = UnityEngine.Debug;

namespace MRMotifs.Shared
{
    /// <summary>
    /// Centralized debug logging with consistent formatting and categories.
    /// Use this for structured, easy-to-filter debug output.
    /// Based on Discover sample pattern.
    /// </summary>
    public static class DebugLogger
    {
        // Enable/disable categories for filtering
        public static bool EnableBullet = true;
        public static bool EnablePlayer = true;
        public static bool EnableColocation = true;
        public static bool EnableNetwork = true;
        public static bool EnableHealth = true;
        public static bool EnableGame = true;
        public static bool EnableShooting = true;

        // Runtime log collection
        public static bool CollectLogs = true;
        public static bool SaveToFile = true;
        private static readonly List<LogEntry> s_logEntries = new List<LogEntry>();
        
        [System.Serializable]
        public struct LogEntry
        {
            public string message;
            public string category;
            public string fullMessage;
            public LogType logType;
            public DateTime timestamp;
            
            public LogEntry(string msg, string cat, string full, LogType type)
            {
                message = msg;
                category = cat;
                fullMessage = full;
                logType = type;
                timestamp = DateTime.Now;
            }
        }
        
        public static IReadOnlyList<LogEntry> LogEntries => s_logEntries;
        public static event System.Action<LogEntry> OnLogAdded;

        private const string COLOR_BULLET = "#FF6B6B";      // Red
        private const string COLOR_PLAYER = "#4ECDC4";      // Teal
        private const string COLOR_COLOCATION = "#45B7D1";  // Blue
        private const string COLOR_NETWORK = "#96CEB4";     // Green
        private const string COLOR_HEALTH = "#FFEAA7";      // Yellow
        private const string COLOR_GAME = "#DDA0DD";        // Plum
        private const string COLOR_SHOOTING = "#FF69B4";    // Hot Pink
        private const string COLOR_WARNING = "#FFB347";     // Orange
        private const string COLOR_ERROR = "#FF4444";       // Bright Red

        /// <summary>
        /// Log bullet-related messages.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Bullet(string message, UnityEngine.Object context = null)
        {
            if (EnableBullet)
                Debug.Log(Format("BULLET", COLOR_BULLET, message), context);
        }

        /// <summary>
        /// Log player-related messages.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Player(string message, UnityEngine.Object context = null)
        {
            if (EnablePlayer)
                Debug.Log(Format("PLAYER", COLOR_PLAYER, message), context);
        }

        /// <summary>
        /// Log colocation-related messages.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Colocation(string message, UnityEngine.Object context = null)
        {
            if (EnableColocation)
                Debug.Log(Format("COLOCATION", COLOR_COLOCATION, message), context);
        }

        /// <summary>
        /// Log network-related messages.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Network(string message, UnityEngine.Object context = null)
        {
            if (EnableNetwork)
                Debug.Log(Format("NETWORK", COLOR_NETWORK, message), context);
        }

        /// <summary>
        /// Log health-related messages.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Health(string message, UnityEngine.Object context = null)
        {
            if (EnableHealth)
                Debug.Log(Format("HEALTH", COLOR_HEALTH, message), context);
        }

        /// <summary>
        /// Log game-related messages.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Game(string message, UnityEngine.Object context = null)
        {
            if (EnableGame)
                Debug.Log(Format("GAME", COLOR_GAME, message), context);
        }

        /// <summary>
        /// Log shooting-related messages (drones, bullets, combat).
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Shooting(string message, UnityEngine.Object context = null)
        {
            if (EnableShooting)
                Debug.Log(Format("SHOOTING", COLOR_SHOOTING, message), context);
        }

        /// <summary>
        /// Log warning with category.
        /// </summary>
        public static void Warning(string category, string message, UnityEngine.Object context = null)
        {
            Debug.LogWarning(Format(category, COLOR_WARNING, message), context);
        }

        /// <summary>
        /// Log error with category.
        /// </summary>
        public static void Error(string category, string message, UnityEngine.Object context = null)
        {
            Debug.LogError(Format(category, COLOR_ERROR, message), context);
        }

        private static string Format(string category, string color, string message)
        {
            return $"<color={color}>[{category}]</color> {message}";
        }

        /// <summary>
        /// Log a state transition (useful for debugging flow).
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void StateChange(string category, string from, string to, UnityEngine.Object context = null)
        {
            Debug.Log($"<color=#9B59B6>[{category}]</color> State: {from} → {to}", context);
        }

        /// <summary>
        /// Log an RPC call for network debugging.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Rpc(string rpcName, string details = "", UnityEngine.Object context = null)
        {
            Debug.Log($"<color=#E74C3C>[RPC]</color> {rpcName} {details}", context);
        }

        /// <summary>
        /// Require a condition to be true, or throw an exception with debug log.
        /// Use this instead of fallback code to expose bugs.
        /// </summary>
        public static void Require(bool condition, string category, string requirement, UnityEngine.Object context = null)
        {
            if (!condition)
            {
                var message = $"REQUIREMENT FAILED: {requirement}";
                Debug.LogError(Format(category, COLOR_ERROR, message), context);
                throw new System.InvalidOperationException($"[{category}] {message}");
            }
        }

        /// <summary>
        /// Require an object to not be null, or throw an exception with debug log.
        /// Returns the object if valid for fluent usage.
        /// </summary>
        public static T RequireNotNull<T>(T obj, string category, string objectName, UnityEngine.Object context = null) where T : class
        {
            if (obj == null)
            {
                var message = $"REQUIRED NULL: {objectName} is null";
                Debug.LogError(Format(category, COLOR_ERROR, message), context);
                throw new System.NullReferenceException($"[{category}] {message}");
            }
            return obj;
        }

        /// <summary>
        /// Log a configuration error and throw.
        /// Use when SerializeField is not assigned in Inspector.
        /// </summary>
        public static void RequireSerializedField<T>(T field, string fieldName, UnityEngine.Object context = null) where T : class
        {
            if (field == null)
            {
                var message = $"MISSING SERIALIZED FIELD: '{fieldName}' not assigned in Inspector";
                Debug.LogError(Format("CONFIG", COLOR_ERROR, message), context);
                throw new System.InvalidOperationException($"[CONFIG] {message}");
            }
        }

        // ============ RUNTIME LOG COLLECTION ============
        
        // Runtime log collection settings
        public static bool CollectRuntimeLogs = true;
        public static bool SaveLogsToFile = true;
        private static readonly List<RuntimeLogEntry> s_runtimeLogs = new List<RuntimeLogEntry>();
        private static string s_logFilePath;
        private static bool s_logSystemInitialized = false;
        
        [System.Serializable]
        public struct RuntimeLogEntry
        {
            public string message;
            public string category;
            public string fullMessage;
            public LogType logType;
            public DateTime timestamp;
            
            public RuntimeLogEntry(string msg, string cat, string full, LogType type)
            {
                message = msg;
                category = cat;
                fullMessage = full;
                logType = type;
                timestamp = DateTime.Now;
            }
            
            public override string ToString()
            {
                return $"[{timestamp:HH:mm:ss.fff}] [{category}] {message}";
            }
        }
        
        public static IReadOnlyList<RuntimeLogEntry> RuntimeLogs => s_runtimeLogs;
        public static event System.Action<RuntimeLogEntry> OnRuntimeLogAdded;
        
        /// <summary>
        /// Initialize the runtime log collection system.
        /// Call this early in your game startup.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void InitializeRuntimeLogCollection()
        {
            if (s_logSystemInitialized) return;
            
#if UNITY_ANDROID && !UNITY_EDITOR
            s_logFilePath = Path.Combine(Application.persistentDataPath, "debug_log.txt");
#else
            var logsDir = Path.Combine(Application.dataPath, "..", "Logs");
            if (!Directory.Exists(logsDir))
                Directory.CreateDirectory(logsDir);
            s_logFilePath = Path.Combine(logsDir, "debug_log.txt");
#endif
            
            // Subscribe to Unity's log messages to capture everything
            Application.logMessageReceived += OnUnityLogMessage;
            
            s_logSystemInitialized = true;
            Debug.Log($"[DebugLogger] Runtime log collection initialized. File: {s_logFilePath}");
        }
        
        private static void OnUnityLogMessage(string logString, string stackTrace, LogType type)
        {
            if (!CollectRuntimeLogs) return;
            
            // Try to extract category from our formatted logs
            string category = "UNITY";
            string cleanMessage = logString;
            
            // Check if this is one of our formatted logs
            if (logString.Contains("]["))
            {
                var start = logString.IndexOf('[');
                var end = logString.IndexOf(']', start);
                if (start >= 0 && end > start)
                {
                    var categoryPart = logString.Substring(start + 1, end - start - 1);
                    // Remove color tags
                    if (categoryPart.Contains(">") && categoryPart.Contains("<"))
                    {
                        var cleanStart = categoryPart.LastIndexOf('>') + 1;
                        var cleanEnd = categoryPart.IndexOf('<', cleanStart);
                        if (cleanEnd > cleanStart)
                            category = categoryPart.Substring(cleanStart, cleanEnd - cleanStart);
                        else
                            category = categoryPart.Substring(cleanStart);
                    }
                    else
                    {
                        category = categoryPart;
                    }
                    
                    // Extract clean message
                    var messageStart = logString.IndexOf(']', end) + 1;
                    if (messageStart < logString.Length)
                        cleanMessage = logString.Substring(messageStart).Trim();
                }
            }
            
            var logEntry = new RuntimeLogEntry(cleanMessage, category, logString, type);
            
            // Add to collection
            s_runtimeLogs.Add(logEntry);
            
            // Limit memory usage
            while (s_runtimeLogs.Count > 1000)
                s_runtimeLogs.RemoveAt(0);
            
            // Save to file if enabled
            if (SaveLogsToFile)
                SaveLogEntryToFile(logEntry, stackTrace);
            
            // Notify subscribers
            OnRuntimeLogAdded?.Invoke(logEntry);
        }
        
        private static void SaveLogEntryToFile(RuntimeLogEntry entry, string stackTrace = null)
        {
            try
            {
                var logBuilder = new StringBuilder();
                logBuilder.AppendLine(entry.ToString());
                
                if (!string.IsNullOrEmpty(stackTrace) && 
                    (entry.logType == LogType.Error || entry.logType == LogType.Exception))
                {
                    logBuilder.AppendLine("Stack Trace:");
                    logBuilder.AppendLine(stackTrace);
                    logBuilder.AppendLine();
                }
                
                File.AppendAllText(s_logFilePath, logBuilder.ToString());
            }
            catch (Exception e)
            {
                // Avoid infinite loop - don't use Debug.LogError here
                Console.WriteLine($"Failed to save log to file: {e.Message}");
            }
        }
        
        /// <summary>
        /// Get logs filtered by category and/or time range.
        /// </summary>
        public static List<RuntimeLogEntry> GetFilteredLogs(string category = null, DateTime? since = null, LogType? logType = null)
        {
            var filtered = new List<RuntimeLogEntry>();
            
            foreach (var log in s_runtimeLogs)
            {
                if (!string.IsNullOrEmpty(category) && !log.category.Equals(category, StringComparison.OrdinalIgnoreCase))
                    continue;
                    
                if (since.HasValue && log.timestamp < since.Value)
                    continue;
                    
                if (logType.HasValue && log.logType != logType.Value)
                    continue;
                    
                filtered.Add(log);
            }
            
            return filtered;
        }
        
        /// <summary>
        /// Get recent error logs.
        /// </summary>
        public static List<RuntimeLogEntry> GetRecentErrors(int minutes = 5)
        {
            var cutoff = DateTime.Now.AddMinutes(-minutes);
            return GetFilteredLogs(null, cutoff, LogType.Error);
        }
        
        /// <summary>
        /// Export all collected logs to a file.
        /// </summary>
        public static string ExportLogsToFile(string fileName = null)
        {
            if (string.IsNullOrEmpty(fileName))
                fileName = $"exported_debug_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                
#if UNITY_ANDROID && !UNITY_EDITOR
            var exportPath = Path.Combine(Application.persistentDataPath, fileName);
#else
            var logsDir = Path.Combine(Application.dataPath, "..", "Logs");
            if (!Directory.Exists(logsDir))
                Directory.CreateDirectory(logsDir);
            var exportPath = Path.Combine(logsDir, fileName);
#endif
            
            var export = new StringBuilder();
            export.AppendLine($"Debug Log Export - {DateTime.Now}");
            export.AppendLine($"Application: {Application.productName} v{Application.version}");
            export.AppendLine($"Unity Version: {Application.unityVersion}");
            export.AppendLine($"Platform: {Application.platform}");
            export.AppendLine($"Device Model: {SystemInfo.deviceModel}");
            export.AppendLine("=====================================\\n");
            
            foreach (var log in s_runtimeLogs)
            {
                export.AppendLine(log.ToString());
            }
            
            File.WriteAllText(exportPath, export.ToString());
            Debug.Log($"[DebugLogger] Logs exported to: {exportPath}");
            
            return exportPath;
        }
        
        /// <summary>
        /// Clear all collected runtime logs.
        /// </summary>
        public static void ClearRuntimeLogs()
        {
            s_runtimeLogs.Clear();
            Debug.Log("[DebugLogger] Runtime logs cleared");
        }
        
        /// <summary>
        /// Get log statistics by category.
        /// </summary>
        public static Dictionary<string, int> GetLogStatistics()
        {
            var stats = new Dictionary<string, int>();
            
            foreach (var log in s_runtimeLogs)
            {
                if (stats.ContainsKey(log.category))
                    stats[log.category]++;
                else
                    stats[log.category] = 1;
            }
            
            return stats;
        }
    }
}
