// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using System.Diagnostics;
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
        public static void Bullet(string message, Object context = null)
        {
            if (EnableBullet)
                Debug.Log(Format("BULLET", COLOR_BULLET, message), context);
        }

        /// <summary>
        /// Log player-related messages.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Player(string message, Object context = null)
        {
            if (EnablePlayer)
                Debug.Log(Format("PLAYER", COLOR_PLAYER, message), context);
        }

        /// <summary>
        /// Log colocation-related messages.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Colocation(string message, Object context = null)
        {
            if (EnableColocation)
                Debug.Log(Format("COLOCATION", COLOR_COLOCATION, message), context);
        }

        /// <summary>
        /// Log network-related messages.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Network(string message, Object context = null)
        {
            if (EnableNetwork)
                Debug.Log(Format("NETWORK", COLOR_NETWORK, message), context);
        }

        /// <summary>
        /// Log health-related messages.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Health(string message, Object context = null)
        {
            if (EnableHealth)
                Debug.Log(Format("HEALTH", COLOR_HEALTH, message), context);
        }

        /// <summary>
        /// Log game-related messages.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Game(string message, Object context = null)
        {
            if (EnableGame)
                Debug.Log(Format("GAME", COLOR_GAME, message), context);
        }

        /// <summary>
        /// Log shooting-related messages (drones, bullets, combat).
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Shooting(string message, Object context = null)
        {
            if (EnableShooting)
                Debug.Log(Format("SHOOTING", COLOR_SHOOTING, message), context);
        }

        /// <summary>
        /// Log warning with category.
        /// </summary>
        public static void Warning(string category, string message, Object context = null)
        {
            Debug.LogWarning(Format(category, COLOR_WARNING, message), context);
        }

        /// <summary>
        /// Log error with category.
        /// </summary>
        public static void Error(string category, string message, Object context = null)
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
        public static void StateChange(string category, string from, string to, Object context = null)
        {
            Debug.Log($"<color=#9B59B6>[{category}]</color> State: {from} → {to}", context);
        }

        /// <summary>
        /// Log an RPC call for network debugging.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Rpc(string rpcName, string details = "", Object context = null)
        {
            Debug.Log($"<color=#E74C3C>[RPC]</color> {rpcName} {details}", context);
        }

        /// <summary>
        /// Require a condition to be true, or throw an exception with debug log.
        /// Use this instead of fallback code to expose bugs.
        /// </summary>
        public static void Require(bool condition, string category, string requirement, Object context = null)
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
        public static T RequireNotNull<T>(T obj, string category, string objectName, Object context = null) where T : class
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
        public static void RequireSerializedField<T>(T field, string fieldName, Object context = null) where T : class
        {
            if (field == null)
            {
                var message = $"MISSING SERIALIZED FIELD: '{fieldName}' not assigned in Inspector";
                Debug.LogError(Format("CONFIG", COLOR_ERROR, message), context);
                throw new System.InvalidOperationException($"[CONFIG] {message}");
            }
        }
    }
}
