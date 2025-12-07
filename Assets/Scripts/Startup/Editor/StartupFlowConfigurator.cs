// Copyright (c) Meta Platforms, Inc. and affiliates.

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace MRMotifs.SharedActivities.Startup.Editor
{
    /// <summary>
    /// Editor utility to configure the Game Startup Manager in the scene.
    /// </summary>
    public static class StartupFlowConfigurator
    {
        [MenuItem("Tools/MR Motifs/Setup Game Startup Manager")]
        public static void SetupGameStartupManager()
        {
            // Create StartupFlowConfig asset if it doesn't exist
            var configPath = "Assets/Resources/StartupFlowConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<StartupFlowConfig>(configPath);
            
            if (config == null)
            {
                // Ensure Resources folder exists
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                
                config = ScriptableObject.CreateInstance<StartupFlowConfig>();
                AssetDatabase.CreateAsset(config, configPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[StartupFlowConfigurator] Created StartupFlowConfig.asset");
            }

            // Find or create the Game Startup Manager GameObject
            var existingManager = Object.FindAnyObjectByType<GameStartupManagerMotif>();
            if (existingManager != null)
            {
                Debug.Log("[StartupFlowConfigurator] GameStartupManagerMotif already exists in scene");
                
                // Ensure config is assigned
                var so = new SerializedObject(existingManager);
                var configProp = so.FindProperty("m_config");
                if (configProp.objectReferenceValue == null)
                {
                    configProp.objectReferenceValue = config;
                    so.ApplyModifiedProperties();
                    Debug.Log("[StartupFlowConfigurator] Assigned StartupFlowConfig to existing manager");
                }
                
                Selection.activeGameObject = existingManager.gameObject;
                return;
            }

            // Create new GameObject
            var gameObject = new GameObject("[MR Motif] Game Startup");
            var startupManager = gameObject.AddComponent<GameStartupManagerMotif>();
            
            // Assign config
            var serializedObj = new SerializedObject(startupManager);
            var configProperty = serializedObj.FindProperty("m_config");
            configProperty.objectReferenceValue = config;
            serializedObj.ApplyModifiedProperties();

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            
            Debug.Log("[StartupFlowConfigurator] Created [MR Motif] Game Startup with GameStartupManagerMotif");
            Selection.activeGameObject = gameObject;
        }

        [MenuItem("Tools/MR Motifs/Create Startup Modal Canvas")]
        public static void CreateStartupModalCanvas()
        {
            // Check if Canvas already exists
            var existingUI = Object.FindAnyObjectByType<StartupModalUI>();
            if (existingUI != null)
            {
                Debug.Log("[StartupFlowConfigurator] StartupModalUI already exists in scene");
                Selection.activeGameObject = existingUI.gameObject;
                return;
            }

            // Create Canvas
            var canvasGO = new GameObject("StartupModalCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // High sorting order to appear on top
            
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Add CanvasGroup for fading
            var canvasGroup = canvasGO.AddComponent<CanvasGroup>();

            // Create Modal Panel
            var modalPanel = new GameObject("ModalPanel");
            modalPanel.transform.SetParent(canvasGO.transform, false);
            var modalRect = modalPanel.AddComponent<RectTransform>();
            modalRect.anchorMin = Vector2.zero;
            modalRect.anchorMax = Vector2.one;
            modalRect.sizeDelta = Vector2.zero;

            // Add background image
            var bgImage = modalPanel.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0, 0, 0, 0.8f);

            // Create content container
            var contentPanel = new GameObject("ContentPanel");
            contentPanel.transform.SetParent(modalPanel.transform, false);
            var contentRect = contentPanel.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.25f, 0.25f);
            contentRect.anchorMax = new Vector2(0.75f, 0.75f);
            contentRect.sizeDelta = Vector2.zero;
            
            var contentBg = contentPanel.AddComponent<UnityEngine.UI.Image>();
            contentBg.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            // Create Title Text
            var titleGO = new GameObject("TitleText");
            titleGO.transform.SetParent(contentPanel.transform, false);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.85f);
            titleRect.anchorMax = new Vector2(1, 1f);
            titleRect.sizeDelta = Vector2.zero;
            var titleText = titleGO.AddComponent<TMPro.TextMeshProUGUI>();
            titleText.text = "Setting Up Game...";
            titleText.fontSize = 36;
            titleText.alignment = TMPro.TextAlignmentOptions.Center;
            titleText.color = Color.white;

            // Create Role Text
            var roleGO = new GameObject("RoleText");
            roleGO.transform.SetParent(contentPanel.transform, false);
            var roleRect = roleGO.AddComponent<RectTransform>();
            roleRect.anchorMin = new Vector2(0, 0.75f);
            roleRect.anchorMax = new Vector2(1, 0.85f);
            roleRect.sizeDelta = Vector2.zero;
            var roleText = roleGO.AddComponent<TMPro.TextMeshProUGUI>();
            roleText.text = "HOST";
            roleText.fontSize = 24;
            roleText.alignment = TMPro.TextAlignmentOptions.Center;
            roleText.color = new Color(0.2f, 0.8f, 0.2f);

            // Create Status Text
            var statusGO = new GameObject("StatusText");
            statusGO.transform.SetParent(contentPanel.transform, false);
            var statusRect = statusGO.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 0.55f);
            statusRect.anchorMax = new Vector2(1, 0.75f);
            statusRect.sizeDelta = Vector2.zero;
            var statusText = statusGO.AddComponent<TMPro.TextMeshProUGUI>();
            statusText.text = "Initializing...";
            statusText.fontSize = 24;
            statusText.alignment = TMPro.TextAlignmentOptions.Center;
            statusText.color = Color.white;

            // Create Progress Bar
            var progressGO = new GameObject("ProgressBar");
            progressGO.transform.SetParent(contentPanel.transform, false);
            var progressRect = progressGO.AddComponent<RectTransform>();
            progressRect.anchorMin = new Vector2(0.1f, 0.45f);
            progressRect.anchorMax = new Vector2(0.9f, 0.55f);
            progressRect.sizeDelta = Vector2.zero;
            
            var progressBg = progressGO.AddComponent<UnityEngine.UI.Image>();
            progressBg.color = new Color(0.3f, 0.3f, 0.3f);

            var progressSlider = progressGO.AddComponent<UnityEngine.UI.Slider>();
            progressSlider.minValue = 0;
            progressSlider.maxValue = 1;
            progressSlider.value = 0;

            // Create fill area for slider
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(progressGO.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.sizeDelta = Vector2.zero;
            var fillImage = fill.AddComponent<UnityEngine.UI.Image>();
            fillImage.color = new Color(0.2f, 0.7f, 1f);

            progressSlider.fillRect = fillRect;

            // Create Spinner
            var spinnerGO = new GameObject("Spinner");
            spinnerGO.transform.SetParent(contentPanel.transform, false);
            var spinnerRect = spinnerGO.AddComponent<RectTransform>();
            spinnerRect.anchorMin = new Vector2(0.45f, 0.2f);
            spinnerRect.anchorMax = new Vector2(0.55f, 0.4f);
            spinnerRect.sizeDelta = Vector2.zero;
            var spinnerImage = spinnerGO.AddComponent<UnityEngine.UI.Image>();
            spinnerImage.color = new Color(0.2f, 0.7f, 1f);

            // Create Error Panel (hidden by default)
            var errorPanel = new GameObject("ErrorPanel");
            errorPanel.transform.SetParent(contentPanel.transform, false);
            var errorPanelRect = errorPanel.AddComponent<RectTransform>();
            errorPanelRect.anchorMin = new Vector2(0.1f, 0.05f);
            errorPanelRect.anchorMax = new Vector2(0.9f, 0.4f);
            errorPanelRect.sizeDelta = Vector2.zero;
            errorPanel.SetActive(false);

            var errorBg = errorPanel.AddComponent<UnityEngine.UI.Image>();
            errorBg.color = new Color(0.4f, 0.1f, 0.1f, 0.9f);

            // Error Text
            var errorTextGO = new GameObject("ErrorText");
            errorTextGO.transform.SetParent(errorPanel.transform, false);
            var errorTextRect = errorTextGO.AddComponent<RectTransform>();
            errorTextRect.anchorMin = new Vector2(0.05f, 0.4f);
            errorTextRect.anchorMax = new Vector2(0.95f, 0.95f);
            errorTextRect.sizeDelta = Vector2.zero;
            var errorText = errorTextGO.AddComponent<TMPro.TextMeshProUGUI>();
            errorText.text = "An error occurred";
            errorText.fontSize = 18;
            errorText.alignment = TMPro.TextAlignmentOptions.Center;
            errorText.color = Color.white;

            // Retry Button
            var retryButtonGO = new GameObject("RetryButton");
            retryButtonGO.transform.SetParent(errorPanel.transform, false);
            var retryRect = retryButtonGO.AddComponent<RectTransform>();
            retryRect.anchorMin = new Vector2(0.3f, 0.05f);
            retryRect.anchorMax = new Vector2(0.7f, 0.35f);
            retryRect.sizeDelta = Vector2.zero;
            
            var retryButtonImage = retryButtonGO.AddComponent<UnityEngine.UI.Image>();
            retryButtonImage.color = new Color(0.2f, 0.5f, 0.8f);
            var retryButton = retryButtonGO.AddComponent<UnityEngine.UI.Button>();

            var retryTextGO = new GameObject("Text");
            retryTextGO.transform.SetParent(retryButtonGO.transform, false);
            var retryTextRect = retryTextGO.AddComponent<RectTransform>();
            retryTextRect.anchorMin = Vector2.zero;
            retryTextRect.anchorMax = Vector2.one;
            retryTextRect.sizeDelta = Vector2.zero;
            var retryText = retryTextGO.AddComponent<TMPro.TextMeshProUGUI>();
            retryText.text = "Retry";
            retryText.fontSize = 18;
            retryText.alignment = TMPro.TextAlignmentOptions.Center;
            retryText.color = Color.white;

            // Add StartupModalUI component
            var modalUI = canvasGO.AddComponent<StartupModalUI>();
            
            // Assign references via SerializedObject
            var so = new SerializedObject(modalUI);
            so.FindProperty("m_modalPanel").objectReferenceValue = modalPanel;
            so.FindProperty("m_canvasGroup").objectReferenceValue = canvasGroup;
            so.FindProperty("m_titleText").objectReferenceValue = titleText;
            so.FindProperty("m_statusText").objectReferenceValue = statusText;
            so.FindProperty("m_roleText").objectReferenceValue = roleText;
            so.FindProperty("m_progressBar").objectReferenceValue = progressSlider;
            so.FindProperty("m_spinner").objectReferenceValue = spinnerGO;
            so.FindProperty("m_errorPanel").objectReferenceValue = errorPanel;
            so.FindProperty("m_errorText").objectReferenceValue = errorText;
            so.FindProperty("m_retryButton").objectReferenceValue = retryButton;
            so.ApplyModifiedProperties();

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvasGO.scene);

            Debug.Log("[StartupFlowConfigurator] Created StartupModalCanvas with UI elements");
            Selection.activeGameObject = canvasGO;
        }

        [MenuItem("Tools/MR Motifs/Link Startup Manager to Modal UI")]
        public static void LinkStartupManagerToModalUI()
        {
            var manager = Object.FindAnyObjectByType<GameStartupManagerMotif>();
            var modalUI = Object.FindAnyObjectByType<StartupModalUI>();

            if (manager == null)
            {
                Debug.LogError("[StartupFlowConfigurator] GameStartupManagerMotif not found in scene. Run 'Setup Game Startup Manager' first.");
                return;
            }

            if (modalUI == null)
            {
                Debug.LogError("[StartupFlowConfigurator] StartupModalUI not found in scene. Run 'Create Startup Modal Canvas' first.");
                return;
            }

            var so = new SerializedObject(manager);
            so.FindProperty("m_modalUI").objectReferenceValue = modalUI;
            so.ApplyModifiedProperties();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            Debug.Log("[StartupFlowConfigurator] Linked GameStartupManagerMotif to StartupModalUI");
        }
    }
}
#endif
