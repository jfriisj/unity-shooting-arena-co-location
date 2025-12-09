using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace MRMotifs.Fixes
{
    /// <summary>
    /// Build preprocessor that fixes Avatar shader compilation issues
    /// by configuring proper settings before the build process.
    /// </summary>
    public class AvatarBuildFix : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        private bool m_originalShaderStrippingEnabled;
        private bool m_originalLightmapStrippingEnabled;

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("[AvatarBuildFix] Configuring build settings to fix Avatar shader issues...");

            // Store original settings
            m_originalShaderStrippingEnabled = EditorUserBuildSettings.development;
            
            // Configure settings to avoid Avatar shader compilation issues
            ConfigureBuildSettings();
            
            // Force Avatar shaders to compile with all necessary variants
            ForceAvatarShaderCompilation();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            Debug.Log("[AvatarBuildFix] Restoring original build settings...");
            
            // Restore original settings after build
            EditorUserBuildSettings.development = m_originalShaderStrippingEnabled;
        }

        private void ConfigureBuildSettings()
        {
            // Enable development build to include more shader variants
            EditorUserBuildSettings.development = true;
            
            // Configure Graphics Settings to include Avatar shaders
            var graphicsSettingsObj = AssetDatabase.LoadAssetAtPath("ProjectSettings/GraphicsSettings.asset", typeof(UnityEngine.Object));
            
            // Disable shader stripping that might remove Avatar shader variants
            PlayerSettings.stripEngineCode = false;
            
            // Ensure proper Android settings for Avatar rendering
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
                PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
                
                // Graphics API settings that work better with Avatar SDK
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new UnityEngine.Rendering.GraphicsDeviceType[] 
                { 
                    UnityEngine.Rendering.GraphicsDeviceType.Vulkan,
                    UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3
                });
            }
            
            Debug.Log("[AvatarBuildFix] Build settings configured for Avatar compatibility");
        }

        private void ForceAvatarShaderCompilation()
        {
            // Find and warm up all Avatar shaders
            string[] avatarShaderGuids = AssetDatabase.FindAssets("t:Shader Avatar");
            
            foreach (string guid in avatarShaderGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                
                if (shader != null && shader.name.Contains("Avatar"))
                {
                    Debug.Log($"[AvatarBuildFix] Pre-warming shader: {shader.name}");
                    
                    // Force the shader to be included in build by marking it as used
                    ShaderUtil.RegisterShader(shader);
                }
            }
        }

        [MenuItem("Tools/MRMotifs/Test Avatar Shader Fix")]
        public static void TestAvatarShaderFix()
        {
            var fix = new AvatarBuildFix();
            fix.ConfigureBuildSettings();
            fix.ForceAvatarShaderCompilation();
            
            Debug.Log("[AvatarBuildFix] Test completed. Check console for any remaining shader issues.");
        }
    }
}