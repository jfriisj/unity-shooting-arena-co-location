// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Fusion;
using Meta.XR.Samples;

namespace MRMotifs.Shooting
{
    /// <summary>
    /// Manages the scoreboard UI, score entries, and player statistics display.
    /// Extracted from ShootingGameManagerMotif to handle scoreboard responsibilities.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class ScoreboardManagerMotif : MonoBehaviour
    {
        [Header("Scoreboard UI")]
        [Tooltip("UI panel showing the scoreboard.")]
        [SerializeField] private GameObject m_scoreboardPanel;

        [Tooltip("Container for player score entries.")]
        [SerializeField] private Transform m_scoreEntryContainer;

        [Tooltip("Prefab for individual score entry.")]
        [SerializeField] private GameObject m_scoreEntryPrefab;

        private readonly Dictionary<PlayerRef, GameObject> m_scoreEntries = new();
        private ShootingGameManagerMotif m_gameManager;

        private void Awake()
        {
            // Subscribe to relevant events
            GameStateEventBus.OnRoundStarted += OnRoundStarted;
            GameStateEventBus.OnRoundEnded += OnRoundEnded;
        }

        private void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            GameStateEventBus.OnRoundStarted -= OnRoundStarted;
            GameStateEventBus.OnRoundEnded -= OnRoundEnded;
        }

        private void Start()
        {
            m_gameManager = FindAnyObjectByType<ShootingGameManagerMotif>();
            
            if (m_scoreboardPanel == null || m_scoreEntryContainer == null || m_scoreEntryPrefab == null)
            {
                AutoFindScoreboardElements();
            }

            // Initialize scoreboard if already in game
            RefreshScoreboard();
        }

        private void Update()
        {
            // Update scoreboard periodically during gameplay
            if (m_gameManager != null && 
                m_gameManager.CurrentGameState == ShootingGameManagerMotif.GameState.Playing)
            {
                UpdateScoreEntries();
            }
        }

        private void AutoFindScoreboardElements()
        {
            if (m_scoreboardPanel == null)
            {
                var scoreboardObj = GameObject.Find("ScoreboardPanel");
                if (scoreboardObj != null)
                {
                    m_scoreboardPanel = scoreboardObj;
                }
            }

            if (m_scoreEntryContainer == null && m_scoreboardPanel != null)
            {
                var containerObj = m_scoreboardPanel.transform.Find("ScoreContainer");
                if (containerObj != null)
                {
                    m_scoreEntryContainer = containerObj;
                }
            }

            if (m_scoreboardPanel == null || m_scoreEntryContainer == null || m_scoreEntryPrefab == null)
            {
                Debug.LogWarning("[ScoreboardManagerMotif] Could not find scoreboard elements. Please assign them manually.");
            }
        }

        // Event handlers

        private void OnRoundStarted()
        {
            RefreshScoreboard();
            ShowScoreboard(false); // Hide during gameplay
        }

        private void OnRoundEnded(PlayerRef winner)
        {
            UpdateScoreEntries();
            ShowScoreboard(true); // Show results
        }

        // Public methods

        /// <summary>
        /// Toggle scoreboard visibility.
        /// </summary>
        public void ToggleScoreboard()
        {
            if (m_scoreboardPanel != null)
            {
                ShowScoreboard(!m_scoreboardPanel.activeSelf);
            }
        }

        /// <summary>
        /// Show or hide the scoreboard.
        /// </summary>
        public void ShowScoreboard(bool show)
        {
            if (m_scoreboardPanel != null)
            {
                m_scoreboardPanel.SetActive(show);
                if (show)
                {
                    UpdateScoreEntries();
                }
            }
        }

        /// <summary>
        /// Refresh the entire scoreboard from current player list.
        /// </summary>
        public void RefreshScoreboard()
        {
            ClearScoreEntries();
            CreateScoreEntriesForAllPlayers();
        }

        // Private methods

        private void UpdateScoreEntries()
        {
            if (m_gameManager == null) return;

            var players = GetAllPlayers();
            
            foreach (var player in players)
            {
                if (player == null || player.Object == null || !player.Object.IsValid) continue;

                var playerRef = player.OwnerPlayer;
                
                if (!m_scoreEntries.ContainsKey(playerRef))
                {
                    CreateScoreEntry(playerRef, player);
                }
                
                UpdateScoreEntry(playerRef, player);
            }
        }

        private void CreateScoreEntriesForAllPlayers()
        {
            if (m_gameManager == null) return;

            var players = GetAllPlayers();
            
            foreach (var player in players)
            {
                if (player != null && player.Object != null && player.Object.IsValid)
                {
                    CreateScoreEntry(player.OwnerPlayer, player);
                }
            }
        }

        private void CreateScoreEntry(PlayerRef playerRef, PlayerHealthMotif player)
        {
            if (m_scoreEntryContainer == null || m_scoreEntryPrefab == null) return;

            var entryObj = Instantiate(m_scoreEntryPrefab, m_scoreEntryContainer);
            m_scoreEntries[playerRef] = entryObj;
            
            UpdateScoreEntry(playerRef, player);
        }

        private void UpdateScoreEntry(PlayerRef playerRef, PlayerHealthMotif player)
        {
            if (!m_scoreEntries.TryGetValue(playerRef, out var entryObj) || entryObj == null) return;

            // Update score entry text (assumes TextMeshProUGUI components)
            var texts = entryObj.GetComponentsInChildren<TextMeshProUGUI>();
            
            if (texts.Length > 0)
            {
                var playerName = $"Player {playerRef.PlayerId}";
                var kills = player.Kills;
                var deaths = player.Deaths;
                var accuracy = player.PlayerStats != null ? player.PlayerStats.Accuracy : 0f;
                
                texts[0].text = $"{playerName}: {kills}K / {deaths}D";
                
                if (texts.Length > 1)
                {
                    texts[1].text = $"Acc: {accuracy:P0}";
                }
            }
        }

        private void ClearScoreEntries()
        {
            foreach (var entry in m_scoreEntries.Values)
            {
                if (entry != null)
                {
                    Destroy(entry);
                }
            }
            m_scoreEntries.Clear();
        }

        private List<PlayerHealthMotif> GetAllPlayers()
        {
            // Use same method as GameManager to get player list
            var players = new List<PlayerHealthMotif>();
            var foundPlayers = FindObjectsByType<PlayerHealthMotif>(FindObjectsSortMode.None);
            players.AddRange(foundPlayers);
            return players;
        }
    }
}
#endif