// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MRMotifs.SharedActivities.Startup;

namespace MRMotifs.Startup.Editor
{
    /// <summary>
    /// Editor utility for setting up the Startup Modal UI in the scene.
    /// </summary>
    public static class StartupFlowSetup
    {
        [MenuItem("Tools/MR Motifs/Setup Startup Modal UI")]
        public static void SetupStartupModalUI()
        {
            // Create the canvas
            var canvasGO = new GameObject("[MR Motif] Startup Modal Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // Position the canvas in front of the camera
            canvasGO.transform.position = new Vector3(0, 1.5f, 2f);
            canvasGO.transform.localScale = Vector3.one * 0.002f; // Scale down for world space
            
            var rectTransform = canvasGO.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(800, 600);
            
            // Create panel background
            var panelGO = new GameObject("Modal Panel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            var panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = new Vector2(50, 50);
            panelRect.offsetMax = new Vector2(-50, -50);
            
            var panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            
            // Create title text
            var titleGO = new GameObject("Title Text");
            titleGO.transform.SetParent(panelGO.transform, false);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.8f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(20, 10);
            titleRect.offsetMax = new Vector2(-20, -20);
            
            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "Starting Game...";
            titleText.fontSize = 48;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
            
            // Create status text
            var statusGO = new GameObject("Status Text");
            statusGO.transform.SetParent(panelGO.transform, false);
            var statusRect = statusGO.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 0.4f);
            statusRect.anchorMax = new Vector2(1, 0.75f);
            statusRect.offsetMin = new Vector2(40, 0);
            statusRect.offsetMax = new Vector2(-40, 0);
            
            var statusText = statusGO.AddComponent<TextMeshProUGUI>();
            statusText.text = "Initializing...";
            statusText.fontSize = 28;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.color = new Color(0.8f, 0.8f, 0.8f);
            
            // Create progress bar background
            var progressBgGO = new GameObject("Progress Bar BG");
            progressBgGO.transform.SetParent(panelGO.transform, false);
            var progressBgRect = progressBgGO.AddComponent<RectTransform>();
            progressBgRect.anchorMin = new Vector2(0.1f, 0.25f);
            progressBgRect.anchorMax = new Vector2(0.9f, 0.35f);
            progressBgRect.offsetMin = Vector2.zero;
            progressBgRect.offsetMax = Vector2.zero;
            
            var progressBgImage = progressBgGO.AddComponent<Image>();
            progressBgImage.color = new Color(0.2f, 0.2f, 0.2f);
            
            // Create progress bar fill
            var progressFillGO = new GameObject("Progress Bar Fill");
            progressFillGO.transform.SetParent(progressBgGO.transform, false);
            var progressFillRect = progressFillGO.AddComponent<RectTransform>();
            progressFillRect.anchorMin = Vector2.zero;
            progressFillRect.anchorMax = new Vector2(0.5f, 1); // Start at 50% for demo
            progressFillRect.offsetMin = new Vector2(4, 4);
            progressFillRect.offsetMax = new Vector2(-4, -4);
            
            var progressFillImage = progressFillGO.AddComponent<Image>();
            progressFillImage.color = new Color(0.2f, 0.6f, 1f);
            
            // Create error text
            var errorGO = new GameObject("Error Text");
            errorGO.transform.SetParent(panelGO.transform, false);
            var errorRect = errorGO.AddComponent<RectTransform>();
            errorRect.anchorMin = new Vector2(0, 0.1f);
            errorRect.anchorMax = new Vector2(1, 0.22f);
            errorRect.offsetMin = new Vector2(20, 0);
            errorRect.offsetMax = new Vector2(-20, 0);
            
            var errorText = errorGO.AddComponent<TextMeshProUGUI>();
            errorText.text = "";
            errorText.fontSize = 20;
            errorText.alignment = TextAlignmentOptions.Center;
            errorText.color = new Color(1f, 0.4f, 0.4f);
            errorGO.SetActive(false);
            
            // Create retry button
            var retryBtnGO = new GameObject("Retry Button");
            retryBtnGO.transform.SetParent(panelGO.transform, false);
            var retryBtnRect = retryBtnGO.AddComponent<RectTransform>();
            retryBtnRect.anchorMin = new Vector2(0.35f, 0.02f);
            retryBtnRect.anchorMax = new Vector2(0.65f, 0.12f);
            retryBtnRect.offsetMin = Vector2.zero;
            retryBtnRect.offsetMax = Vector2.zero;
            
            var retryBtnImage = retryBtnGO.AddComponent<Image>();
            retryBtnImage.color = new Color(0.3f, 0.3f, 0.3f);
            var retryBtn = retryBtnGO.AddComponent<Button>();
            
            var retryBtnTextGO = new GameObject("Text");
            retryBtnTextGO.transform.SetParent(retryBtnGO.transform, false);
            var retryBtnTextRect = retryBtnTextGO.AddComponent<RectTransform>();
            retryBtnTextRect.anchorMin = Vector2.zero;
            retryBtnTextRect.anchorMax = Vector2.one;
            retryBtnTextRect.offsetMin = Vector2.zero;
            retryBtnTextRect.offsetMax = Vector2.zero;
            
            var retryBtnText = retryBtnTextGO.AddComponent<TextMeshProUGUI>();
            retryBtnText.text = "Retry";
            retryBtnText.fontSize = 24;
            retryBtnText.alignment = TextAlignmentOptions.Center;
            retryBtnText.color = Color.white;
            retryBtnGO.SetActive(false);
            
            // Add StartupModalUI component
            var modalUI = canvasGO.AddComponent<StartupModalUI>();
            
            // Use reflection to set the serialized fields
            var serializedObj = new SerializedObject(modalUI);
            serializedObj.FindProperty("m_modalPanel").objectReferenceValue = panelGO;
            serializedObj.FindProperty("m_titleText").objectReferenceValue = titleText;
            serializedObj.FindProperty("m_statusText").objectReferenceValue = statusText;
            serializedObj.FindProperty("m_progressBarFill").objectReferenceValue = progressFillImage;
            serializedObj.FindProperty("m_errorText").objectReferenceValue = errorText;
            serializedObj.FindProperty("m_retryButton").objectReferenceValue = retryBtn;
            serializedObj.ApplyModifiedProperties();
            
            // Select the created object
            Selection.activeGameObject = canvasGO;
            
            Debug.Log("[StartupFlowSetup] Created Startup Modal Canvas. Now add the GameStartupManagerMotif component and wire up the references.");
            EditorUtility.DisplayDialog("Startup Modal Created", 
                "Created '[MR Motif] Startup Modal Canvas' with StartupModalUI component.\n\n" +
                "Next steps:\n" +
                "1. Create a StartupFlowConfig asset (Right-click > Create > MR Motifs > Startup Flow Config)\n" +
                "2. Add GameStartupManagerMotif to a separate GameObject\n" +
                "3. Assign references in GameStartupManagerMotif",
                "OK");
        }
        
        [MenuItem("Assets/Create/MR Motifs/Startup Flow Config")]
        public static void CreateStartupFlowConfig()
        {
            var asset = ScriptableObject.CreateInstance<StartupFlowConfig>();
            
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path))
            {
                path = "Assets";
            }
            else if (System.IO.Path.GetExtension(path) != "")
            {
                path = path.Replace(System.IO.Path.GetFileName(path), "");
            }
            
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(path + "/StartupFlowConfig.asset");
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
