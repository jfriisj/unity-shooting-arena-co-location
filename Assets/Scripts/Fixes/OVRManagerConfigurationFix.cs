using UnityEngine;
using System.Collections;

/// <summary>
/// Comprehensive fix for OVRManager settings to enable proper MRUK and Colocation support in Unity Editor
/// Based on Meta Quest documentation:
/// - https://developers.meta.com/horizon/llmstxt/documentation/unity/unity-mr-utility-kit-gs.md
/// - https://developers.meta.com/horizon/llmstxt/documentation/unity/unity-colocation-discovery.md
/// </summary>
public class OVRManagerConfigurationFix : MonoBehaviour
{
    [Header("OVR Manager Configuration")]
    [Tooltip("Enable Scene Support for MRUK")]
    public bool m_enableSceneSupport = true;
    
    [Tooltip("Enable Passthrough Support")]
    public bool m_enablePassthroughSupport = true;
    
    [Tooltip("Enable Colocation Session Support")]
    public bool m_enableColocationSupport = true;
    
    [Header("Permission Settings")]
    [Tooltip("Request Scene permission on startup")]
    public bool m_requestScenePermission = true;
    
    [Tooltip("Request Passthrough permission on startup")]
    public bool m_requestPassthroughPermission = true;

    void Awake()
    {
        StartCoroutine(ConfigureOVRManagerAsync());
    }

    IEnumerator ConfigureOVRManagerAsync()
    {
        // Wait a frame to ensure OVRManager is initialized
        yield return null;
        
        OVRManager ovrManager = FindOVRManager();
        if (ovrManager == null)
        {
            Debug.LogError("[OVRManagerConfigurationFix] OVRManager not found! Please ensure OVRCameraRig is in the scene.");
            yield break;
        }

        Debug.Log("[OVRManagerConfigurationFix] Configuring OVRManager for MRUK and Colocation support");
        
        ConfigureQuestFeatures(ovrManager);
        ConfigurePermissions(ovrManager);
        ConfigurePassthrough(ovrManager);
        
        Debug.Log("[OVRManagerConfigurationFix] OVRManager configuration completed");
    }

    OVRManager FindOVRManager()
    {
        // First try to find OVRManager in the scene
        OVRManager ovrManager = FindObjectOfType<OVRManager>();
        
        if (ovrManager == null)
        {
            Debug.Log("[OVRManagerConfigurationFix] OVRManager not found, checking OVRCameraRig...");
            
            // Try to find it on OVRCameraRig
            OVRCameraRig cameraRig = FindObjectOfType<OVRCameraRig>();
            if (cameraRig != null)
            {
                ovrManager = cameraRig.GetComponent<OVRManager>();
            }
        }
        
        return ovrManager;
    }

