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
    /// Debug visualization for the shooting game.
    /// Shows player positions, health bars, bullet trajectories, and game state.
    /// Toggle with both thumbstick buttons pressed simultaneously.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class ShootingDebugVisualizerMotif : MonoBehaviour
    {
        [Header("Debug Settings")]
        [Tooltip("Enable debug visualization on start.")]
        [SerializeField] private bool m_enabledOnStart = false;

        [Tooltip("Show floating health bars above players.")]
        [SerializeField] private bool m_showHealthBars = true;

        [Tooltip("Show bullet trajectory lines.")]
        [SerializeField] private bool m_showBulletTrails = true;

        [Tooltip("Show game state info panel.")]
        [SerializeField] private bool m_showGameStatePanel = true;

        [Tooltip("Show spawn point markers.")]
        [SerializeField] private bool m_showSpawnPoints = true;

        [Tooltip("Show colocation anchor position.")]
        [SerializeField] private bool m_showAnchorPosition = true;

        [Header("Colors")]
        [SerializeField] private Color m_healthBarColor = Color.green;
        [SerializeField] private Color m_damageColor = Color.red;
        [SerializeField] private Color m_bulletTrailColor = Color.yellow;
        [SerializeField] private Color m_spawnPointColor = Color.cyan;
        [SerializeField] private Color m_anchorColor = Color.magenta;

        private bool m_isEnabled;
        private GameObject m_debugCanvas;
        private TextMeshProUGUI m_debugText;
        private readonly List<LineRenderer> m_bulletTrails = new();
        private readonly Dictionary<PlayerHealthMotif, GameObject> m_healthBarObjects = new();
        private GameObject m_anchorMarker;
        private readonly List<GameObject> m_spawnPointMarkers = new();
        private float m_toggleCooldown;
        
        private ShootingGameManagerMotif m_gameManager;
        private NetworkRunner m_networkRunner;

        private void Start()
        {
            m_isEnabled = m_enabledOnStart;
            m_gameManager = FindAnyObjectByType<ShootingGameManagerMotif>();
            m_networkRunner = FindAnyObjectByType<NetworkRunner>();
            
            if (m_isEnabled)
            {
                EnableDebugVisualization();
            }

            Debug.Log("[ShootingDebugVisualizer] Started - Press both thumbsticks to toggle debug view");
        }

        private void Update()
        {
            // Toggle with both thumbstick buttons
            m_toggleCooldown -= Time.deltaTime;
            
            bool leftThumb = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
            bool rightThumb = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch);
            
            if (leftThumb && rightThumb && m_toggleCooldown <= 0f)
            {
                m_toggleCooldown = 0.5f;
                ToggleDebugVisualization();
            }

            if (m_isEnabled)
            {
                UpdateDebugVisualization();
            }
        }

        private void OnDestroy()
        {
            DisableDebugVisualization();
        }

        public void ToggleDebugVisualization()
        {
            m_isEnabled = !m_isEnabled;
            
            if (m_isEnabled)
            {
                EnableDebugVisualization();
            }
            else
            {
                DisableDebugVisualization();
            }

            Debug.Log($"[ShootingDebugVisualizer] Debug visualization: {(m_isEnabled ? "ENABLED" : "DISABLED")}");
        }

        private void EnableDebugVisualization()
        {
            // Create debug canvas for game state info
            if (m_showGameStatePanel)
            {
                CreateDebugCanvas();
            }

            // Create spawn point markers
            if (m_showSpawnPoints)
            {
                CreateSpawnPointMarkers();
            }

            // Create anchor marker
            if (m_showAnchorPosition)
            {
                CreateAnchorMarker();
            }
        }

        private void DisableDebugVisualization()
        {
            // Destroy debug canvas
            if (m_debugCanvas != null)
            {
                Destroy(m_debugCanvas);
                m_debugCanvas = null;
            }

            // Destroy health bars
            foreach (var healthBar in m_healthBarObjects.Values)
            {
                if (healthBar != null)
                {
                    Destroy(healthBar);
                }
            }
            m_healthBarObjects.Clear();

            // Destroy bullet trails
            foreach (var trail in m_bulletTrails)
            {
                if (trail != null)
                {
                    Destroy(trail.gameObject);
                }
            }
            m_bulletTrails.Clear();

            // Destroy spawn point markers
            foreach (var marker in m_spawnPointMarkers)
            {
                if (marker != null)
                {
                    Destroy(marker);
                }
            }
            m_spawnPointMarkers.Clear();

            // Destroy anchor marker
            if (m_anchorMarker != null)
            {
                Destroy(m_anchorMarker);
                m_anchorMarker = null;
            }
        }

        private void CreateDebugCanvas()
        {
            var cameraRig = FindAnyObjectByType<OVRCameraRig>();
            if (cameraRig == null) return;

            m_debugCanvas = new GameObject("DebugCanvas");
            var canvas = m_debugCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            m_debugCanvas.transform.SetParent(cameraRig.centerEyeAnchor);
            m_debugCanvas.transform.localPosition = new Vector3(-0.3f, -0.2f, 0.5f);
            m_debugCanvas.transform.localRotation = Quaternion.identity;
            m_debugCanvas.transform.localScale = Vector3.one * 0.001f;

            // Create background panel
            var panel = new GameObject("Panel");
            panel.transform.SetParent(m_debugCanvas.transform);
            panel.transform.localPosition = Vector3.zero;
            panel.transform.localRotation = Quaternion.identity;
            panel.transform.localScale = Vector3.one;
            
            var panelImage = panel.AddComponent<UnityEngine.UI.Image>();
            panelImage.color = new Color(0, 0, 0, 0.7f);
            panelImage.rectTransform.sizeDelta = new Vector2(250, 200);

            // Create text
            var textObj = new GameObject("DebugText");
            textObj.transform.SetParent(panel.transform);
            textObj.transform.localPosition = Vector3.zero;
            textObj.transform.localRotation = Quaternion.identity;
            textObj.transform.localScale = Vector3.one;
            
            m_debugText = textObj.AddComponent<TextMeshProUGUI>();
            m_debugText.fontSize = 14;
            m_debugText.color = Color.white;
            m_debugText.alignment = TextAlignmentOptions.TopLeft;
            m_debugText.rectTransform.sizeDelta = new Vector2(240, 190);
            m_debugText.rectTransform.anchoredPosition = Vector2.zero;
        }

        private void CreateSpawnPointMarkers()
        {
            // Just create a marker at origin for open play area mode
            var marker = CreateMarker("SpawnOrigin", Vector3.zero, m_spawnPointColor, 0.15f);
            m_spawnPointMarkers.Add(marker);
        }

        private void CreateAnchorMarker()
        {
            // Will be positioned when anchor is found
            m_anchorMarker = CreateMarker("AnchorMarker", Vector3.zero, m_anchorColor, 0.2f);
            m_anchorMarker.SetActive(false);
        }

        private GameObject CreateMarker(string name, Vector3 position, Color color, float size)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = $"Debug_{name}";
            marker.transform.position = position;
            marker.transform.localScale = Vector3.one * size;

            // Remove collider
            var collider = marker.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            // Set material
            var renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                renderer.material.color = color;
                
                // Make it glow/emissive
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", color * 0.5f);
            }

            return marker;
        }

        private void UpdateDebugVisualization()
        {
            UpdateGameStatePanel();
            UpdateHealthBars();
            UpdateAnchorMarker();
            UpdateBulletTrails();
        }

        private void UpdateGameStatePanel()
        {
            if (m_debugText == null) return;

            string stateText = "=== SHOOTING DEBUG ===\n\n";

            // Game state
            if (m_gameManager != null)
            {
                stateText += $"State: {m_gameManager.CurrentGameState}\n";
                stateText += $"Time: {m_gameManager.RemainingTime:F1}s\n";
                stateText += $"Countdown: {m_gameManager.CountdownValue}\n\n";
            }
            else
            {
                stateText += "Game Manager: NOT FOUND\n\n";
            }

            // Network state
            if (m_networkRunner != null && m_networkRunner.IsRunning)
            {
                int playerCount = 0;
                foreach (var _ in m_networkRunner.ActivePlayers)
                {
                    playerCount++;
                }
                stateText += $"Network: CONNECTED\n";
                stateText += $"Players: {playerCount}\n";
                stateText += $"IsHost: {m_networkRunner.IsServer}\n\n";
            }
            else
            {
                stateText += "Network: DISCONNECTED\n\n";
            }

            // Player info
            var players = FindObjectsByType<PlayerHealthMotif>(FindObjectsSortMode.None);
            stateText += $"--- Players ({players.Length}) ---\n";
            foreach (var player in players)
            {
                if (player == null || player.Object == null || !player.Object.IsValid) continue;
                
                string localTag = player.Object.HasStateAuthority ? " (YOU)" : "";
                stateText += $"P{player.OwnerPlayer.PlayerId}{localTag}: {player.CurrentHealth}HP K:{player.Kills} D:{player.Deaths}\n";
            }

            m_debugText.text = stateText;
        }

        private void UpdateHealthBars()
        {
            if (!m_showHealthBars) return;

            var players = FindObjectsByType<PlayerHealthMotif>(FindObjectsSortMode.None);
            
            foreach (var player in players)
            {
                if (player == null || player.Object == null || !player.Object.IsValid) continue;

                // Create health bar if needed
                if (!m_healthBarObjects.ContainsKey(player))
                {
                    var healthBar = CreateHealthBar(player);
                    m_healthBarObjects[player] = healthBar;
                }

                // Update health bar
                var bar = m_healthBarObjects[player];
                if (bar != null)
                {
                    // Position above player's head
                    bar.transform.position = player.transform.position + Vector3.up * 0.3f;
                    
                    // Face camera
                    var cameraRig = FindAnyObjectByType<OVRCameraRig>();
                    if (cameraRig != null)
                    {
                        bar.transform.LookAt(cameraRig.centerEyeAnchor);
                        bar.transform.Rotate(0, 180, 0);
                    }

                    // Update fill
                    var fill = bar.transform.Find("Fill");
                    if (fill != null)
                    {
                        float healthPercent = player.CurrentHealth / 100f;
                        fill.localScale = new Vector3(healthPercent, 1, 1);
                        fill.localPosition = new Vector3((healthPercent - 1f) * 0.05f, 0, 0);
                        
                        var fillRenderer = fill.GetComponent<Renderer>();
                        if (fillRenderer != null)
                        {
                            fillRenderer.material.color = Color.Lerp(m_damageColor, m_healthBarColor, healthPercent);
                        }
                    }
                }
            }

            // Clean up stale health bars
            var staleKeys = new List<PlayerHealthMotif>();
            foreach (var kvp in m_healthBarObjects)
            {
                if (kvp.Key == null || kvp.Key.gameObject == null)
                {
                    staleKeys.Add(kvp.Key);
                    if (kvp.Value != null)
                    {
                        Destroy(kvp.Value);
                    }
                }
            }
            foreach (var key in staleKeys)
            {
                m_healthBarObjects.Remove(key);
            }
        }

        private GameObject CreateHealthBar(PlayerHealthMotif player)
        {
            var healthBar = new GameObject($"HealthBar_{player.OwnerPlayer.PlayerId}");
            
            // Background
            var bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bg.name = "Background";
            bg.transform.SetParent(healthBar.transform);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localScale = new Vector3(0.1f, 0.015f, 0.005f);
            Destroy(bg.GetComponent<Collider>());
            bg.GetComponent<Renderer>().material.color = Color.black;

            // Fill
            var fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fill.name = "Fill";
            fill.transform.SetParent(healthBar.transform);
            fill.transform.localPosition = Vector3.zero;
            fill.transform.localScale = new Vector3(0.1f, 0.012f, 0.006f);
            Destroy(fill.GetComponent<Collider>());
            fill.GetComponent<Renderer>().material.color = m_healthBarColor;

            return healthBar;
        }

        private void UpdateAnchorMarker()
        {
            if (!m_showAnchorPosition || m_anchorMarker == null) return;

            var anchor = FindAnyObjectByType<OVRSpatialAnchor>();
            if (anchor != null && anchor.Localized)
            {
                m_anchorMarker.SetActive(true);
                m_anchorMarker.transform.position = anchor.transform.position;
                
                // Rotate slowly for visibility
                m_anchorMarker.transform.Rotate(0, 90 * Time.deltaTime, 0);
            }
        }

        private void UpdateBulletTrails()
        {
            if (!m_showBulletTrails) return;

            // Find all bullets and draw debug lines from their origin
            var bullets = FindObjectsByType<BulletMotif>(FindObjectsSortMode.None);
            
            foreach (var bullet in bullets)
            {
                if (bullet != null)
                {
                    Debug.DrawRay(bullet.transform.position, bullet.transform.forward * 0.5f, m_bulletTrailColor);
                }
            }
        }
    }
}
#endif
