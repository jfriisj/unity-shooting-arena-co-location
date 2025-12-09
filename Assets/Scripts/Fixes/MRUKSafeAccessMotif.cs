// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using Meta.XR.MRUtilityKit;
using Meta.XR.Samples;

namespace MRMotifs.Fixes
{
    /// <summary>
    /// Provides safe access to MRUK singleton methods to prevent null reference exceptions.
    /// Attach to any GameObject that needs to access MRUK rooms safely.
    /// </summary>
    [MetaCodeSample("MRMotifs-Fixes")]
    public class MRUKSafeAccessMotif : MonoBehaviour
    {
        [Header("Safety Settings")]
        [Tooltip("Enable debug logging for MRUK access attempts")]
        public bool enableDebugLogging = false;
        
        [Tooltip("Maximum time to wait for MRUK initialization (seconds)")]
        public float maxWaitTime = 10f;
        
        private float m_startTime;
        private bool m_isWaitingForMRUK = false;
        
        /// <summary>
        /// Safely get the current room from MRUK, returns null if not available.
        /// </summary>
        public static MRUKRoom GetCurrentRoomSafe()
        {
            if (MRUK.Instance == null)
            {
                return null;
            }
            
            try
            {
                return MRUK.Instance.GetCurrentRoom();
            }
            catch (System.Exception)
            {
                // Return null instead of throwing exception
                return null;
            }
        }
        
        /// <summary>
        /// Check if MRUK is properly initialized with rooms.
        /// </summary>
        public static bool IsMRUKReady()
        {
            return MRUK.Instance != null && 
                   MRUK.Instance.Rooms != null && 
                   MRUK.Instance.Rooms.Count > 0;
        }
        
        /// <summary>
        /// Get the number of available rooms safely.
        /// </summary>
        public static int GetRoomCountSafe()
        {
            if (MRUK.Instance?.Rooms == null)
                return 0;
                
            return MRUK.Instance.Rooms.Count;
        }
        
        private void Start()
        {
            m_startTime = Time.time;
            m_isWaitingForMRUK = true;
            
            if (enableDebugLogging)
                Debug.Log($"[MRUKSafeAccess] Starting MRUK readiness check on {gameObject.name}");
        }
        
        private void Update()
        {
            if (!m_isWaitingForMRUK)
                return;
                
            if (IsMRUKReady())
            {
                m_isWaitingForMRUK = false;
                OnMRUKReady();
            }
            else if (Time.time - m_startTime > maxWaitTime)
            {
                m_isWaitingForMRUK = false;
                OnMRUKTimeout();
            }
        }
        
        /// <summary>
        /// Called when MRUK becomes ready with rooms.
        /// Override in derived classes for custom behavior.
        /// </summary>
        protected virtual void OnMRUKReady()
        {
            if (enableDebugLogging)
                Debug.Log($"[MRUKSafeAccess] MRUK is ready with {GetRoomCountSafe()} rooms on {gameObject.name}");
        }
        
        /// <summary>
        /// Called when MRUK doesn't become ready within maxWaitTime.
        /// Override in derived classes for custom behavior.
        /// </summary>
        protected virtual void OnMRUKTimeout()
        {
            if (enableDebugLogging)
                Debug.LogWarning($"[MRUKSafeAccess] MRUK timeout after {maxWaitTime}s on {gameObject.name}");
        }
    }
}