    void ConfigureQuestFeatures(OVRManager ovrManager)
    {
        try
        {
            // Enable Scene Support for MRUK
            if (m_enableSceneSupport)
            {
                var questFeaturesProperty = typeof(OVRManager).GetProperty("questFeatures");
                if (questFeaturesProperty != null)
                {
                    var questFeatures = questFeaturesProperty.GetValue(ovrManager);
                    if (questFeatures != null)
                    {
                        // Use reflection to enable scene support
                        var sceneSupportField = questFeatures.GetType().GetField("sceneSupport");
                        if (sceneSupportField != null)
                        {
                            sceneSupportField.SetValue(questFeatures, true);
                            Debug.Log("[OVRManagerConfigurationFix] ✓ Scene Support enabled");
                        }
                        
                        // Enable passthrough support
                        if (m_enablePassthroughSupport)
                        {
                            var passthroughSupportField = questFeatures.GetType().GetField("passthroughSupport");
                            if (passthroughSupportField != null)
                            {
                                passthroughSupportField.SetValue(questFeatures, true);
                                Debug.Log("[OVRManagerConfigurationFix] ✓ Passthrough Support enabled");
                            }
                        }
                        
                        // Enable colocation session support
                        if (m_enableColocationSupport)
                        {
                            var colocationField = questFeatures.GetType().GetField("colocationSessionSupport");
                            if (colocationField != null)
                            {
                                colocationField.SetValue(questFeatures, true);
                                Debug.Log("[OVRManagerConfigurationFix] ✓ Colocation Session Support enabled");
                            }
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[OVRManagerConfigurationFix] Could not configure Quest Features via reflection: {ex.Message}");
            Debug.Log("[OVRManagerConfigurationFix] Please manually enable Scene Support, Passthrough Support, and Colocation Session Support in OVRManager > Quest Features");
        }
    }

    void ConfigurePermissions(OVRManager ovrManager)
    {
        try
        {
            // Configure permission requests
            var permissionRequestsProperty = typeof(OVRManager).GetProperty("permissionRequestsOnStartup");
            if (permissionRequestsProperty != null)
            {
                var permissionRequests = permissionRequestsProperty.GetValue(ovrManager);
                if (permissionRequests != null)
                {
                    if (m_requestScenePermission)
                    {
                        var sceneField = permissionRequests.GetType().GetField("scene");
                        if (sceneField != null)
                        {
                            sceneField.SetValue(permissionRequests, true);
                            Debug.Log("[OVRManagerConfigurationFix] ✓ Scene permission request enabled");
                        }
                    }
                    
                    if (m_requestPassthroughPermission)
                    {
                        var passthroughField = permissionRequests.GetType().GetField("enablePassthrough");
                        if (passthroughField != null)
                        {
                            passthroughField.SetValue(permissionRequests, true);
                            Debug.Log("[OVRManagerConfigurationFix] ✓ Passthrough permission request enabled");
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[OVRManagerConfigurationFix] Could not configure permissions via reflection: {ex.Message}");
            Debug.Log("[OVRManagerConfigurationFix] Please manually enable Scene and Passthrough permissions in OVRManager > Permission Requests On Startup");
        }
    }

    void ConfigurePassthrough(OVRManager ovrManager)
    {
        // Enable passthrough for Mixed Reality
        if (m_enablePassthroughSupport && Application.isEditor)
        {
            // In Unity Editor, we need to ensure passthrough is ready for testing
            var passthroughLayer = FindObjectOfType<OVRPassthroughLayer>();
            if (passthroughLayer != null)
            {
                passthroughLayer.enabled = true;
                Debug.Log("[OVRManagerConfigurationFix] ✓ Passthrough layer enabled for Unity Editor");
            }
        }
    }

    // Helper method for manual configuration
    [ContextMenu("Configure OVRManager Now")]
    public void ManuallyConfigureOVRManager()
    {
        StartCoroutine(ConfigureOVRManagerAsync());
    }

    void OnValidate()
    {
        // Provide helpful information in the inspector
        if (!m_enableSceneSupport && Application.isEditor)
        {
            Debug.LogWarning("[OVRManagerConfigurationFix] Scene Support is disabled. MRUK features will not work properly.");
        }
        
        if (!m_enableColocationSupport && Application.isEditor)
        {
            Debug.LogWarning("[OVRManagerConfigurationFix] Colocation Support is disabled. Multiplayer alignment features will not work properly.");
        }
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(OVRManagerConfigurationFix))]
public class OVRManagerConfigurationFixEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        OVRManagerConfigurationFix fix = (OVRManagerConfigurationFix)target;
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Configure OVRManager Now"))
        {
            fix.ManuallyConfigureOVRManager();
        }
        
        GUILayout.Space(10);
        
        // Show helpful information
        UnityEditor.EditorGUILayout.HelpBox(
            "This script automatically configures OVRManager settings required for MRUK and Colocation Discovery according to Meta's documentation:\n\n" +
            "• Scene Support - Required for MRUK room scanning\n" +
            "• Passthrough Support - Required for Mixed Reality\n" +
            "• Colocation Session Support - Required for multiplayer alignment\n" +
            "• Permission Requests - Required for runtime access", 
            UnityEditor.MessageType.Info);
        
        // Show current OVRManager status
        OVRManager ovrManager = FindObjectOfType<OVRManager>();
        if (ovrManager != null)
        {
            UnityEditor.EditorGUILayout.HelpBox("✓ OVRManager found in scene", UnityEditor.MessageType.Info);
        }
        else
        {
            UnityEditor.EditorGUILayout.HelpBox("⚠ OVRManager not found. Please ensure OVRCameraRig is in the scene.", UnityEditor.MessageType.Warning);
        }
    }
}
#endif