// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.XR.Samples;

namespace MRMotifs.Shooting
{
    /// <summary>
    /// Static event bus for game state communication between components.
    /// Eliminates tight coupling and race conditions by using events instead of direct references.
    /// Converted from Photon Fusion to Unity NGO.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public static class GameStateEventBus
    {
        /// <summary>
        /// Fired when the game state changes (Waiting, Countdown, Playing, RoundEnd).
        /// </summary>
        public static event Action<ShootingGameManagerMotif.GameState, ShootingGameManagerMotif.GameState> OnGameStateChanged;

        /// <summary>
        /// Fired when a round starts (countdown completes).
        /// </summary>
        public static event Action OnRoundStarted;

        /// <summary>
        /// Fired when a round ends (winner found or time up).
        /// Parameter is winner's client ID (ulong.MaxValue if no winner/timeout).
        /// </summary>
        public static event Action<ulong> OnRoundEnded;

        /// <summary>
        /// Fired during countdown with the current countdown value.
        /// </summary>
        public static event Action<int> OnCountdownTick;

        /// <summary>
        /// Fired when the round timer updates (every frame during play).
        /// </summary>
        public static event Action<float> OnTimerUpdated;

        /// <summary>
        /// Fired when restart input is detected (dual-grip hold).
        /// </summary>
        public static event Action OnRestartRequested;

        /// <summary>
        /// Fired when a winner is announced with display message.
        /// </summary>
        public static event Action<string> OnWinnerAnnounced;

        // Fire methods (called by ShootingGameManagerMotif)

        public static void FireGameStateChanged(ShootingGameManagerMotif.GameState oldState, ShootingGameManagerMotif.GameState newState)
        {
            OnGameStateChanged?.Invoke(oldState, newState);
        }

        public static void FireRoundStarted()
        {
            OnRoundStarted?.Invoke();
        }

        /// <summary>
        /// Fire round ended event with NGO client ID.
        /// </summary>
        public static void FireRoundEndedNGO(ulong winnerClientId)
        {
            OnRoundEnded?.Invoke(winnerClientId);
        }

        public static void FireCountdownTick(int countdownValue)
        {
            OnCountdownTick?.Invoke(countdownValue);
        }

        public static void FireTimerUpdated(float remainingTime)
        {
            OnTimerUpdated?.Invoke(remainingTime);
        }

        public static void FireRestartRequested()
        {
            OnRestartRequested?.Invoke();
        }

        public static void FireWinnerAnnounced(string winnerMessage)
        {
            OnWinnerAnnounced?.Invoke(winnerMessage);
        }

        /// <summary>
        /// Clear all event subscriptions (useful for scene cleanup).
        /// </summary>
        public static void ClearAllEvents()
        {
            OnGameStateChanged = null;
            OnRoundStarted = null;
            OnRoundEnded = null;
            OnCountdownTick = null;
            OnTimerUpdated = null;
            OnRestartRequested = null;
            OnWinnerAnnounced = null;
        }
    }
}