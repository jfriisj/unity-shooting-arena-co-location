// Copyright (c) Meta Platforms, Inc. and affiliates.

#if UNITY_EDITOR && FUSION2
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace MRMotifs.SharedActivities.Startup.Editor
{
    /// <summary>
    /// Editor utilities for setting up the Role Selection system.
    /// </summary>
    public static class RoleSelectionSetup
    {
        [MenuItem("Tools/MR Motifs/Setup Role Selection System")]
        public static void SetupRoleSelectionSystem()
        {
            // 1. Create SessionDiscoveryManager
            CreateSessionDiscoveryManager();

            // 2. Create Role Selection Canvas
            CreateRoleSelectionCanvas();

            // 3. Configure GameStartupManager
            ConfigureGameStartupManager();

            // 4. Configure Auto Matchmaking
            ConfigureAutoMatchmaking();

            Debug.Log("[RoleSelectionSetup] Role Selection System setup complete!");
            
            // Save scene
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        private static void CreateSessionDiscoveryManager()
        {
            var existing = Object.FindAnyObjectByType<SessionDiscoveryManager>();
            if (existing != null)
            {
                Debug.Log("[RoleSelectionSetup] SessionDiscoveryManager already exists");
                return;
            }

            var go = new GameObject("[MR Motif] Session Discovery");
            go.AddComponent<SessionDiscoveryManager>();
            Undo.RegisterCreatedObjectUndo(go, "Create Session Discovery Manager");
            Debug.Log("[RoleSelectionSetup] Created SessionDiscoveryManager");
        }

        private static void CreateRoleSelectionCanvas()
        {
            var existing = Object.FindAnyObjectByType<RoleSelectionModalUI>();
            if (existing != null)
            {
                Debug.Log("[RoleSelectionSetup] RoleSelectionModalUI already exists");
                return;
            }

            // Create Canvas
            var canvasGO = new GameObject("RoleSelectionCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.dynamicPixelsPerUnit = 10f;
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            var canvasGroup = canvasGO.AddComponent<CanvasGroup>();

            // Position canvas in front of camera
            var cameraRig = Object.FindAnyObjectByType<OVRCameraRig>();
            if (cameraRig != null)
            {
                canvasGO.transform.SetParent(cameraRig.centerEyeAnchor);
                canvasGO.transform.localPosition = new Vector3(0, 0, 1.5f);
                canvasGO.transform.localRotation = Quaternion.identity;
                canvasGO.transform.localScale = Vector3.one * 0.001f;
            }
            else
            {
                canvasGO.transform.position = new Vector3(0, 1.5f, 2f);
                canvasGO.transform.localScale = Vector3.one * 0.001f;
            }

            // Create Modal Panel
            var modalPanel = CreatePanel(canvasGO.transform, "ModalPanel", new Vector2(800, 600));
            var modalPanelImage = modalPanel.GetComponent<Image>();
            modalPanelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Create Role Selection Panel
            var rolePanel = CreatePanel(modalPanel.transform, "RoleSelectionPanel", new Vector2(780, 580));
            var rolePanelImage = rolePanel.GetComponent<Image>();
            rolePanelImage.color = new Color(0, 0, 0, 0); // Transparent

            // Title
            var title = CreateText(rolePanel.transform, "TitleText", "Choose Your Role", 48);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -60);

            // Host Button
            var hostButton = CreateButton(rolePanel.transform, "HostButton", "HOST", new Vector2(-150, -50));
            var hostButtonRect = hostButton.GetComponent<RectTransform>();
            hostButtonRect.sizeDelta = new Vector2(300, 200);
            var hostButtonImage = hostButton.GetComponent<Image>();
            hostButtonImage.color = new Color(0.2f, 0.6f, 0.2f, 1f);

            // Host Description
            var hostDesc = CreateText(hostButton.transform, "DescText", "Create a new game\nand invite friends", 20);
            var hostDescRect = hostDesc.GetComponent<RectTransform>();
            hostDescRect.anchoredPosition = new Vector2(0, -40);

            // Client Button
            var clientButton = CreateButton(rolePanel.transform, "ClientButton", "JOIN GAME", new Vector2(150, -50));
            var clientButtonRect = clientButton.GetComponent<RectTransform>();
            clientButtonRect.sizeDelta = new Vector2(300, 200);
            var clientButtonImage = clientButton.GetComponent<Image>();
            clientButtonImage.color = new Color(0.2f, 0.4f, 0.8f, 1f);

            // Client Description
            var clientDesc = CreateText(clientButton.transform, "DescText", "Join an existing\ngame session", 20);
            var clientDescRect = clientDesc.GetComponent<RectTransform>();
            clientDescRect.anchoredPosition = new Vector2(0, -40);

            // Create Session List Panel (hidden by default)
            var sessionPanel = CreatePanel(modalPanel.transform, "SessionListPanel", new Vector2(780, 580));
            sessionPanel.SetActive(false);
            var sessionPanelImage = sessionPanel.GetComponent<Image>();
            sessionPanelImage.color = new Color(0, 0, 0, 0);

            // Session List Title
            var sessionTitle = CreateText(sessionPanel.transform, "SessionListTitle", "Available Games", 36);
            var sessionTitleRect = sessionTitle.GetComponent<RectTransform>();
            sessionTitleRect.anchorMin = new Vector2(0.5f, 1f);
            sessionTitleRect.anchorMax = new Vector2(0.5f, 1f);
            sessionTitleRect.anchoredPosition = new Vector2(0, -40);

            // Session List Container
            var sessionContainer = CreatePanel(sessionPanel.transform, "SessionListContainer", new Vector2(700, 350));
            var containerRect = sessionContainer.GetComponent<RectTransform>();
            containerRect.anchoredPosition = new Vector2(0, -30);
            var containerImage = sessionContainer.GetComponent<Image>();
            containerImage.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            // Add Vertical Layout Group
            var layoutGroup = sessionContainer.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 10;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            // No Sessions Text
            var noSessions = CreateText(sessionContainer.transform, "NoSessionsText", "No games found.\nAsk a friend to host a game.", 24);
            noSessions.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

            // Searching Indicator
            var searching = CreateText(sessionPanel.transform, "SearchingIndicator", "Searching...", 20);
            var searchingRect = searching.GetComponent<RectTransform>();
            searchingRect.anchorMin = new Vector2(0.5f, 0f);
            searchingRect.anchorMax = new Vector2(0.5f, 0f);
            searchingRect.anchoredPosition = new Vector2(0, 100);

            // Back Button
            var backButton = CreateButton(sessionPanel.transform, "BackButton", "← Back", new Vector2(-280, 240));
            var backButtonRect = backButton.GetComponent<RectTransform>();
            backButtonRect.sizeDelta = new Vector2(150, 50);

            // Refresh Button
            var refreshButton = CreateButton(sessionPanel.transform, "RefreshButton", "Refresh", new Vector2(280, 240));
            var refreshButtonRect = refreshButton.GetComponent<RectTransform>();
            refreshButtonRect.sizeDelta = new Vector2(150, 50);

            // Create Session Entry Prefab (as child, will be used as template)
            var sessionEntry = CreateButton(sessionContainer.transform, "SessionEntryPrefab", "Session Name", Vector2.zero);
            var entryRect = sessionEntry.GetComponent<RectTransform>();
            entryRect.sizeDelta = new Vector2(680, 60);
            sessionEntry.SetActive(false); // Hide the prefab template

            // Add RoleSelectionModalUI component
            var roleSelectionUI = canvasGO.AddComponent<RoleSelectionModalUI>();

            // Use SerializedObject to set references
            var so = new SerializedObject(roleSelectionUI);
            so.FindProperty("m_modalPanel").objectReferenceValue = modalPanel;
            so.FindProperty("m_canvasGroup").objectReferenceValue = canvasGroup;
            so.FindProperty("m_roleSelectionPanel").objectReferenceValue = rolePanel;
            so.FindProperty("m_titleText").objectReferenceValue = title.GetComponent<TMP_Text>();
            so.FindProperty("m_hostButton").objectReferenceValue = hostButton.GetComponent<Button>();
            so.FindProperty("m_clientButton").objectReferenceValue = clientButton.GetComponent<Button>();
            so.FindProperty("m_sessionListPanel").objectReferenceValue = sessionPanel;
            so.FindProperty("m_sessionListTitle").objectReferenceValue = sessionTitle.GetComponent<TMP_Text>();
            so.FindProperty("m_sessionListContainer").objectReferenceValue = sessionContainer.transform;
            so.FindProperty("m_sessionEntryPrefab").objectReferenceValue = sessionEntry;
            so.FindProperty("m_backButton").objectReferenceValue = backButton.GetComponent<Button>();
            so.FindProperty("m_refreshButton").objectReferenceValue = refreshButton.GetComponent<Button>();
            so.FindProperty("m_noSessionsText").objectReferenceValue = noSessions.GetComponent<TMP_Text>();
            so.FindProperty("m_searchingIndicator").objectReferenceValue = searching;
            so.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Role Selection Canvas");
            Debug.Log("[RoleSelectionSetup] Created RoleSelectionCanvas with UI");
        }

        private static void ConfigureGameStartupManager()
        {
            var startupManager = Object.FindAnyObjectByType<GameStartupManagerMotif>();
            if (startupManager == null)
            {
                Debug.LogWarning("[RoleSelectionSetup] GameStartupManagerMotif not found in scene");
                return;
            }

            var so = new SerializedObject(startupManager);
            
            // Find and assign references
            var roleSelectionUI = Object.FindAnyObjectByType<RoleSelectionModalUI>();
            var sessionDiscovery = Object.FindAnyObjectByType<SessionDiscoveryManager>();

            if (roleSelectionUI != null)
            {
                so.FindProperty("m_roleSelectionUI").objectReferenceValue = roleSelectionUI;
            }

            if (sessionDiscovery != null)
            {
                so.FindProperty("m_sessionDiscovery").objectReferenceValue = sessionDiscovery;
            }

            so.FindProperty("m_requireRoleSelection").boolValue = true;
            so.FindProperty("m_disableAutoMatchmaking").boolValue = true;
            so.ApplyModifiedProperties();

            Debug.Log("[RoleSelectionSetup] Configured GameStartupManagerMotif");
        }

        private static void ConfigureAutoMatchmaking()
        {
            var autoMatchmaking = GameObject.Find("[BuildingBlock] Auto Matchmaking");
            if (autoMatchmaking == null)
            {
                Debug.LogWarning("[RoleSelectionSetup] Auto Matchmaking building block not found");
                return;
            }

            // Note: We don't disable it here - GameStartupManagerMotif will set StartMode to Manual at runtime
            Debug.Log("[RoleSelectionSetup] Auto Matchmaking will be set to Manual mode at runtime");
        }

        #region UI Creation Helpers

        private static GameObject CreatePanel(Transform parent, string name, Vector2 size)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            
            var rect = panel.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            
            var image = panel.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
            
            return panel;
        }

        private static GameObject CreateText(Transform parent, string name, string text, int fontSize)
        {
            var textGO = new GameObject(name);
            textGO.transform.SetParent(parent, false);
            
            var rect = textGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 100);
            
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            
            return textGO;
        }

        private static GameObject CreateButton(Transform parent, string name, string text, Vector2 position)
        {
            var buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent, false);
            
            var rect = buttonGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 80);
            rect.anchoredPosition = position;
            
            var image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.4f, 1f);
            
            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = image;
            
            // Add button text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = new Vector2(0, 20);
            
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            
            return buttonGO;
        }

        #endregion
    }
}
#endif
