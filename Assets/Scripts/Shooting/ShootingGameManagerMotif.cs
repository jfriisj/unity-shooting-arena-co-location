// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using Meta.XR.Samples;

namespace MRMotifs.Shooting
{
    /// <summary>
    /// Manages the shooting game state, including round management,
    /// scoring, and UI updates. Coordinates between all players.
    /// Converted from Photon Fusion to Unity NGO.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class ShootingGameManagerMotif : NetworkBehaviour
    {
        private ShootingGameConfigMotif m_config;

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
        public NetworkVariable<GameState> CurrentGameState = new NetworkVariable<GameState>(
            GameState.WaitingForPlayers,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        /// <summary>
        /// Remaining time in the current round.
        /// </summary>
        public NetworkVariable<float> RemainingTime = new NetworkVariable<float>(
            180f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        /// <summary>
        /// Countdown before round starts.
        /// </summary>
        public NetworkVariable<int> CountdownValue = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        /// <summary>
        /// Player scores synchronized across clients.
        /// Each entry is packed as: PlayerId * 1000000 + Kills * 1000 + Deaths
        /// </summary>
        public NetworkList<int> PackedPlayerScores;

        private readonly List<PlayerHealthMotif> m_players = new();
        private float m_roundEndTime;
        private float m_lastCountdownTick;

        private void Awake()
        {
            // Prevent device from sleeping
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            m_config = GetComponent<ShootingGameConfigMotif>();
            if (m_config == null)
            {
                Debug.LogError("ShootingGameConfigMotif not found on ShootingGameManagerMotif!");
            }

            // Initialize NetworkList
            PackedPlayerScores = new NetworkList<int>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Subscribe to value changes for all clients
            CurrentGameState.OnValueChanged += OnGameStateChanged;
            RemainingTime.OnValueChanged += OnTimerChanged;
            CountdownValue.OnValueChanged += OnCountdownChanged;

            if (IsServer)
            {
                CurrentGameState.Value = GameState.WaitingForPlayers;
                RemainingTime.Value = m_config != null ? m_config.roundDuration : 180f;
                
                // Subscribe to restart requests
                GameStateEventBus.OnRestartRequested += OnRestartRequested;
            }

            // Find all existing players
            RefreshPlayerList();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            CurrentGameState.OnValueChanged -= OnGameStateChanged;
            RemainingTime.OnValueChanged -= OnTimerChanged;
            CountdownValue.OnValueChanged -= OnCountdownChanged;
            GameStateEventBus.OnRestartRequested -= OnRestartRequested;
        }

        private void OnGameStateChanged(GameState previousValue, GameState newValue)
        {
            GameStateEventBus.FireGameStateChanged(previousValue, newValue);
        }

        private void OnTimerChanged(float previousValue, float newValue)
        {
            GameStateEventBus.FireTimerUpdated(newValue);
        }

        private void OnCountdownChanged(int previousValue, int newValue)
        {
            GameStateEventBus.FireCountdownTick(newValue);
        }

        private void OnRestartRequested()
        {
            if (!IsServer) return;

            switch (CurrentGameState.Value)
            {
                case GameState.RoundEnd:
                    RestartRound();
                    break;
                case GameState.Playing:
                    ResetCurrentRound();
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (!IsServer)
            {
                return;
            }

            switch (CurrentGameState.Value)
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
                    if (m_config != null && m_config.autoRestart && Time.time >= m_roundEndTime + m_config.autoRestartDelay)
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

            // Count active players from NetworkManager
            int activePlayerCount = GetActivePlayerCount();
            if (activePlayerCount >= (m_config != null ? m_config.minPlayersToStart : 2))
            {
                StartCountdown();
            }
        }

        private int GetActivePlayerCount()
        {
            if (NetworkManager.Singleton == null) return 0;
            return NetworkManager.Singleton.ConnectedClientsIds.Count;
        }

        private void StartCountdown()
        {
            var oldState = CurrentGameState.Value;
            CurrentGameState.Value = GameState.Countdown;
            CountdownValue.Value = m_config != null ? m_config.countdownDuration : 5;
            m_lastCountdownTick = Time.time;
            
            // Fire event for state change
            GameStateEventBus.FireGameStateChanged(oldState, CurrentGameState.Value);
        }

        private void UpdateCountdown()
        {
            // Decrease every 1 second
            if (Time.time - m_lastCountdownTick >= 1f && CountdownValue.Value > 0)
            {
                m_lastCountdownTick = Time.time;
                CountdownValue.Value--;
                
                // Fire countdown tick event
                GameStateEventBus.FireCountdownTick(CountdownValue.Value);

                if (CountdownValue.Value <= 0)
                {
                    StartRound();
                }
            }
        }

        private void StartRound()
        {
            var oldState = CurrentGameState.Value;
            CurrentGameState.Value = GameState.Playing;
            RemainingTime.Value = m_config != null ? m_config.roundDuration : 180f;

            // Reset all player stats
            foreach (var player in m_players)
            {
                if (player != null && player.NetworkObject != null && player.NetworkObject.IsSpawned)
                {
                    player.ResetStats();
                }
            }

            // Fire events
            GameStateEventBus.FireGameStateChanged(oldState, CurrentGameState.Value);
            GameStateEventBus.FireRoundStarted();
        }

        private void UpdateRoundTimer()
        {
            RemainingTime.Value -= Time.fixedDeltaTime;
            
            // Fire timer update event
            GameStateEventBus.FireTimerUpdated(RemainingTime.Value);

            if (RemainingTime.Value <= 0)
            {
                EndRound(ulong.MaxValue); // No winner
            }
        }

        private void CheckForWinner()
        {
            foreach (var player in m_players)
            {
                // Skip players that aren't fully spawned yet
                if (player == null || player.NetworkObject == null || !player.NetworkObject.IsSpawned)
                {
                    continue;
                }
                
                if (player.Kills >= (m_config != null ? m_config.killsToWin : 10))
                {
                    EndRound(player.OwnerPlayer.Value);
                    return;
                }
            }
        }

        private void EndRound(ulong winner)
        {
            var oldState = CurrentGameState.Value;
            CurrentGameState.Value = GameState.RoundEnd;
            m_roundEndTime = Time.time;
            
            // Fire events
            GameStateEventBus.FireGameStateChanged(oldState, CurrentGameState.Value);
            GameStateEventBus.FireRoundEndedNGO(winner);
            
            AnnounceWinnerClientRpc(winner, (m_config != null && m_config.autoRestart) ? m_config.autoRestartDelay : -1f);
        }

        private void UpdatePlayerScores()
        {
            // Ensure list is sized correctly
            while (PackedPlayerScores.Count < m_players.Count)
            {
                PackedPlayerScores.Add(0);
            }

            for (var i = 0; i < m_players.Count && i < PackedPlayerScores.Count; i++)
            {
                var player = m_players[i];
                // Skip players that aren't fully spawned yet
                if (player == null || player.NetworkObject == null || !player.NetworkObject.IsSpawned)
                {
                    continue;
                }
                // Pack: PlayerId * 1000000 + Kills * 1000 + Deaths
                var packed = (int)player.OwnerPlayer.Value * 1000000 + player.Kills * 1000 + player.Deaths;
                PackedPlayerScores[i] = packed;
            }
        }

        [ClientRpc]
        private void AnnounceWinnerClientRpc(ulong winner, float restartDelay)
        {
            var winnerText = winner == ulong.MaxValue 
                ? "Time's Up! Round Over" 
                : $"Player {winner} Wins!";
            
            if (restartDelay > 0)
            {
                winnerText += $"\nNew round in {restartDelay:F0}s...";
            }
            
            // Fire winner announcement event
            GameStateEventBus.FireWinnerAnnounced(winnerText);
        }

        /// <summary>
        /// Call to restart the round (server only).
        /// </summary>
        public void RestartRound()
        {
            if (!IsServer)
            {
                // If not server, request restart via RPC
                RequestRestartServerRpc();
                return;
            }

            Debug.Log("[ShootingGameManager] Restarting round...");
            
            // Reset all player stats
            RefreshPlayerList();
            foreach (var player in m_players)
            {
                if (player != null && player.NetworkObject != null && player.NetworkObject.IsSpawned)
                {
                    player.ResetStats();
                }
            }

            // Announce restart to all clients
            OnRoundRestartClientRpc();

            // Start countdown for new round
            StartCountdown();
        }

        /// <summary>
        /// Force reset the current round without ending it (emergency reset).
        /// </summary>
        public void ResetCurrentRound()
        {
            if (!IsServer)
            {
                RequestResetServerRpc();
                return;
            }

            Debug.Log("[ShootingGameManager] Resetting current round...");
            
            // Reset timer
            RemainingTime.Value = m_config != null ? m_config.roundDuration : 180f;

            // Reset all player stats but keep playing
            RefreshPlayerList();
            foreach (var player in m_players)
            {
                if (player != null && player.NetworkObject != null && player.NetworkObject.IsSpawned)
                {
                    player.ResetStats();
                }
            }

            // Announce reset
            OnRoundResetClientRpc();

            // If not already playing, start countdown
            if (CurrentGameState.Value != GameState.Playing)
            {
                StartCountdown();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestRestartServerRpc()
        {
            Debug.Log("[ShootingGameManager] Restart requested by client");
            RestartRound();
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestResetServerRpc()
        {
            Debug.Log("[ShootingGameManager] Reset requested by client");
            ResetCurrentRound();
        }

        [ClientRpc]
        private void OnRoundRestartClientRpc()
        {
            Debug.Log("[ShootingGameManager] Round restarting...");
        }

        [ClientRpc]
        private void OnRoundResetClientRpc()
        {
            Debug.Log("[ShootingGameManager] Round reset!");
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
