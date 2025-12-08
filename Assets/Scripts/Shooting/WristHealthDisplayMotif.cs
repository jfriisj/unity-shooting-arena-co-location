// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Meta.XR.Samples;

namespace MRMotifs.Shooting
{
    /// <summary>
    /// Displays player health on the inner side of the right wrist.
    /// The player can raise their arm to see their health status.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class WristHealthDisplayMotif : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Offset from the wrist anchor - positioned on inner side of right wrist.")]
        [SerializeField] private Vector3 m_positionOffset = new Vector3(-0.08f, 0.02f, -0.02f);

        [Tooltip("Rotation offset (Euler angles) - faces player when looking at inner wrist.")]
        [SerializeField] private Vector3 m_rotationOffset = new Vector3(0f, 90f, 0f);

        [Tooltip("Scale of the health display.")]
        [SerializeField] private float m_scale = 0.0008f;

        private PlayerHealthMotif m_playerHealth;
        private OVRCameraRig m_cameraRig;
        private GameObject m_healthDisplay;
        private Image m_healthBarFill;
        private TextMeshProUGUI m_healthText;
        private Canvas m_canvas;

        private void Start()
        {
            m_cameraRig = FindAnyObjectByType<OVRCameraRig>();
            if (m_cameraRig == null)
            {
                Debug.LogWarning("[WristHealthDisplayMotif] OVRCameraRig not found");
                return;
            }

            // Create the wrist health display
            CreateWristHealthDisplay();

            // Start looking for player health
            StartCoroutine(WaitForPlayerHealth());
        }

        private System.Collections.IEnumerator WaitForPlayerHealth()
        {
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

            // Subscribe to health updates
            m_playerHealth.OnHealthUpdated += UpdateHealth;

            // Initialize with local health value
            UpdateHealth(m_playerHealth.GetCurrentHealth(), 100);
            Debug.Log("[WristHealthDisplayMotif] Connected to player health");
        }

        private void CreateWristHealthDisplay()
        {
            // Get the right hand anchor - display on inner wrist (left side of right arm)
            var rightHand = m_cameraRig.rightControllerAnchor;
            if (rightHand == null)
            {
                Debug.LogWarning("[WristHealthDisplayMotif] Right controller anchor not found");
                return;
            }

            // Create canvas for world-space UI
            m_healthDisplay = new GameObject("WristHealthDisplay");
            m_healthDisplay.transform.SetParent(rightHand, false);
            m_healthDisplay.transform.localPosition = m_positionOffset;
            m_healthDisplay.transform.localRotation = Quaternion.Euler(m_rotationOffset);
            m_healthDisplay.transform.localScale = Vector3.one * m_scale;

            m_canvas = m_healthDisplay.AddComponent<Canvas>();
            m_canvas.renderMode = RenderMode.WorldSpace;

            var canvasRect = m_healthDisplay.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(200, 80);

            // Add canvas scaler for consistent sizing
            var scaler = m_healthDisplay.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            // Add graphic raycaster (optional, for interactivity)
            m_healthDisplay.AddComponent<GraphicRaycaster>();

            // Create background panel
            var bgPanel = CreateUIElement("Background", m_healthDisplay.transform);
            var bgRect = bgPanel.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImage = bgPanel.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.7f);

            // Create health bar background
            var healthBarBg = CreateUIElement("HealthBarBg", bgPanel.transform);
            var healthBarBgRect = healthBarBg.GetComponent<RectTransform>();
            healthBarBgRect.anchorMin = new Vector2(0.05f, 0.2f);
            healthBarBgRect.anchorMax = new Vector2(0.95f, 0.5f);
            healthBarBgRect.offsetMin = Vector2.zero;
            healthBarBgRect.offsetMax = Vector2.zero;
            var healthBarBgImg = healthBarBg.AddComponent<Image>();
            healthBarBgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Create health bar fill
            var healthBarFill = CreateUIElement("HealthBarFill", healthBarBg.transform);
            var healthBarFillRect = healthBarFill.GetComponent<RectTransform>();
            healthBarFillRect.anchorMin = Vector2.zero;
            healthBarFillRect.anchorMax = Vector2.one;
            healthBarFillRect.offsetMin = new Vector2(2, 2);
            healthBarFillRect.offsetMax = new Vector2(-2, -2);
            m_healthBarFill = healthBarFill.AddComponent<Image>();
            m_healthBarFill.color = new Color(0.2f, 0.8f, 0.2f, 1f);

            // Create health text
            var healthTextObj = CreateUIElement("HealthText", bgPanel.transform);
            var healthTextRect = healthTextObj.GetComponent<RectTransform>();
            healthTextRect.anchorMin = new Vector2(0, 0.5f);
            healthTextRect.anchorMax = new Vector2(1, 0.95f);
            healthTextRect.offsetMin = Vector2.zero;
            healthTextRect.offsetMax = Vector2.zero;
            m_healthText = healthTextObj.AddComponent<TextMeshProUGUI>();
            m_healthText.text = "100";
            m_healthText.fontSize = 36;
            m_healthText.alignment = TextAlignmentOptions.Center;
            m_healthText.color = Color.white;
            m_healthText.fontStyle = FontStyles.Bold;

            Debug.Log("[WristHealthDisplayMotif] Wrist health display created");
        }

        private GameObject CreateUIElement(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private void UpdateHealth(int current, int max)
        {
            if (m_healthBarFill == null || m_healthText == null) return;

            var healthPercent = (float)current / max;

            // Update health bar fill
            var rect = m_healthBarFill.rectTransform;
            rect.anchorMax = new Vector2(healthPercent, 1);

            // Update color based on health
            if (healthPercent > 0.5f)
            {
                m_healthBarFill.color = Color.Lerp(Color.yellow, Color.green, (healthPercent - 0.5f) * 2f);
            }
            else
            {
                m_healthBarFill.color = Color.Lerp(Color.red, Color.yellow, healthPercent * 2f);
            }

            // Update text
            m_healthText.text = current.ToString();
        }

        private void OnDestroy()
        {
            if (m_playerHealth != null)
            {
                m_playerHealth.OnHealthUpdated -= UpdateHealth;
            }

            if (m_healthDisplay != null)
            {
                Destroy(m_healthDisplay);
            }
        }
    }
}
#endif
