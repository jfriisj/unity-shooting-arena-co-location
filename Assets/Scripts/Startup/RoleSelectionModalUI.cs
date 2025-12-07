// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using Meta.XR.Samples;

namespace MRMotifs.SharedActivities.Startup
{
    /// <summary>
    /// Represents a discovered host session that clients can join.
    /// </summary>
    [Serializable]
    public class DiscoveredSession
    {
        public string SessionName;
        public string HostName;
        public int PlayerCount;
        public int MaxPlayers;
        public Guid ColocationGroupId;
        public DateTime DiscoveredAt;
    }

    /// <summary>
    /// UI component for role selection (Host/Client) at game startup.
    /// Shows after platform init, before any network connection.
    /// 
    /// Host: Creates a new session, scans room, shares anchor
    /// Client: Shows list of available sessions to join
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class RoleSelectionModalUI : MonoBehaviour
    {
        [Header("Main Panel")]
        [SerializeField] private GameObject m_modalPanel;
        [SerializeField] private CanvasGroup m_canvasGroup;

        [Header("Role Selection Panel")]
        [SerializeField] private GameObject m_roleSelectionPanel;
        [SerializeField] private TMP_Text m_titleText;
        [SerializeField] private Button m_hostButton;
        [SerializeField] private Button m_clientButton;
        [SerializeField] private TMP_Text m_hostButtonText;
        [SerializeField] private TMP_Text m_clientButtonText;

        [Header("Session List Panel (for Clients)")]
        [SerializeField] private GameObject m_sessionListPanel;
        [SerializeField] private TMP_Text m_sessionListTitle;
        [SerializeField] private Transform m_sessionListContainer;
        [SerializeField] private GameObject m_sessionEntryPrefab;
        [SerializeField] private Button m_backButton;
        [SerializeField] private Button m_refreshButton;
        [SerializeField] private TMP_Text m_noSessionsText;
        [SerializeField] private GameObject m_searchingIndicator;

        [Header("Settings")]
        [SerializeField] private float m_sessionDiscoveryTimeout = 10f;
        [SerializeField] private float m_refreshInterval = 3f;

        // Events
        public event Action OnHostSelected;
        public event Action<string> OnSessionSelected; // Session name to join
        public event Action OnBackToRoleSelection;

        // State
        private List<DiscoveredSession> m_discoveredSessions = new List<DiscoveredSession>();
        private List<GameObject> m_sessionEntryInstances = new List<GameObject>();
        private bool m_isSearching = false;
        private Coroutine m_discoveryCoroutine;
        private bool m_isVisible = true;

        private void Awake()
        {
            // Setup button listeners
            if (m_hostButton != null)
            {
                m_hostButton.onClick.AddListener(OnHostButtonClicked);
            }

            if (m_clientButton != null)
            {
                m_clientButton.onClick.AddListener(OnClientButtonClicked);
            }

            if (m_backButton != null)
            {
                m_backButton.onClick.AddListener(OnBackButtonClicked);
            }

            if (m_refreshButton != null)
            {
                m_refreshButton.onClick.AddListener(OnRefreshButtonClicked);
            }
        }

        private void Start()
        {
            // Show role selection panel by default
            ShowRoleSelection();
        }

        private void OnDestroy()
        {
            if (m_discoveryCoroutine != null)
            {
                StopCoroutine(m_discoveryCoroutine);
            }
        }

        #region Public Methods

        /// <summary>
        /// Show the modal and role selection panel.
        /// </summary>
        public void Show()
        {
            if (m_modalPanel != null)
            {
                m_modalPanel.SetActive(true);
            }
            m_isVisible = true;
            ShowRoleSelection();
        }

        /// <summary>
        /// Hide the modal.
        /// </summary>
        public void Hide()
        {
            m_isVisible = false;
            StopSessionDiscovery();
            
            if (m_canvasGroup != null)
            {
                StartCoroutine(FadeOut());
            }
            else if (m_modalPanel != null)
            {
                m_modalPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Add a discovered session to the list.
        /// Called by SessionDiscoveryManager when a session is found.
        /// </summary>
        public void AddDiscoveredSession(DiscoveredSession session)
        {
            // Check if session already exists
            var existing = m_discoveredSessions.Find(s => s.SessionName == session.SessionName);
            if (existing != null)
            {
                // Update existing session info
                existing.PlayerCount = session.PlayerCount;
                existing.DiscoveredAt = DateTime.Now;
            }
            else
            {
                m_discoveredSessions.Add(session);
            }

            RefreshSessionListUI();
        }

        /// <summary>
        /// Clear all discovered sessions.
        /// </summary>
        public void ClearDiscoveredSessions()
        {
            m_discoveredSessions.Clear();
            RefreshSessionListUI();
        }

        /// <summary>
        /// Update the list with sessions from Fusion's session list.
        /// </summary>
        public void UpdateSessionList(List<SessionInfo> sessions)
        {
            m_discoveredSessions.Clear();

            foreach (var session in sessions)
            {
                var discovered = new DiscoveredSession
                {
                    SessionName = session.Name,
                    HostName = session.Name, // Could extract host name from properties
                    PlayerCount = session.PlayerCount,
                    MaxPlayers = session.MaxPlayers,
                    DiscoveredAt = DateTime.Now
                };
                m_discoveredSessions.Add(discovered);
            }

            RefreshSessionListUI();
        }

        #endregion

        #region Panel Navigation

        private void ShowRoleSelection()
        {
            if (m_roleSelectionPanel != null)
            {
                m_roleSelectionPanel.SetActive(true);
            }

            if (m_sessionListPanel != null)
            {
                m_sessionListPanel.SetActive(false);
            }

            StopSessionDiscovery();
        }

        private void ShowSessionList()
        {
            if (m_roleSelectionPanel != null)
            {
                m_roleSelectionPanel.SetActive(false);
            }

            if (m_sessionListPanel != null)
            {
                m_sessionListPanel.SetActive(true);
            }

            // Start searching for sessions
            StartSessionDiscovery();
        }

        #endregion

        #region Button Handlers

        private void OnHostButtonClicked()
        {
            Debug.Log("[RoleSelection] HOST selected");
            OnHostSelected?.Invoke();
        }

        private void OnClientButtonClicked()
        {
            Debug.Log("[RoleSelection] CLIENT selected - showing session list");
            ShowSessionList();
        }

        private void OnBackButtonClicked()
        {
            Debug.Log("[RoleSelection] Back to role selection");
            ShowRoleSelection();
            OnBackToRoleSelection?.Invoke();
        }

        private void OnRefreshButtonClicked()
        {
            Debug.Log("[RoleSelection] Refreshing session list");
            ClearDiscoveredSessions();
            StartSessionDiscovery();
        }

        private void OnSessionEntryClicked(string sessionName)
        {
            Debug.Log($"[RoleSelection] Selected session: {sessionName}");
            StopSessionDiscovery();
            OnSessionSelected?.Invoke(sessionName);
        }

        #endregion

        #region Session Discovery

        private void StartSessionDiscovery()
        {
            if (m_isSearching) return;

            m_isSearching = true;
            
            if (m_searchingIndicator != null)
            {
                m_searchingIndicator.SetActive(true);
            }

            if (m_noSessionsText != null)
            {
                m_noSessionsText.gameObject.SetActive(false);
            }

            m_discoveryCoroutine = StartCoroutine(DiscoverSessionsCoroutine());
        }

        private void StopSessionDiscovery()
        {
            m_isSearching = false;

            if (m_discoveryCoroutine != null)
            {
                StopCoroutine(m_discoveryCoroutine);
                m_discoveryCoroutine = null;
            }

            if (m_searchingIndicator != null)
            {
                m_searchingIndicator.SetActive(false);
            }
        }

        private IEnumerator DiscoverSessionsCoroutine()
        {
            float elapsed = 0f;

            while (m_isSearching && elapsed < m_sessionDiscoveryTimeout)
            {
                // Request session list from SessionDiscoveryManager
                var discoveryManager = FindAnyObjectByType<SessionDiscoveryManager>();
                if (discoveryManager != null)
                {
                    discoveryManager.RefreshSessionList();
                }

                yield return new WaitForSeconds(m_refreshInterval);
                elapsed += m_refreshInterval;
            }

            m_isSearching = false;
            
            if (m_searchingIndicator != null)
            {
                m_searchingIndicator.SetActive(false);
            }

            // Show "no sessions" message if list is empty
            if (m_discoveredSessions.Count == 0 && m_noSessionsText != null)
            {
                m_noSessionsText.gameObject.SetActive(true);
                m_noSessionsText.text = "No games found.\nAsk a friend to host a game.";
            }
        }

        #endregion

        #region UI Updates

        private void RefreshSessionListUI()
        {
            // Clear existing entries
            foreach (var entry in m_sessionEntryInstances)
            {
                Destroy(entry);
            }
            m_sessionEntryInstances.Clear();

            // Hide "no sessions" text if we have sessions
            if (m_noSessionsText != null)
            {
                m_noSessionsText.gameObject.SetActive(m_discoveredSessions.Count == 0 && !m_isSearching);
            }

            // Create entry for each session
            if (m_sessionListContainer != null && m_sessionEntryPrefab != null)
            {
                foreach (var session in m_discoveredSessions)
                {
                    var entry = Instantiate(m_sessionEntryPrefab, m_sessionListContainer);
                    m_sessionEntryInstances.Add(entry);

                    // Setup entry UI
                    var nameText = entry.GetComponentInChildren<TMP_Text>();
                    if (nameText != null)
                    {
                        nameText.text = $"{session.SessionName}\n<size=80%>{session.PlayerCount}/{session.MaxPlayers} players</size>";
                    }

                    var button = entry.GetComponent<Button>();
                    if (button != null)
                    {
                        string sessionName = session.SessionName; // Capture for closure
                        button.onClick.AddListener(() => OnSessionEntryClicked(sessionName));
                    }
                }
            }
        }

        private IEnumerator FadeOut()
        {
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (m_canvasGroup != null)
                {
                    m_canvasGroup.alpha = 1f - (elapsed / duration);
                }
                yield return null;
            }

            if (m_modalPanel != null)
            {
                m_modalPanel.SetActive(false);
            }

            if (m_canvasGroup != null)
            {
                m_canvasGroup.alpha = 1f;
            }
        }

        #endregion
    }
}
#endif
