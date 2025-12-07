// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using Meta.XR.Samples;

namespace MRMotifs.SharedActivities.Startup
{
    /// <summary>
    /// Configuration settings for the game startup flow.
    /// Defines timeouts and behavior for host/client initialization.
    /// </summary>
    [CreateAssetMenu(fileName = "StartupFlowConfig", menuName = "MRMotifs/Startup Flow Config")]
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class StartupFlowConfig : ScriptableObject
    {
        [Header("Timeouts")]
        [Tooltip("General timeout for each step in seconds.")]
        [SerializeField] private float m_stepTimeout = 30f;

        [Tooltip("Timeout waiting for anchor discovery and localization.")]
        [SerializeField] private float m_anchorWaitTimeout = 60f;

        [Tooltip("Timeout waiting for room mesh to load.")]
        [SerializeField] private float m_roomLoadTimeout = 30f;

        [Tooltip("Timeout waiting for network session to be created/joined.")]
        [SerializeField] private float m_networkTimeout = 30f;

        [Header("Behavior")]
        [Tooltip("Automatically prompt for room scan if no scene exists (host only).")]
        [SerializeField] private bool m_autoPromptRoomScan = true;

        [Tooltip("Skip room sharing step if disabled.")]
        [SerializeField] private bool m_enableRoomSharing = true;

        [Tooltip("Show detailed debug logs during startup.")]
        [SerializeField] private bool m_showDebugLogs = true;

        [Header("UI Settings")]
        [Tooltip("Delay before hiding the modal after startup completes.")]
        [SerializeField] private float m_modalHideDelay = 1f;

        [Tooltip("Animate progress bar smoothly between steps.")]
        [SerializeField] private bool m_smoothProgressBar = true;

        // Public accessors
        public float StepTimeout => m_stepTimeout;
        public float AnchorWaitTimeout => m_anchorWaitTimeout;
        public float RoomLoadTimeout => m_roomLoadTimeout;
        public float NetworkTimeout => m_networkTimeout;
        public bool AutoPromptRoomScan => m_autoPromptRoomScan;
        public bool EnableRoomSharing => m_enableRoomSharing;
        public bool ShowDebugLogs => m_showDebugLogs;
        public float ModalHideDelay => m_modalHideDelay;
        public bool SmoothProgressBar => m_smoothProgressBar;
    }
}
