// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using UnityEngine;
using Fusion;

namespace MRMotifs.SharedActivities.ShootingSample
{
    /// <summary>
    /// Interface for any object that can take damage in the shooting game.
    /// Implements the damage system pattern from Discover/DroneRage.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Callback invoked when damage is applied.
        /// </summary>
        /// <param name="damageable">The object that was damaged.</param>
        /// <param name="amount">Amount of damage/healing applied.</param>
        /// <param name="targetDied">Whether the target died from this damage.</param>
        public delegate void DamageCallback(IDamageable damageable, float amount, bool targetDied);

        /// <summary>
        /// Heals this object by the specified amount.
        /// </summary>
        /// <param name="healing">Amount to heal.</param>
        /// <param name="callback">Optional callback when healing is applied.</param>
        void Heal(float healing, DamageCallback callback = null);

        /// <summary>
        /// Applies damage to this object.
        /// </summary>
        /// <param name="damage">Amount of damage to apply.</param>
        /// <param name="position">World position where damage was applied.</param>
        /// <param name="normal">Surface normal at damage point.</param>
        /// <param name="callback">Optional callback when damage is applied.</param>
        void TakeDamage(float damage, Vector3 position, Vector3 normal, DamageCallback callback = null);
    }
}
#endif
