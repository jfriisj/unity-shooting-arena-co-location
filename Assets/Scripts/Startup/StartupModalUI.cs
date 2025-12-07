// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Meta.XR.Samples;

namespace MRMotifs.SharedActivities.Startup
{
    /// <summary>
    /// UI component for displaying the startup modal during game initialization.
    /// Shows current state, progress, and allows error recovery.
    /// 
    /// Structure:
    /// - Title text (e.g., "Setting Up Game...")
    /// - Status text (e.g., "Checking room scan...")
    /// - Progress bar or spinner
    /// - Step indicators (checkmarks for completed steps)
    /// - Error panel with retry button
    /// - Room scan button (for host if needed)
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class StartupModalUI : MonoBehaviour
    {
        [Header("Main Panel")]
        [SerializeField] private GameObject m_modalPanel;
        [SerializeField] private CanvasGroup m_canvasGroup;

        [Header("Text Elements")]
        [SerializeField] private TMP_Text m_titleText;
        [SerializeField] private TMP_Text m_statusText;
        [SerializeField] private TMP_Text m_roleText;

        [Header("Progress")]
        [SerializeField] private Slider m_progressBar;
        [SerializeField] private GameObject m_spinner;

        [Header("Step Indicators")]
        [SerializeField] private StepIndicator[] m_hostSteps;
        [SerializeField] private StepIndicator[] m_clientSteps;
        [SerializeField] private GameObject m_hostStepsContainer;
        [SerializeField] private GameObject m_clientStepsContainer;

        [Header("Error Panel")]
        [SerializeField] private GameObject m_errorPanel;
        [SerializeField] private TMP_Text m_errorText;
        [SerializeField] private Button m_retryButton;

        [Header("Action Buttons")]
        [SerializeField] private Button m_scanRoomButton;

        [Header("Animation")]
        [SerializeField] private float m_fadeSpeed = 2f;
        [SerializeField] private float m_progressSmoothSpeed = 5f;

        // Reference to startup manager
        private GameStartupManagerMotif m_startupManager;
        private float m_targetProgress;
        private float m_currentProgress;
        private bool m_isVisible = true;

        [System.Serializable]
        public class StepIndicator
        {
            public GameObject stepObject;
            public Image iconImage;
            public TMP_Text labelText;
            public Sprite pendingIcon;
            public Sprite inProgressIcon;
            public Sprite completedIcon;
            public Color pendingColor = Color.gray;
            public Color inProgressColor = Color.yellow;
            public Color completedColor = Color.green;
        }

        private void Awake()
        {
            // Find startup manager
            m_startupManager = FindAnyObjectByType<GameStartupManagerMotif>();

            // Setup button listeners
            if (m_retryButton != null)
            {
                m_retryButton.onClick.AddListener(OnRetryClicked);
            }

            if (m_scanRoomButton != null)
            {
                m_scanRoomButton.onClick.AddListener(OnScanRoomClicked);
            }

            // Initialize UI
            if (m_errorPanel != null) m_errorPanel.SetActive(false);
            if (m_scanRoomButton != null) m_scanRoomButton.gameObject.SetActive(false);
            if (m_spinner != null) m_spinner.SetActive(true);
        }

        private void Update()
        {
            // Smooth progress bar animation
            if (m_progressBar != null && m_startupManager != null && m_startupManager.Config.SmoothProgressBar)
            {
                m_currentProgress = Mathf.Lerp(m_currentProgress, m_targetProgress, Time.deltaTime * m_progressSmoothSpeed);
                m_progressBar.value = m_currentProgress;
            }

            // Handle canvas group fade
            if (m_canvasGroup != null)
            {
                float targetAlpha = m_isVisible ? 1f : 0f;
                m_canvasGroup.alpha = Mathf.MoveTowards(m_canvasGroup.alpha, targetAlpha, Time.deltaTime * m_fadeSpeed);

                if (!m_isVisible && m_canvasGroup.alpha <= 0f)
                {
                    m_modalPanel.SetActive(false);
                }
            }

            // Rotate spinner
            if (m_spinner != null && m_spinner.activeSelf)
            {
                m_spinner.transform.Rotate(0, 0, -180f * Time.deltaTime);
            }
        }

        /// <summary>
        /// Update the UI to reflect the current startup state.
        /// </summary>
        public void UpdateState(StartupState state, string statusMessage, float progress, bool isHost)
        {
            // Show modal if hidden
            if (m_modalPanel != null && !m_modalPanel.activeSelf)
            {
                m_modalPanel.SetActive(true);
                m_isVisible = true;
            }

            // Update title based on role
            if (m_titleText != null)
            {
                m_titleText.text = state == StartupState.Error 
                    ? "Setup Failed" 
                    : (state == StartupState.Ready ? "Ready!" : "Setting Up Game...");
            }

            // Update role text
            if (m_roleText != null)
            {
                m_roleText.text = isHost ? "HOST" : "CLIENT";
                m_roleText.color = isHost ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.2f, 0.6f, 1f);
            }

            // Update status text
            if (m_statusText != null)
            {
                m_statusText.text = statusMessage;
            }

            // Update progress
            m_targetProgress = progress;
            if (m_progressBar != null && !(m_startupManager != null && m_startupManager.Config.SmoothProgressBar))
            {
                m_progressBar.value = progress;
            }

            // Show/hide step containers based on role
            if (m_hostStepsContainer != null) m_hostStepsContainer.SetActive(isHost);
            if (m_clientStepsContainer != null) m_clientStepsContainer.SetActive(!isHost);

            // Update step indicators
            UpdateStepIndicators(state, isHost);

            // Handle error state
            if (m_errorPanel != null)
            {
                m_errorPanel.SetActive(state == StartupState.Error);
            }

            if (m_errorText != null && state == StartupState.Error)
            {
                m_errorText.text = m_startupManager?.ErrorMessage ?? "An error occurred";
            }

            // Show/hide spinner
            if (m_spinner != null)
            {
                m_spinner.SetActive(state != StartupState.Ready && state != StartupState.Error);
            }

            // Show scan room button when prompting for scan
            if (m_scanRoomButton != null)
            {
                m_scanRoomButton.gameObject.SetActive(isHost && state == StartupState.PromptingRoomScan);
            }
        }

