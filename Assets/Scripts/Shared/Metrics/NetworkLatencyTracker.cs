// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.Samples;

namespace MRMotifs.SharedActivities.Metrics
{
    /// <summary>
    /// Measures network latency using RPC ping/pong messages.
    /// Must be attached to a NetworkObject for proper functionality.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class NetworkLatencyTracker : NetworkBehaviour
    {
        #region Serialized Fields
        
        [Header("Ping Settings")]
        [Tooltip("Interval in seconds between ping messages.")]
        [SerializeField] private float m_pingInterval = 1.0f;
        
        [Tooltip("Number of samples to average for RTT calculation.")]
        [SerializeField] private int m_sampleCount = 10;
        
        [Tooltip("Timeout in seconds before considering a ping lost.")]
        [SerializeField] private float m_pingTimeout = 5.0f;
        
        [Header("Debug")]
        [Tooltip("Log ping/pong messages to console.")]
        [SerializeField] private bool m_debugLogging = false;
        
        #endregion
        
        #region Private Fields
        
        private readonly Queue<float> m_rttSamples = new();
        private readonly Dictionary<int, float> m_pendingPings = new();
        
        private float m_lastPingTime;
        private int m_pingSequence;
        private int m_pongReceived;
        private int m_pingsSent;
        private float m_minRtt = float.MaxValue;
        private float m_maxRtt = 0f;
        private float m_averageRtt;
        
        // For detecting packet loss
        private int m_totalPingsSent;
        private int m_totalPongsReceived;
        
        #endregion
        
        #region Network Lifecycle
        
        public override void Spawned()
        {
            base.Spawned();
            
            if (m_debugLogging)
            {
                Debug.Log($"[NetworkLatencyTracker] Spawned. HasStateAuthority: {Object.HasStateAuthority}");
            }
        }
        
        public override void FixedUpdateNetwork()
        {
            // Only clients send pings to measure RTT to host
            if (Object.HasStateAuthority) return;
            
            // Send ping at interval
            if (Runner.SimulationTime - m_lastPingTime >= m_pingInterval)
            {
                SendPing();
                m_lastPingTime = Runner.SimulationTime;
            }
            
            // Check for timed out pings
            CheckPingTimeouts();
        }
        
        #endregion
        
        #region Ping/Pong Logic
        
        private void SendPing()
        {
            int seq = m_pingSequence++;
            float sendTime = Time.realtimeSinceStartup;
            
            m_pendingPings[seq] = sendTime;
            m_pingsSent++;
            m_totalPingsSent++;
            
            if (m_debugLogging)
            {
                Debug.Log($"[NetworkLatencyTracker] Sending ping #{seq}");
            }
            
            RpcPing(seq, sendTime);
        }
        
        /// <summary>
        /// RPC from client to host - host immediately responds with pong.
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RpcPing(int sequence, float clientSendTime, RpcInfo info = default)
        {
            if (m_debugLogging)
            {
                Debug.Log($"[NetworkLatencyTracker] Host received ping #{sequence} from {info.Source}");
            }
            
            // Send pong back to the sender
            RpcPong(sequence, clientSendTime, info.Source);
        }
        
        /// <summary>
        /// RPC from host back to client with the original timestamp.
        /// </summary>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcPong(int sequence, float originalSendTime, PlayerRef targetPlayer)
        {
            // Only process if this is for us
            if (Runner.LocalPlayer != targetPlayer) return;
            
            if (!m_pendingPings.ContainsKey(sequence))
            {
                if (m_debugLogging)
                {
                    Debug.LogWarning($"[NetworkLatencyTracker] Received pong for unknown ping #{sequence}");
                }
                return;
            }
            
            float sendTime = m_pendingPings[sequence];
            float rtt = (Time.realtimeSinceStartup - sendTime) * 1000f; // Convert to ms
            
            m_pendingPings.Remove(sequence);
            m_pongReceived++;
            m_totalPongsReceived++;
            
            RecordRttSample(rtt);
            
            if (m_debugLogging)
            {
                Debug.Log($"[NetworkLatencyTracker] Pong #{sequence} RTT: {rtt:F2}ms");
            }
        }
        
        private void RecordRttSample(float rtt)
        {
            m_rttSamples.Enqueue(rtt);
            
            while (m_rttSamples.Count > m_sampleCount)
            {
                m_rttSamples.Dequeue();
            }
            
            // Update statistics
            m_minRtt = Mathf.Min(m_minRtt, rtt);
            m_maxRtt = Mathf.Max(m_maxRtt, rtt);
            
            // Calculate average
            float sum = 0f;
            foreach (float sample in m_rttSamples)
            {
                sum += sample;
            }
            m_averageRtt = sum / m_rttSamples.Count;
        }
        
        private void CheckPingTimeouts()
        {
            float currentTime = Time.realtimeSinceStartup;
            var timedOutPings = new List<int>();
            
            foreach (var kvp in m_pendingPings)
            {
                if (currentTime - kvp.Value > m_pingTimeout)
                {
                    timedOutPings.Add(kvp.Key);
                }
            }
            
            foreach (int seq in timedOutPings)
            {
                m_pendingPings.Remove(seq);
                
                if (m_debugLogging)
                {
                    Debug.LogWarning($"[NetworkLatencyTracker] Ping #{seq} timed out");
                }
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Get the average round-trip time in milliseconds.
        /// </summary>
        public float GetAverageRtt()
        {
            return m_averageRtt;
        }
        
        /// <summary>
        /// Get the minimum observed RTT in milliseconds.
        /// </summary>
        public float GetMinRtt()
        {
            return m_minRtt < float.MaxValue ? m_minRtt : 0f;
        }
        
        /// <summary>
        /// Get the maximum observed RTT in milliseconds.
        /// </summary>
        public float GetMaxRtt()
        {
            return m_maxRtt;
        }
        
        /// <summary>
        /// Get the current RTT jitter (max - min).
        /// </summary>
        public float GetJitter()
        {
            if (m_minRtt >= float.MaxValue) return 0f;
            return m_maxRtt - m_minRtt;
        }
        
        /// <summary>
        /// Get the packet loss percentage.
        /// </summary>
        public float GetPacketLossPercentage()
        {
            if (m_totalPingsSent == 0) return 0f;
            
            int lost = m_totalPingsSent - m_totalPongsReceived - m_pendingPings.Count;
            return (float)lost / m_totalPingsSent * 100f;
        }
        
        /// <summary>
        /// Get the number of pending (unanswered) pings.
        /// </summary>
        public int GetPendingPingCount()
        {
            return m_pendingPings.Count;
        }
        
        /// <summary>
        /// Get detailed latency statistics as a formatted string.
        /// </summary>
        public string GetStatisticsString()
        {
            return $"RTT: Avg={m_averageRtt:F1}ms Min={GetMinRtt():F1}ms Max={m_maxRtt:F1}ms | " +
                   $"Jitter={GetJitter():F1}ms | Loss={GetPacketLossPercentage():F2}%";
        }
        
        /// <summary>
        /// Reset all statistics.
        /// </summary>
        public void ResetStatistics()
        {
            m_rttSamples.Clear();
            m_pendingPings.Clear();
            m_minRtt = float.MaxValue;
            m_maxRtt = 0f;
            m_averageRtt = 0f;
            m_totalPingsSent = 0;
            m_totalPongsReceived = 0;
            m_pingsSent = 0;
            m_pongReceived = 0;
            
            Debug.Log("[NetworkLatencyTracker] Statistics reset.");
        }
        
        #endregion
    }
}
#endif
