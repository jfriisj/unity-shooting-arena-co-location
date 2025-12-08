// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using UnityEngine;
using TMPro;
using Fusion;
using Meta.XR.Samples;

namespace MRMotifs.Shooting
{
    /// <summary>
    /// Handles all game state UI updates by subscribing to GameStateEventBus events.
    /// Extracted from ShootingGameManagerMotif to follow single responsibility principle.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class GameStateUIHandler : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Text showing game status.")]
        [SerializeField] private TextMeshProUGUI m_statusText;

        [Tooltip("Text showing remaining time.")]
        [SerializeField] private TextMeshProUGUI m_timerText;

        private ShootingGameManagerMotif m_gameManager;

        private void Awake()
        {
            // Subscribe to events before any other component can fire them
            GameStateEventBus.OnGameStateChanged += OnGameStateChanged;
            GameStateEventBus.OnCountdownTick += OnCountdownTick;
            GameStateEventBus.OnTimerUpdated += OnTimerUpdated;
            GameStateEventBus.OnWinnerAnnounced += OnWinnerAnnounced;
        }

        private void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            GameStateEventBus.OnGameStateChanged -= OnGameStateChanged;
            GameStateEventBus.OnCountdownTick -= OnCountdownTick;
            GameStateEventBus.OnTimerUpdated -= OnTimerUpdated;
            GameStateEventBus.OnWinnerAnnounced -= OnWinnerAnnounced;
        }

        private void Start()
        {
            m_gameManager = FindAnyObjectByType<ShootingGameManagerMotif>();
            
            if (m_statusText == null || m_timerText == null)
            {
                AutoFindUIElements();
            }

            // Initialize UI immediately
            if (m_gameManager != null)
            {
                UpdateStatusForCurrentState(m_gameManager.CurrentGameState);
                UpdateTimer(m_gameManager.RemainingTime);
            }
        }

        private void AutoFindUIElements()
        {
            if (m_statusText == null)
            {
                var statusTextObj = GameObject.Find("StatusText");
                if (statusTextObj != null)
                {
                    m_statusText = statusTextObj.GetComponent<TextMeshProUGUI>();
                }
            }

            if (m_timerText == null)
            {
                var timerTextObj = GameObject.Find("TimerText");
                if (timerTextObj != null)
                {
                    m_timerText = timerTextObj.GetComponent<TextMeshProUGUI>();
                }
            }

            if (m_statusText == null || m_timerText == null)
            {
                Debug.LogWarning("[GameStateUIHandler] Could not find UI text elements. Please assign them manually.");
            }
        }

        // Event handlers

        private void OnGameStateChanged(ShootingGameManagerMotif.GameState oldState, ShootingGameManagerMotif.GameState newState)
        {
            UpdateStatusForCurrentState(newState);
        }

        private void OnCountdownTick(int countdownValue)
        {
            if (m_statusText != null)
            {
                m_statusText.text = countdownValue > 0 ? $"Starting in {countdownValue}..." : "GO!";
            }
        }

        private void OnTimerUpdated(float remainingTime)
        {
            UpdateTimer(remainingTime);
        }

        private void OnWinnerAnnounced(string winnerMessage)
        {
            if (m_statusText != null)
            {
                m_statusText.text = winnerMessage;
            }
        }

        // UI Update methods

        private void UpdateStatusForCurrentState(ShootingGameManagerMotif.GameState gameState)
        {
            if (m_statusText == null || m_gameManager == null)
            {
                return;
            }

            switch (gameState)
            {
                case ShootingGameManagerMotif.GameState.WaitingForPlayers:
                    var activePlayerCount = GetActivePlayerCount();
                    var minPlayers = GetMinPlayersToStart();
                    m_statusText.text = $"Waiting for players ({activePlayerCount}/{minPlayers})";
                    break;

                case ShootingGameManagerMotif.GameState.Countdown:
                    // Will be updated by OnCountdownTick
                    break;

                case ShootingGameManagerMotif.GameState.Playing:
                    m_statusText.text = "FIGHT!";
                    break;

                case ShootingGameManagerMotif.GameState.RoundEnd:
                    // Will be updated by OnWinnerAnnounced
                    break;
            }
        }

        private void UpdateTimer(float remainingTime)
        {
            if (m_timerText != null)
            {
                var minutes = Mathf.FloorToInt(remainingTime / 60);
                var seconds = Mathf.FloorToInt(remainingTime % 60);
                m_timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }

        // Helper methods to get data from GameManager without tight coupling

        private int GetActivePlayerCount()
        {
            if (m_gameManager?.Runner == null) return 0;
            
            int count = 0;
            foreach (var _ in m_gameManager.Runner.ActivePlayers)
            {
                count++;
            }
            return count;
        }

        private int GetMinPlayersToStart()
        {
            // Use reflection to access private field from GameManager
            if (m_gameManager == null) return 2;

            var type = typeof(ShootingGameManagerMotif);
            var field = type.GetField("m_minPlayersToStart", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            return field != null ? (int)field.GetValue(m_gameManager) : 2;
        }

        /// <summary>
        /// Public method to show custom status messages (for round reset, etc.)
        /// </summary>
        public void ShowStatus(string message)
        {
            if (m_statusText != null)
            {
                m_statusText.text = message;
            }
        }
    }
}
#endif