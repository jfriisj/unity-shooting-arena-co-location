// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace MRMotifs.SharedActivities
{
    /// <summary>
    /// Displays an experiment timer in the scene, showing elapsed time since session start.
    /// Attaches to a world-space canvas for VR visibility.
    /// </summary>
    public class ExperimentTimerMotif : MonoBehaviour
    {
        [Header("Timer Settings")]
        [SerializeField] private float m_targetDurationMinutes = 5f;
        [SerializeField] private bool m_showTargetProgress = true;
        [SerializeField] private bool m_autoStartTimer = true;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI m_timerText;
        [SerializeField] private Image m_progressBar;

        [Header("Visual Feedback")]
        [SerializeField] private Color m_normalColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color m_warningColor = new Color(0.9f, 0.7f, 0.1f, 1f);
        [SerializeField] private Color m_completeColor = new Color(0.2f, 0.6f, 1f, 1f);

        private float m_elapsedTime;
        private bool m_isRunning;
        private DateTime m_startTime;

        public float ElapsedTime => m_elapsedTime;
        public float ElapsedMinutes => m_elapsedTime / 60f;
        public bool IsRunning => m_isRunning;
        public float TargetDurationMinutes => m_targetDurationMinutes;

        public static ExperimentTimerMotif Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (m_autoStartTimer)
            {
                StartTimer();
            }

            // Auto-find UI components if not assigned
            if (m_timerText == null)
            {
                m_timerText = GetComponentInChildren<TextMeshProUGUI>();
            }

            UpdateDisplay();
        }

        private void Update()
        {
            if (m_isRunning)
            {
                m_elapsedTime += Time.deltaTime;
                UpdateDisplay();
            }
        }

        /// <summary>
        /// Start the experiment timer
        /// </summary>
        public void StartTimer()
        {
            m_isRunning = true;
            m_startTime = DateTime.Now;
            m_elapsedTime = 0f;
            Debug.Log("[ExperimentTimer] Timer started");
        }

        /// <summary>
        /// Stop the experiment timer
        /// </summary>
        public void StopTimer()
        {
            m_isRunning = false;
            Debug.Log($"[ExperimentTimer] Timer stopped at {FormatTime(m_elapsedTime)}");
        }

        /// <summary>
        /// Reset the timer to zero
        /// </summary>
        public void ResetTimer()
        {
            m_elapsedTime = 0f;
            m_isRunning = false;
            UpdateDisplay();
            Debug.Log("[ExperimentTimer] Timer reset");
        }

        /// <summary>
        /// Set the target duration for the experiment
        /// </summary>
        public void SetTargetDuration(float minutes)
        {
            m_targetDurationMinutes = minutes;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (m_timerText != null)
            {
                string timeStr = FormatTime(m_elapsedTime);
                
                if (m_showTargetProgress)
                {
                    float targetSeconds = m_targetDurationMinutes * 60f;
                    float remaining = Mathf.Max(0, targetSeconds - m_elapsedTime);
                    string remainingStr = FormatTime(remaining);
                    m_timerText.text = $"{timeStr}\n<size=60%>Target: {m_targetDurationMinutes:F0}min | Remaining: {remainingStr}</size>";
                }
                else
                {
                    m_timerText.text = timeStr;
                }

                // Update color based on progress
                float progress = m_elapsedTime / (m_targetDurationMinutes * 60f);
                if (progress >= 1f)
                {
                    m_timerText.color = m_completeColor;
                }
                else if (progress >= 0.8f)
                {
                    m_timerText.color = m_warningColor;
                }
                else
                {
                    m_timerText.color = m_normalColor;
                }
            }

            if (m_progressBar != null)
            {
                float progress = Mathf.Clamp01(m_elapsedTime / (m_targetDurationMinutes * 60f));
                m_progressBar.fillAmount = progress;

                if (progress >= 1f)
                {
                    m_progressBar.color = m_completeColor;
                }
                else if (progress >= 0.8f)
                {
                    m_progressBar.color = m_warningColor;
                }
                else
                {
                    m_progressBar.color = m_normalColor;
                }
            }
        }

        private string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes:D2}:{secs:D2}";
        }

        /// <summary>
        /// Get formatted elapsed time string
        /// </summary>
        public string GetFormattedTime()
        {
            return FormatTime(m_elapsedTime);
        }

        /// <summary>
        /// Check if target duration has been reached
        /// </summary>
        public bool HasReachedTarget()
        {
            return m_elapsedTime >= (m_targetDurationMinutes * 60f);
        }
    }
}
