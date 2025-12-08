// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using UnityEngine;
using Meta.XR.Samples;

namespace MRMotifs.Shooting
{
    /// <summary>
    /// Handles game input for restart gestures and other game controls.
    /// Extracted from ShootingGameManagerMotif to separate input concerns.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class GameInputHandler : MonoBehaviour
    {
        [Header("Input Settings")]
        [Tooltip("Duration to hold both grip buttons to trigger restart.")]
        [SerializeField] private float m_restartHoldDuration = 2f;

        [Tooltip("Show visual feedback during restart hold.")]
        [SerializeField] private bool m_showRestartFeedback = true;

        private float m_restartHoldTime;
        private ShootingGameManagerMotif m_gameManager;

        private void Awake()
        {
            // Subscribe to restart requests from event bus
            GameStateEventBus.OnRestartRequested += OnRestartRequested;
        }

        private void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            GameStateEventBus.OnRestartRequested -= OnRestartRequested;
        }

        private void Start()
        {
            m_gameManager = FindAnyObjectByType<ShootingGameManagerMotif>();
        }

        private void Update()
        {
            CheckRestartInput();
        }

        private void CheckRestartInput()
        {
            // Hold both grip buttons to restart (works in RoundEnd state or during gameplay)
            bool leftGrip = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);
            bool rightGrip = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);

            if (leftGrip && rightGrip)
            {
                m_restartHoldTime += Time.deltaTime;

                // Show progress feedback
                if (m_showRestartFeedback)
                {
                    ShowRestartProgress(m_restartHoldTime / m_restartHoldDuration);
                }

                if (m_restartHoldTime >= m_restartHoldDuration)
                {
                    m_restartHoldTime = 0f;
                    HideRestartProgress();
                    
                    // Fire restart request through event bus
                    GameStateEventBus.FireRestartRequested();
                }
            }
            else
            {
                if (m_restartHoldTime > 0)
                {
                    HideRestartProgress();
                }
                m_restartHoldTime = 0f;
            }
        }

        private void OnRestartRequested()
        {
            // Handle the restart request based on current game state
            if (m_gameManager == null) return;

            switch (m_gameManager.CurrentGameState)
            {
                case ShootingGameManagerMotif.GameState.RoundEnd:
                    m_gameManager.RestartRound();
                    Debug.Log("[GameInputHandler] Restart round requested via dual-grip gesture");
                    break;

                case ShootingGameManagerMotif.GameState.Playing:
                    m_gameManager.ResetCurrentRound();
                    Debug.Log("[GameInputHandler] Reset round requested via dual-grip gesture");
                    break;

                default:
                    Debug.Log("[GameInputHandler] Restart gesture ignored in current game state");
                    break;
            }
        }

        private void ShowRestartProgress(float progress)
        {
            // TODO: Implement visual feedback for restart progress
            // This could show a progress bar or radial fill around hands
            // For now, just debug output
            if (Time.frameCount % 30 == 0) // Log every half second at 60fps
            {
                Debug.Log($"[GameInputHandler] Restart progress: {progress:P0}");
            }
        }

        private void HideRestartProgress()
        {
            // TODO: Hide visual feedback
            // For now, just ensure we're not spamming debug logs
        }

        /// <summary>
        /// Public method to manually trigger restart (for UI buttons, etc.)
        /// </summary>
        public void TriggerRestart()
        {
            GameStateEventBus.FireRestartRequested();
        }

        /// <summary>
        /// Check if restart gesture is currently being held.
        /// </summary>
        public bool IsRestartGestureActive()
        {
            return m_restartHoldTime > 0;
        }

        /// <summary>
        /// Get the current restart progress (0 to 1).
        /// </summary>
        public float GetRestartProgress()
        {
            return m_restartHoldTime / m_restartHoldDuration;
        }
    }
}
#endif