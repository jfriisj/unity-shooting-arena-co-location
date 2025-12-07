// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using Meta.XR.Samples;

namespace MRMotifs.SharedActivities.ShootingSample
{
    /// <summary>
    /// Manages the shooting game state, including round management,
    /// scoring, and UI updates. Coordinates between all players.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class ShootingGameManagerMotif : NetworkBehaviour
    {
        [Header("Game Settings")]
        [Tooltip("Duration of each round in seconds.")]
        [SerializeField] private float m_roundDuration = 180f;

        [Tooltip("Number of kills needed to win.")]
        [SerializeField] private int m_killsToWin = 10;

        [Tooltip("Minimum players required to start.")]
        [SerializeField] private int m_minPlayersToStart = 2;

        [Tooltip("Delay before automatically starting a new round after round end.")]
        [SerializeField] private float m_autoRestartDelay = 10f;

        [Tooltip("Whether to automatically restart rounds.")]
        [SerializeField] private bool m_autoRestart = true;

        [Header("UI References")]
        [Tooltip("UI panel showing the scoreboard.")]
        [SerializeField] private GameObject m_scoreboardPanel;

        [Tooltip("Text showing remaining time.")]
        [SerializeField] private TextMeshProUGUI m_timerText;

        [Tooltip("Text showing game status.")]
        [SerializeField] private TextMeshProUGUI m_statusText;

        [Tooltip("Container for player score entries.")]
        [SerializeField] private Transform m_scoreEntryContainer;

        [Tooltip("Prefab for individual score entry.")]
        [SerializeField] private GameObject m_scoreEntryPrefab;

        [Header("Audio")]
        [Tooltip("Sound played when round starts.")]
        [SerializeField] private AudioClip m_roundStartSound;

        [Tooltip("Sound played when round ends.")]
        [SerializeField] private AudioClip m_roundEndSound;

        /// <summary>
        /// Current game state.
        /// </summary>
        public enum GameState
        {
            WaitingForPlayers,
            Countdown,
            Playing,
            RoundEnd
        }

        /// <summary>
        /// Networked game state.
        /// </summary>
        [Networked, OnChangedRender(nameof(OnGameStateChanged))]
        public GameState CurrentGameState { get; set; }

        /// <summary>
        /// Remaining time in the current round.
        /// </summary>
        [Networked, OnChangedRender(nameof(OnTimerChanged))]
        public float RemainingTime { get; set; }

        /// <summary>
        /// Countdown before round starts.
        /// </summary>
        [Networked, OnChangedRender(nameof(OnCountdownChanged))]
        public int CountdownValue { get; set; }

        /// <summary>
        /// Player scores synchronized across clients.
        /// Each entry is packed as: PlayerId * 1000000 + Kills * 1000 + Deaths
        /// </summary>
        [Networked, Capacity(8)]
        private NetworkArray<int> PackedPlayerScores => default;

        private readonly List<PlayerHealthMotif> m_players = new();
        private readonly Dictionary<PlayerRef, GameObject> m_scoreEntries = new();
        private AudioSource m_audioSource;
        private bool m_hasSpawned;
        private float m_roundEndTime;
        private float m_restartHoldTime;
        private const float RESTART_HOLD_DURATION = 2f; // Hold both grips for 2 seconds to restart

        public override void Spawned()
        {
            base.Spawned();
            m_hasSpawned = true;

            m_audioSource = GetComponent<AudioSource>();
            if (m_audioSource == null)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (Object.HasStateAuthority)
            {
                CurrentGameState = GameState.WaitingForPlayers;
                RemainingTime = m_roundDuration;
            }

            // Find all existing players
            RefreshPlayerList();

            UpdateUI();
        }

        private void Update()
        {
            // Don't access networked properties until spawned
            if (!Object || !Object.IsValid)
                return;
                
            // Check for restart input (hold both grip buttons)
            CheckRestartInput();
            
            // Update restart countdown display during RoundEnd
            if (CurrentGameState == GameState.RoundEnd && m_autoRestart && m_statusText != null)
            {
                float timeRemaining = (m_roundEndTime + m_autoRestartDelay) - Time.time;
                if (timeRemaining > 0)
                {
                    // Update the countdown text
                    var baseText = m_statusText.text.Split('\n')[0];
                    m_statusText.text = $"{baseText}\nNew round in {Mathf.CeilToInt(timeRemaining)}s...";
                }
            }
        }

        private void CheckRestartInput()
        {
            // Hold both grip buttons to restart (works in RoundEnd state)
            bool leftGrip = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);
            bool rightGrip = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);

