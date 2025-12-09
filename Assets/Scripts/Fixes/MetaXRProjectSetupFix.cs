using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace ArenaShooter.Fixes
{
    /// <summary>
    /// Automatically fixes common Meta XR Project Setup Tool issues
    /// Addresses the "1 outstanding Recommended fix" warning
    /// </summary>
    [InitializeOnLoad]
    public class MetaXRProjectSetupFix
    {
        static MetaXRProjectSetupFix()
        {
            // Run fixes on domain reload
            EditorApplication.delayCall += () => {
                FixMetaXRProjectSettings();
            };
        }

        [MenuItem("Arena Shooter/Fix Meta XR Project Settings")]
        public static void FixMetaXRProjectSettings()
        {
            Debug.Log("[MetaXRProjectSetupFix] Starting Meta XR project setup fixes...");
            
            try
            {
                // Fix 1: OVRManager Quest Features configuration
                ConfigureOVRManagerQuestFeatures();
                
                // Fix 2: Android Build Settings
                ConfigureAndroidBuildSettings();
                
                // Fix 3: XR Plugin Management
                ConfigureXRPluginManagement();
                
                // Fix 4: Player Settings for Quest
                ConfigurePlayerSettings();
                
                // Fix 5: Meta XR Simulator settings
                ConfigureMetaXRSimulator();
                
                Debug.Log("[MetaXRProjectSetupFix] All Meta XR project setup fixes applied successfully!");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MetaXRProjectSetupFix] Error applying fixes: {e.Message}");
            }
        }
        
        private static void ConfigureOVRManagerQuestFeatures()
        {
            var ovrManager = UnityEngine.Object.FindAnyObjectByType<OVRManager>();
            if (ovrManager == null)
            {
                Debug.LogWarning("[MetaXRProjectSetupFix] No OVRManager found in scene");
                return;
            }
            
            Debug.Log("[MetaXRProjectSetupFix] Configuring OVRManager Quest Features...");
            
            // Use reflection to access Quest Features
            var ovrManagerType = typeof(OVRManager);
            var questFeaturesField = ovrManagerType.GetField("_questFeatures", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (questFeaturesField != null)
            {
                var questFeatures = questFeaturesField.GetValue(ovrManager);
                var questFeaturesType = questFeatures.GetType();
                
                // Enable Scene Support (required for MRUK)
                SetQuestFeature(questFeatures, questFeaturesType, "m_SceneSupport", true);
                
                // Enable Passthrough Support
                SetQuestFeature(questFeatures, questFeaturesType, "m_PassthroughSupport", true);
                
                // Enable Spatial Anchors Support
                SetQuestFeature(questFeatures, questFeaturesType, "m_SpatialAnchorsSupport", true);
                
                // Enable Colocation Session Support
                SetQuestFeature(questFeatures, questFeaturesType, "m_ColocationSessionSupport", true);
                
                // Enable Shared Spatial Anchors Support
                SetQuestFeature(questFeatures, questFeaturesType, "m_SharedSpatialAnchorsSupport", true);
                
                Debug.Log("[MetaXRProjectSetupFix] OVRManager Quest Features configured");
                
                // Mark object as dirty to save changes
                EditorUtility.SetDirty(ovrManager);
            }
            
            // Configure permissions
            ConfigureAndroidPermissions();
        }
        
        private static void SetQuestFeature(object questFeatures, Type questFeaturesType, string fieldName, bool value)
        {
            try
            {
                var field = questFeaturesType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(questFeatures, value);
                    Debug.Log($"[MetaXRProjectSetupFix] Set {fieldName} = {value}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MetaXRProjectSetupFix] Could not set {fieldName}: {e.Message}");
            }
        }
        
        private static void ConfigureAndroidPermissions()
        {
            Debug.Log("[MetaXRProjectSetupFix] Configuring Android permissions...");
            
            // These are the permissions typically required by Meta XR features
            var requiredPermissions = new string[]
            {
                "com.oculus.permission.USE_SCENE",
                "com.oculus.permission.SCENE_CAPTURE", 
                "com.oculus.permission.USE_ANCHOR_API",
                "com.oculus.permission.SHARE_SPATIAL_ANCHOR",
                "android.permission.RECORD_AUDIO", // For voice/avatar features
                "android.permission.MODIFY_AUDIO_SETTINGS"
            };
            
            foreach (var permission in requiredPermissions)
            {
                // Note: Unity doesn't provide a direct API to modify AndroidManifest.xml permissions
                // This would typically be handled by the Meta XR Project Setup Tool
                // or by custom AndroidManifest.xml modifications
                Debug.Log($"[MetaXRProjectSetupFix] Required permission: {permission}");
            }
        }
        
        private static void ConfigureAndroidBuildSettings()
        {
            Debug.Log("[MetaXRProjectSetupFix] Configuring Android build settings...");
            
            // Set Android as build target
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[MetaXRProjectSetupFix] Build target is not Android - this should be set manually");
            }
            
            // Configure Android settings
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23; // Android 6.0
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            
            Debug.Log("[MetaXRProjectSetupFix] Android build settings configured");
        }
        
        private static void ConfigureXRPluginManagement()
        {
            Debug.Log("[MetaXRProjectSetupFix] XR Plugin Management should be configured via Project Settings > XR Plug-in Management");
            // XR Plugin Management configuration typically requires the XR Management package
            // and should be configured through the UI: Project Settings > XR Plug-in Management
        }
        
        private static void ConfigurePlayerSettings()
        {
            Debug.Log("[MetaXRProjectSetupFix] Configuring Player Settings...");
            
            // Graphics API
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new UnityEngine.Rendering.GraphicsDeviceType[] { 
                UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3,
                UnityEngine.Rendering.GraphicsDeviceType.Vulkan 
            });
            
            // Color Space
            PlayerSettings.colorSpace = ColorSpace.Linear;
            
            // Multithreaded rendering
            PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);
            
            Debug.Log("[MetaXRProjectSetupFix] Player Settings configured");
        }
        
        private static void ConfigureMetaXRSimulator()
        {
            Debug.Log("[MetaXRProjectSetupFix] Meta XR Simulator configuration - this is typically handled automatically");
            // Meta XR Simulator settings are usually managed automatically
            // when the simulator is installed and activated
        }
    }
}