        private void UpdateStepIndicators(StartupState currentState, bool isHost)
        {
            var steps = isHost ? m_hostSteps : m_clientSteps;
            if (steps == null || steps.Length == 0) return;

            // Define step states for host
            // Host: RoomScan -> Session -> Anchor -> RoomShare -> Ready
            // Client: Join -> WaitAnchor -> Localize -> LoadRoom -> Ready

            int currentStepIndex = GetStepIndex(currentState, isHost);

            for (int i = 0; i < steps.Length; i++)
            {
                var step = steps[i];
                if (step.stepObject == null) continue;

                StepState stepState;
                if (i < currentStepIndex)
                {
                    stepState = StepState.Completed;
                }
                else if (i == currentStepIndex)
                {
                    stepState = currentState == StartupState.Error ? StepState.Error : StepState.InProgress;
                }
                else
                {
                    stepState = StepState.Pending;
                }

                ApplyStepState(step, stepState);
            }
        }

        private enum StepState
        {
            Pending,
            InProgress,
            Completed,
            Error
        }

        private int GetStepIndex(StartupState state, bool isHost)
        {
            if (isHost)
            {
                return state switch
                {
                    StartupState.Initializing => 0,
                    StartupState.CheckingRoomScan => 0,
                    StartupState.PromptingRoomScan => 0,
                    StartupState.CreatingSession => 1,
                    StartupState.SharingRoom => 2,
                    StartupState.Ready => 3,
                    StartupState.Error => -1,
                    _ => 0
                };
            }
            else
            {
                return state switch
                {
                    StartupState.Initializing => 0,
                    StartupState.JoiningSession => 0,
                    StartupState.WaitingForAnchor => 1,
                    StartupState.LocalizingAnchor => 2,
                    StartupState.LoadingRoom => 3,
                    StartupState.Ready => 4,
                    StartupState.Error => -1,
                    _ => 0
                };
            }
        }

        private void ApplyStepState(StepIndicator step, StepState state)
        {
            if (step.iconImage != null)
            {
                step.iconImage.sprite = state switch
                {
                    StepState.Completed => step.completedIcon,
                    StepState.InProgress => step.inProgressIcon,
                    _ => step.pendingIcon
                };

                step.iconImage.color = state switch
                {
                    StepState.Completed => step.completedColor,
                    StepState.InProgress => step.inProgressColor,
                    StepState.Error => Color.red,
                    _ => step.pendingColor
                };
            }

            if (step.labelText != null)
            {
                step.labelText.color = state switch
                {
                    StepState.Completed => step.completedColor,
                    StepState.InProgress => Color.white,
                    StepState.Error => Color.red,
                    _ => step.pendingColor
                };
            }
        }

        /// <summary>
        /// Show the modal.
        /// </summary>
        public void Show()
        {
            m_isVisible = true;
            if (m_modalPanel != null)
            {
                m_modalPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Hide the modal with fade animation.
        /// </summary>
        public void Hide()
        {
            m_isVisible = false;
        }

        /// <summary>
        /// Immediately hide the modal without animation.
        /// </summary>
        public void HideImmediate()
        {
            m_isVisible = false;
            if (m_canvasGroup != null)
            {
                m_canvasGroup.alpha = 0f;
            }
            if (m_modalPanel != null)
            {
                m_modalPanel.SetActive(false);
            }
        }

        private void OnRetryClicked()
        {
            if (m_startupManager != null)
            {
                m_startupManager.RetryCurrentStep();
            }
        }

        private void OnScanRoomClicked()
        {
            if (m_startupManager != null)
            {
                m_startupManager.RequestRoomScan();
            }
        }
    }
}
