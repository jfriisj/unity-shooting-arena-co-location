// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using Meta.XR.Samples;

namespace MRMotifs.Shooting
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

        // UI and scoreboard now handled by separate components:
        // - GameStateUIHandler manages status/timer text
        // - ScoreboardManagerMotif manages scoreboard panel
        // - GameInputHandler manages restart input

        // Audio now handled by ShootingAudioMotif via event bus

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
        private float m_roundEndTime;
        // Input handling now in GameInputHandler
        // Scoreboard entries now in ScoreboardManagerMotif
        // Audio source now in ShootingAudioMotif

        private void Awake()
        {
            // Prevent device from sleeping to avoid "crashes" when user is inactive (e.g. waiting for round reset)
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        public override void Spawned()
        {
            base.Spawned();

            if (Object.HasStateAuthority)
            {
                CurrentGameState = GameState.WaitingForPlayers;
                RemainingTime = m_roundDuration;
                
                // Subscribe to restart requests from GameInputHandler
                GameStateEventBus.OnRestartRequested += OnRestartRequested;
            }

            // Find all existing players
            RefreshPlayerList();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            GameStateEventBus.OnRestartRequested -= OnRestartRequested;
        }

        private void OnRestartRequested()
        {
            if (!Object.HasStateAuthority) return;

            switch (CurrentGameState)
            {
                case GameState.RoundEnd:
                    RestartRound();
                    break;
                case GameState.Playing:
                    ResetCurrentRound();
                    break;
            }
        }

        private void Update()
        {
            // Input handling now done by GameInputHandler
            // Auto-restart countdown display now handled by GameStateUIHandler
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
            var oldState = CurrentGameState;
            CurrentGameState = GameState.Countdown;
            CountdownValue = 5;
            
            // Fire event for state change
            GameStateEventBus.FireGameStateChanged(oldState, CurrentGameState);
        }

        private void UpdateCountdown()
        {
            // Countdown is handled in FixedUpdateNetwork ticks
            // Decrease every ~60 ticks (1 second at 60 tick rate)
            if (Runner.Tick % 60 == 0 && CountdownValue > 0)
            {
                CountdownValue--;
                
                // Fire countdown tick event
                GameStateEventBus.FireCountdownTick(CountdownValue);

                if (CountdownValue <= 0)
                {
                    StartRound();
                }
            }
        }

        private void StartRound()
        {
            var oldState = CurrentGameState;
            CurrentGameState = GameState.Playing;
            RemainingTime = m_roundDuration;

            // Reset all player stats
            foreach (var player in m_players)
            {
                if (player != null && player.Object != null && player.Object.IsValid)
                {
                    player.ResetStats();
                }
            }

            // Fire events
            GameStateEventBus.FireGameStateChanged(oldState, CurrentGameState);
            GameStateEventBus.FireRoundStarted();
        }

        private void UpdateRoundTimer()
        {
            RemainingTime -= Runner.DeltaTime;
            
            // Fire timer update event
            GameStateEventBus.FireTimerUpdated(RemainingTime);

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
            var oldState = CurrentGameState;
            CurrentGameState = GameState.RoundEnd;
            m_roundEndTime = Time.time;
            
            // Fire events
            GameStateEventBus.FireGameStateChanged(oldState, CurrentGameState);
            GameStateEventBus.FireRoundEnded(winner);
            
            AnnounceWinnerRpc(winner, m_autoRestart ? m_autoRestartDelay : -1f);
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
            var winnerText = winner == PlayerRef.None 
                ? "Time's Up! Round Over" 
                : $"Player {winner.PlayerId} Wins!";
            
            if (restartDelay > 0)
            {
                winnerText += $"\nNew round in {restartDelay:F0}s...";
            }
            
            // Fire winner announcement event instead of direct UI manipulation
            GameStateEventBus.FireWinnerAnnounced(winnerText);
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
            // UI updates now handled by GameStateUIHandler via events
        }

        /// <summary>
        /// Notify all clients that the round has been reset.
        /// </summary>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void OnRoundResetRpc()
        {
            Debug.Log("[ShootingGameManager] Round reset!");
            // UI updates now handled by GameStateUIHandler via events
        }



        // Scoreboard management now handled by ScoreboardManagerMotif

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
