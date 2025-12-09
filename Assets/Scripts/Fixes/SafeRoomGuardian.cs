// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using Meta.XR.MRUtilityKit;
using Meta.XR.Samples;

namespace MRMotifs.Fixes
{
    /// <summary>
    /// A safe wrapper for RoomGuardian that prevents null reference exceptions
    /// when MRUK is not yet initialized. Replace or supplement the original RoomGuardian.
    /// </summary>
    [MetaCodeSample("MRMotifs-Fixes")]
    public class SafeRoomGuardian : MonoBehaviour
    {
        [Header("Room Guardian Settings")]
        [Tooltip("Enable the room guardian boundary visualization")]
        public bool enableBoundaryVisualization = true;
        
        [Tooltip("Material for the room boundary visualization")]
        public Material boundaryMaterial;
        
        [Tooltip("Height of the boundary visualization")]
        public float boundaryHeight = 2.5f;
        
        [Tooltip("Check for MRUK readiness every N seconds")]
        public float checkInterval = 1f;
        
        private bool m_isActive = false;
        private float m_lastCheckTime = 0f;
        private RoomGuardian m_originalRoomGuardian;
        
        private void Start()
        {
            // Disable original room guardian to prevent errors
            m_originalRoomGuardian = GetComponent<RoomGuardian>();
            if (m_originalRoomGuardian != null)
            {
                m_originalRoomGuardian.enabled = false;
                Debug.Log("[SafeRoomGuardian] Disabled original RoomGuardian to prevent null reference errors");
            }
        }
        
        private void Update()
        {
            // Check MRUK readiness periodically
            if (Time.time - m_lastCheckTime >= checkInterval)
            {
                m_lastCheckTime = Time.time;
                CheckMRUKReadiness();
            }
        }
        
        private void CheckMRUKReadiness()
        {
            if (m_isActive)
                return;
                
            // Check if MRUK is ready
            if (MRUK.Instance != null && 
                MRUK.Instance.Rooms != null && 
                MRUK.Instance.Rooms.Count > 0)
            {
                // MRUK is ready, enable original room guardian
                if (m_originalRoomGuardian != null)
                {
                    m_originalRoomGuardian.enabled = true;
                    Debug.Log("[SafeRoomGuardian] MRUK is ready, enabled original RoomGuardian");
                }
                
                m_isActive = true;
                
                // Disable this component as it's no longer needed
                this.enabled = false;
            }
        }
        
        /// <summary>
        /// Force check MRUK readiness (for debugging)
        /// </summary>
        [ContextMenu("Force Check MRUK")]
        public void ForceCheckMRUK()
        {
            Debug.Log($"[SafeRoomGuardian] Force check - MRUK Instance: {(MRUK.Instance != null ? "Available" : "Null")}");
            if (MRUK.Instance != null)
            {
                Debug.Log($"[SafeRoomGuardian] MRUK Rooms: {(MRUK.Instance.Rooms != null ? MRUK.Instance.Rooms.Count.ToString() : "Null")}");
            }
            CheckMRUKReadiness();
        }
    }
}