            if (leftGrip && rightGrip)
            {
                m_restartHoldTime += Time.deltaTime;

                if (m_restartHoldTime >= RESTART_HOLD_DURATION)
                {
                    m_restartHoldTime = 0f;
                    
                    if (CurrentGameState == GameState.RoundEnd)
                    {
                        RestartRound();
                    }
                    else if (CurrentGameState == GameState.Playing)
                    {
                        // During gameplay, reset the round
                        ResetCurrentRound();
                    }
                }
            }
            else
            {
                m_restartHoldTime = 0f;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority)
            {
                return;
            }

            switch (CurrentGameState)
            {
                case GameState.WaitingForPlayers:
                    CheckForMinPlayers();
                    break;

                case GameState.Countdown:
                    UpdateCountdown();
                    break;

                case GameState.Playing:
                    UpdateRoundTimer();
                    CheckForWinner();
                    break;

                case GameState.RoundEnd:
                    // Auto-restart after delay if enabled
                    if (m_autoRestart && Time.time >= m_roundEndTime + m_autoRestartDelay)
                    {
                        RestartRound();
                    }
                    break;
            }

            UpdatePlayerScores();
        }

        private void RefreshPlayerList()
        {
            m_players.Clear();
            var players = FindObjectsByType<PlayerHealthMotif>(FindObjectsSortMode.None);
            m_players.AddRange(players);
        }

        private void CheckForMinPlayers()
        {
            RefreshPlayerList();

            // Count active players from Fusion's network runner
            int activePlayerCount = GetActivePlayerCount();
            if (activePlayerCount >= m_minPlayersToStart)
            {
                StartCountdown();
            }
        }

        private int GetActivePlayerCount()
        {
            if (Runner == null) return 0;
            
            int count = 0;
            foreach (var _ in Runner.ActivePlayers)
            {
                count++;
            }
            return count;
        }

        private void StartCountdown()
        {
            CurrentGameState = GameState.Countdown;
            CountdownValue = 5;
        }

        private void UpdateCountdown()
        {
            // Countdown is handled in FixedUpdateNetwork ticks
            // Decrease every ~60 ticks (1 second at 60 tick rate)
            if (Runner.Tick % 60 == 0 && CountdownValue > 0)
            {
                CountdownValue--;

                if (CountdownValue <= 0)
                {
                    StartRound();
                }
            }
        }

        private void StartRound()
        {
            CurrentGameState = GameState.Playing;
            RemainingTime = m_roundDuration;

            // Reset all player stats
            foreach (var player in m_players)
            {
                player.ResetStats();
            }

            PlaySoundRpc(true);
        }

        private void UpdateRoundTimer()
        {
            RemainingTime -= Runner.DeltaTime;

            if (RemainingTime <= 0)
            {
                EndRound(PlayerRef.None);
            }
        }

        private void CheckForWinner()
        {
            foreach (var player in m_players)
            {
                // Skip players that aren't fully spawned yet
                if (player == null || player.Object == null || !player.Object.IsValid)
                {
                    continue;
                }
                
                if (player.Kills >= m_killsToWin)
                {
                    EndRound(player.OwnerPlayer);
                    return;
                }
            }
        }

        private void EndRound(PlayerRef winner)
        {
            CurrentGameState = GameState.RoundEnd;
            m_roundEndTime = Time.time;
            AnnounceWinnerRpc(winner, m_autoRestart ? m_autoRestartDelay : -1f);
            PlaySoundRpc(false);
        }

        private void UpdatePlayerScores()
        {
            for (var i = 0; i < m_players.Count && i < PackedPlayerScores.Length; i++)
            {
                var player = m_players[i];
                // Skip players that aren't fully spawned yet
                if (player == null || player.Object == null || !player.Object.IsValid)
                {
                    continue;
                }
                // Pack: PlayerId * 1000000 + Kills * 1000 + Deaths
                var packed = player.OwnerPlayer.PlayerId * 1000000 + player.Kills * 1000 + player.Deaths;
                _ = PackedPlayerScores.Set(i, packed);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void AnnounceWinnerRpc(PlayerRef winner, float restartDelay)
        {
            if (m_statusText != null)
            {
                var winnerText = winner == PlayerRef.None 
                    ? "Time's Up! Round Over" 
                    : $"Player {winner.PlayerId} Wins!";
                
                if (restartDelay > 0)
                {
                    winnerText += $"\nNew round in {restartDelay:F0}s...";
                }
                
                m_statusText.text = winnerText;
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void PlaySoundRpc(bool isStart)
        {
            if (m_audioSource != null)
            {
                var clip = isStart ? m_roundStartSound : m_roundEndSound;
                if (clip != null)
                {
                    m_audioSource.PlayOneShot(clip);
                }
            }
        }

        /// <summary>
        /// Call to restart the round (host only).
        /// </summary>
        public void RestartRound()
        {
            if (!Object.HasStateAuthority)
            {
                // If not host, request restart via RPC
                RequestRestartRpc();
                return;
            }

            Debug.Log("[ShootingGameManager] Restarting round...");
            
            // Reset all player stats
            RefreshPlayerList();
            foreach (var player in m_players)
            {
                if (player != null && player.Object != null && player.Object.IsValid)
                {
                    player.ResetStats();
                }
            }

            // Announce restart to all clients
            OnRoundRestartRpc();

            // Start countdown for new round
            StartCountdown();
        }

        /// <summary>
        /// Force reset the current round without ending it (emergency reset).
        /// </summary>
        public void ResetCurrentRound()
        {
            if (!Object.HasStateAuthority)
            {
                RequestResetRpc();
                return;
            }

            Debug.Log("[ShootingGameManager] Resetting current round...");
            
            // Reset timer
            RemainingTime = m_roundDuration;

            // Reset all player stats but keep playing
            RefreshPlayerList();
            foreach (var player in m_players)
            {
                if (player != null && player.Object != null && player.Object.IsValid)
                {
                    player.ResetStats();
                }
            }

            // Announce reset
            OnRoundResetRpc();

            // If not already playing, start countdown
            if (CurrentGameState != GameState.Playing)
            {
                StartCountdown();
            }
        }

        /// <summary>
        /// RPC for clients to request a round restart from the host.
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RequestRestartRpc()
        {
            Debug.Log("[ShootingGameManager] Restart requested by client");
            RestartRound();
        }

        /// <summary>
        /// RPC for clients to request a round reset from the host.
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RequestResetRpc()
        {
            Debug.Log("[ShootingGameManager] Reset requested by client");
            ResetCurrentRound();
        }

        /// <summary>
        /// Notify all clients that the round is restarting.
        /// </summary>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void OnRoundRestartRpc()
        {
            Debug.Log("[ShootingGameManager] Round restarting...");
            if (m_statusText != null)
            {
                m_statusText.text = "New Round Starting...";
            }
        }

        /// <summary>
        /// Notify all clients that the round has been reset.
        /// </summary>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void OnRoundResetRpc()
        {
            Debug.Log("[ShootingGameManager] Round reset!");
            if (m_statusText != null)
            {
                m_statusText.text = "Round Reset!";
            }
        }

        // UI Update callbacks
        private void OnGameStateChanged()
        {
            UpdateUI();
        }

        private void OnTimerChanged()
        {
            if (m_timerText != null)
            {
                var minutes = Mathf.FloorToInt(RemainingTime / 60);
                var seconds = Mathf.FloorToInt(RemainingTime % 60);
                m_timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }

        private void OnCountdownChanged()
        {
            if (m_statusText != null && CurrentGameState == GameState.Countdown)
            {
                m_statusText.text = CountdownValue > 0 ? $"Starting in {CountdownValue}..." : "GO!";
            }
        }

        private void UpdateUI()
        {
            if (m_statusText == null)
            {
                return;
            }

            switch (CurrentGameState)
            {
                case GameState.WaitingForPlayers:
                    m_statusText.text = $"Waiting for players ({GetActivePlayerCount()}/{m_minPlayersToStart})";
                    break;

                case GameState.Countdown:
                    m_statusText.text = $"Starting in {CountdownValue}...";
                    break;

                case GameState.Playing:
                    m_statusText.text = "FIGHT!";
                    break;

                case GameState.RoundEnd:
                    // Set by AnnounceWinnerRpc
                    break;
            }
        }

        /// <summary>
        /// Toggle scoreboard visibility.
        /// </summary>
        public void ToggleScoreboard()
        {
            if (m_scoreboardPanel != null)
            {
                m_scoreboardPanel.SetActive(!m_scoreboardPanel.activeSelf);
            }
        }

        /// <summary>
        /// Register a new player with the game manager.
        /// </summary>
        public void RegisterPlayer(PlayerHealthMotif player)
        {
            if (!m_players.Contains(player))
            {
                m_players.Add(player);
            }
        }

        /// <summary>
        /// Unregister a player from the game manager.
        /// </summary>
        public void UnregisterPlayer(PlayerHealthMotif player)
        {
            _ = m_players.Remove(player);
        }
    }
}
#endif
