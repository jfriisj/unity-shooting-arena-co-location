using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MRMotifs.Debugging
{
    /// <summary>
    /// Helper script to quickly set up a debug log display UI.
    /// Use this in the editor to create the necessary UI components.
    /// </summary>
    public static class DebugLogDisplaySetup
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/MRMotifs/Create Debug Log Display")]
        public static void CreateDebugLogDisplay()
        {
            // Create main canvas
            var canvasGO = new GameObject("DebugLogCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // Position for VR
            canvasGO.transform.position = new Vector3(0, 1.5f, 2f);
            canvasGO.transform.localScale = Vector3.one * 0.001f; // Scale down for VR
            
            // Create log panel
            var logPanel = new GameObject("LogPanel");
            logPanel.transform.SetParent(canvasGO.transform, false);
            
            var panelImage = logPanel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);
            
            var panelRect = logPanel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(800, 600);
            
            // Create scroll rect
            var scrollRect = logPanel.AddComponent<ScrollRect>();
            
            // Create viewport
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(logPanel.transform, false);
            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = new Vector2(0, -50); // Leave space for buttons
            
            scrollRect.viewport = viewportRect;
            
            // Create content
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0, 1);
            contentRect.sizeDelta = new Vector2(0, 600);
            
            var contentLayoutGroup = content.AddComponent<VerticalLayoutGroup>();
            contentLayoutGroup.childControlHeight = false;
            contentLayoutGroup.childControlWidth = true;
            
            var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            
            // Create log text
            var logTextGO = new GameObject("LogText");
            logTextGO.transform.SetParent(content.transform, false);
            
            var logText = logTextGO.AddComponent<TextMeshProUGUI>();
            logText.text = "Debug logs will appear here...";
            logText.fontSize = 12;
            logText.color = Color.white;
            logText.alignment = TextAlignmentOptions.TopLeft;
            logText.enableWordWrapping = true;
            
            // Create buttons panel
            var buttonsPanel = new GameObject("ButtonsPanel");
            buttonsPanel.transform.SetParent(logPanel.transform, false);
            
            var buttonsPanelRect = buttonsPanel.GetComponent<RectTransform>();
            buttonsPanelRect.anchorMin = new Vector2(0, 0);
            buttonsPanelRect.anchorMax = new Vector2(1, 0);
            buttonsPanelRect.pivot = new Vector2(0.5f, 0);
            buttonsPanelRect.sizeDelta = new Vector2(0, 50);
            
            var buttonsLayoutGroup = buttonsPanel.AddComponent<HorizontalLayoutGroup>();
            buttonsLayoutGroup.spacing = 10;
            buttonsLayoutGroup.padding = new RectOffset(10, 10, 10, 10);
            
            // Create buttons
            var toggleButton = CreateButton("Toggle", buttonsPanel.transform);
            var clearButton = CreateButton("Clear", buttonsPanel.transform);
            var exportButton = CreateButton("Export", buttonsPanel.transform);
            
            // Create category filter dropdown
            var dropdownGO = new GameObject("CategoryFilter");
            dropdownGO.transform.SetParent(buttonsPanel.transform, false);
            
            var dropdown = dropdownGO.AddComponent<TMP_Dropdown>();
            dropdown.options.Add(new TMP_Dropdown.OptionData("ALL"));
            
            // Add the DebugLogDisplay component
            var debugLogDisplay = canvasGO.AddComponent<DebugLogDisplay>();
            
            // Assign references using reflection to access private fields
            var fields = typeof(DebugLogDisplay).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                switch (field.Name)
                {
                    case "m_logPanel":
                        field.SetValue(debugLogDisplay, logPanel);
                        break;
                    case "m_scrollRect":
                        field.SetValue(debugLogDisplay, scrollRect);
                        break;
                    case "m_logText":
                        field.SetValue(debugLogDisplay, logText);
                        break;
                    case "m_toggleButton":
                        field.SetValue(debugLogDisplay, toggleButton);
                        break;
                    case "m_clearButton":
                        field.SetValue(debugLogDisplay, clearButton);
                        break;
                    case "m_exportButton":
                        field.SetValue(debugLogDisplay, exportButton);
                        break;
                    case "m_categoryFilter":
                        field.SetValue(debugLogDisplay, dropdown);
                        break;
                }
            }
            
            // Select the created object
            UnityEditor.Selection.activeGameObject = canvasGO;
            
            Debug.Log("[DebugLogDisplaySetup] Debug log display UI created successfully!");
        }
        
        private static Button CreateButton(string text, Transform parent)
        {
            var buttonGO = new GameObject(text + "Button");
            buttonGO.transform.SetParent(parent, false);
            
            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            var button = buttonGO.AddComponent<Button>();
            
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            var textComponent = textGO.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 14;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            return button;
        }
#endif
    }
}