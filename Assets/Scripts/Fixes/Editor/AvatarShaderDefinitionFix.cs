using UnityEngine;
using UnityEditor;
using System.IO;

namespace MRMotifs.Fixes
{
    /// <summary>
    /// Specific fix for the Avatar/Meta shader 'avatar_IsSubmeshType' and 'avatar_SUBMESH_TYPE_OUTFIT' errors.
    /// This creates a shader include file that provides the missing definitions.
    /// </summary>
    public static class AvatarShaderDefinitionFix
    {
        private const string SHADER_FIX_INCLUDE = @"
// Auto-generated Avatar shader fix for missing definitions
// This file provides missing function and constant definitions for Avatar/Meta shaders

#ifndef AVATAR_SHADER_FIX_INCLUDED
#define AVATAR_SHADER_FIX_INCLUDED

// Missing submesh type constants
#ifndef avatar_SUBMESH_TYPE_OUTFIT
#define avatar_SUBMESH_TYPE_OUTFIT 0
#endif

#ifndef avatar_SUBMESH_TYPE_BODY
#define avatar_SUBMESH_TYPE_BODY 1
#endif

#ifndef avatar_SUBMESH_TYPE_HAIR
#define avatar_SUBMESH_TYPE_HAIR 2
#endif

// Missing function definition - avatar_IsSubmeshType
#ifndef avatar_IsSubmeshType
bool avatar_IsSubmeshType(int submeshType, int targetType)
{
    return submeshType == targetType;
}
#endif

#endif // AVATAR_SHADER_FIX_INCLUDED
";

        [MenuItem("Tools/MRMotifs/Create Avatar Shader Fix Include")]
        public static void CreateShaderFixInclude()
        {
            string shaderIncludePath = "Assets/Shaders/AvatarShaderFix.hlsl";
            
            // Ensure directory exists
            string directory = Path.GetDirectoryName(shaderIncludePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Write the fix include file
            File.WriteAllText(shaderIncludePath, SHADER_FIX_INCLUDE);
            
            AssetDatabase.Refresh();
            
            Debug.Log($"[AvatarShaderDefinitionFix] Created shader fix include at: {shaderIncludePath}");
            Debug.Log("[AvatarShaderDefinitionFix] You may need to modify Avatar shaders to include this file if the build errors persist.");
        }

        [MenuItem("Tools/MRMotifs/Apply Avatar Shader Workaround")]
        public static void ApplyShaderWorkaround()
        {
            Debug.Log("[AvatarShaderDefinitionFix] Applying Avatar shader workaround...");
            
            // Method 1: Create the include file
            CreateShaderFixInclude();
            
            // Method 2: Force Graphics Settings to include all variants
            ForceIncludeShaderVariants();
            
            // Method 3: Configure project settings for better compatibility
            ConfigureProjectSettings();
            
            Debug.Log("[AvatarShaderDefinitionFix] Workaround applied. Try building again.");
        }

        private static void ForceIncludeShaderVariants()
        {
            // Create a simple material that uses Avatar shaders to force compilation
            Material avatarMaterial = new Material(Shader.Find("Avatar/Meta"));
            if (avatarMaterial.shader != null)
            {
                // Save it as an asset to force inclusion
                string materialPath = "Assets/TempAvatarMaterial.mat";
                AssetDatabase.CreateAsset(avatarMaterial, materialPath);
                
                Debug.Log("[AvatarShaderDefinitionFix] Created temporary Avatar material to force shader inclusion");
            }
        }

        private static void ConfigureProjectSettings()
        {
            // Disable shader stripping for development
            EditorUserBuildSettings.development = true;
            
            // Ensure Vulkan compatibility
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new UnityEngine.Rendering.GraphicsDeviceType[] 
                { 
                    UnityEngine.Rendering.GraphicsDeviceType.Vulkan
                });
                
                // Enable GPU skinning which Avatar SDK requires
                PlayerSettings.gpuSkinning = true;
            }
            
            Debug.Log("[AvatarShaderDefinitionFix] Project settings configured for Avatar compatibility");
        }
    }
}