// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Meta.XR.Samples;

namespace MRMotifs.SharedActivities.ShootingSample
{
    /// <summary>
    /// Displays player HUD showing health, ammo, and score.
    /// Attaches to the camera rig and follows the player's view.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class ShootingHUDMotif : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Slider showing current health.")]
        [SerializeField] private Slider m_healthSlider;

        [Tooltip("Text showing health value.")]
        [SerializeField] private TextMeshProUGUI m_healthText;

        [Tooltip("Text showing kills count.")]
        [SerializeField] private TextMeshProUGUI m_killsText;

        [Tooltip("Text showing deaths count.")]
        [SerializeField] private TextMeshProUGUI m_deathsText;

        [Tooltip("Panel shown when player is dead.")]
        [SerializeField] private GameObject m_deathPanel;

        [Tooltip("Text showing respawn countdown.")]
        [SerializeField] private TextMeshProUGUI m_respawnText;

        [Tooltip("Crosshair image.")]
        [SerializeField] private Image m_crosshair;

        [Tooltip("Hit marker image (shown briefly on hit).")]
        [SerializeField] private Image m_hitMarker;

        [Tooltip("Damage indicator image.")]
        [SerializeField] private Image m_damageIndicator;

        [Header("Settings")]
        [Tooltip("Duration to show hit marker.")]
        [SerializeField] private float m_hitMarkerDuration = 0.1f;

        [Tooltip("Duration to show damage indicator.")]
        [SerializeField] private float m_damageIndicatorDuration = 0.5f;

        [Tooltip("Distance from camera for HUD.")]
        [SerializeField] private float m_hudDistance = 2f;

        private PlayerHealthMotif m_playerHealth;
        private float m_hitMarkerTimer;
        private float m_damageTimer;
        private float m_respawnTimer;
        private bool m_isRespawning;

        private IEnumerator Start()
        {
            // Wait for local player's health component to be spawned
            while (m_playerHealth == null)
            {
                var players = FindObjectsByType<PlayerHealthMotif>(FindObjectsSortMode.None);
                foreach (var player in players)
                {
                    if (player.Object != null && player.Object.HasStateAuthority)
                    {
                        m_playerHealth = player;
                        break;
                    }
                }

                if (m_playerHealth == null)
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }

            m_playerHealth.OnHealthUpdated += UpdateHealth;
            m_playerHealth.OnScoreUpdated += UpdateScore;
            m_playerHealth.OnPlayerDied += OnPlayerDied;
            m_playerHealth.OnPlayerRespawned += OnPlayerRespawned;

            // Initialize UI
            UpdateHealth(m_playerHealth.CurrentHealth, 100);
            UpdateScore(m_playerHealth.Kills, m_playerHealth.Deaths);

            // Hide markers initially
            if (m_hitMarker != null)
            {
                m_hitMarker.gameObject.SetActive(false);
            }

            if (m_damageIndicator != null)
            {
                m_damageIndicator.gameObject.SetActive(false);
            }

            if (m_deathPanel != null)
            {
                m_deathPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (m_playerHealth != null)
            {
                m_playerHealth.OnHealthUpdated -= UpdateHealth;
                m_playerHealth.OnScoreUpdated -= UpdateScore;
                m_playerHealth.OnPlayerDied -= OnPlayerDied;
                m_playerHealth.OnPlayerRespawned -= OnPlayerRespawned;
            }
        }

        private void Update()
        {
            // Update hit marker
            if (m_hitMarkerTimer > 0)
            {
                m_hitMarkerTimer -= Time.deltaTime;
                if (m_hitMarkerTimer <= 0 && m_hitMarker != null)
                {
                    m_hitMarker.gameObject.SetActive(false);
                }
            }

            // Update damage indicator
            if (m_damageTimer > 0)
            {
                m_damageTimer -= Time.deltaTime;
                if (m_damageIndicator != null)
                {
                    var alpha = Mathf.Lerp(0, 0.5f, m_damageTimer / m_damageIndicatorDuration);
                    var color = m_damageIndicator.color;
                    color.a = alpha;
                    m_damageIndicator.color = color;

                    if (m_damageTimer <= 0)
                    {
                        m_damageIndicator.gameObject.SetActive(false);
                    }
                }
            }

            // Update respawn timer
            if (m_isRespawning && m_respawnText != null)
            {
                m_respawnTimer -= Time.deltaTime;
                m_respawnText.text = $"Respawning in {Mathf.CeilToInt(m_respawnTimer)}...";
            }
        }

        private void UpdateHealth(int current, int max)
        {
            var healthPercent = (float)current / max;

            if (m_healthSlider != null)
            {
                m_healthSlider.value = healthPercent;
            }

            if (m_healthText != null)
            {
                m_healthText.text = $"{current}";
            }

            // Show damage indicator if health decreased
            ShowDamageIndicator();
        }

        private void UpdateScore(int kills, int deaths)
        {
            if (m_killsText != null)
            {
                m_killsText.text = $"Kills: {kills}";
            }

            if (m_deathsText != null)
            {
                m_deathsText.text = $"Deaths: {deaths}";
            }
        }

        private void OnPlayerDied(Fusion.PlayerRef killer)
        {
            if (m_deathPanel != null)
            {
                m_deathPanel.SetActive(true);
            }

            if (m_crosshair != null)
            {
                m_crosshair.gameObject.SetActive(false);
            }

            m_isRespawning = true;
            m_respawnTimer = 3f; // Match respawn delay in PlayerHealthMotif
        }

        private void OnPlayerRespawned()
        {
            if (m_deathPanel != null)
            {
                m_deathPanel.SetActive(false);
            }

            if (m_crosshair != null)
            {
                m_crosshair.gameObject.SetActive(true);
            }

            m_isRespawning = false;
        }

        /// <summary>
        /// Show hit marker when player lands a hit on enemy.
        /// </summary>
        public void ShowHitMarker()
        {
            if (m_hitMarker != null)
            {
                m_hitMarker.gameObject.SetActive(true);
                m_hitMarkerTimer = m_hitMarkerDuration;
            }
        }

        /// <summary>
        /// Show damage indicator when player takes damage.
        /// </summary>
        public void ShowDamageIndicator()
        {
            if (m_damageIndicator != null)
            {
                m_damageIndicator.gameObject.SetActive(true);
                m_damageTimer = m_damageIndicatorDuration;

                var color = m_damageIndicator.color;
                color.a = 0.5f;
                m_damageIndicator.color = color;
            }
        }
    }
}
#endif
