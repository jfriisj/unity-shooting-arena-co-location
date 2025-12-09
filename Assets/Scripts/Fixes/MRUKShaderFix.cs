using UnityEngine;
using System.Collections;
using System.Reflection;

namespace ArenaDrone.Fixes
{
    /// <summary>
    /// Fixes shader compatibility issues between Meta MRUK and URP
    /// Prevents "State comes from an incompatible keyword space" errors
    /// when ImmersiveSceneDebugger tries to create materials with Meta/Lit shader
    /// </summary>
    [DefaultExecutionOrder(-1000)] // Execute before MRUK components
    public class MRUKShaderFix : MonoBehaviour
    {
        private static bool s_isInitialized = false;

        private void Awake()
        {
            if (s_isInitialized) return;
            s_isInitialized = true;

            // Start coroutine to patch MRUK components after they're created
            StartCoroutine(PatchMRUKComponents());
        }

        private IEnumerator PatchMRUKComponents()
        {
            // Wait a few frames for MRUK components to initialize
            yield return new WaitForFrames(3);

            // Find and patch ImmersiveSceneDebugger instances
            var debuggers = FindObjectsOfType<MonoBehaviour>();
            foreach (var debugger in debuggers)
            {
                if (debugger.GetType().Name.Contains("ImmersiveSceneDebugger"))
                {
                    PatchImmersiveSceneDebugger(debugger);
                }
            }

            Debug.Log("[MRUKShaderFix] MRUK shader compatibility patches applied");
        }

        private void PatchImmersiveSceneDebugger(MonoBehaviour debugger)
        {
            try
            {
                // Use reflection to access the private field that holds the problematic material
                var type = debugger.GetType();
                var materialField = type.GetField("_checkerMaterial", BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (materialField != null)
                {
                    // Create a URP-compatible replacement material
                    var urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
                    if (urpLitShader != null)
                    {
                        var compatibleMaterial = new Material(urpLitShader);
                        compatibleMaterial.name = "MRUK_CompatibleChecker";
                        compatibleMaterial.SetColor("_BaseColor", Color.white);
                        compatibleMaterial.SetFloat("_Metallic", 0f);
                        compatibleMaterial.SetFloat("_Smoothness", 0.5f);
                        
                        // Replace the problematic material
                        materialField.SetValue(debugger, compatibleMaterial);
                        
                        Debug.Log($"[MRUKShaderFix] Patched {debugger.name} with URP-compatible material");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MRUKShaderFix] Could not patch ImmersiveSceneDebugger: {e.Message}");
            }
        }

        // Helper method to wait for multiple frames
        private class WaitForFrames : CustomYieldInstruction
        {
            private int frameCount;
            private int targetFrameCount;

            public WaitForFrames(int frames)
            {
                targetFrameCount = frames;
                frameCount = 0;
            }

            public override bool keepWaiting
            {
                get
                {
                    return ++frameCount < targetFrameCount;
                }
            }
        }
    }

    /// <summary>
    /// Runtime patch for ImmersiveSceneDebugger to use URP-compatible materials
    /// </summary>
    public static class ImmersiveSceneDebuggerPatch
    {
        /// <summary>
        /// Creates a URP-compatible checker material instead of using Meta/Lit shader
        /// </summary>
        public static Material CreateCompatibleCheckerMaterial()
        {
            var urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null)
            {
                Debug.LogError("[MRUKShaderFix] Universal Render Pipeline/Lit shader not found");
                return null;
            }

            var material = new Material(urpLitShader);
            
            // Set up basic checker pattern properties
            material.name = "MRUK_CompatibleChecker";
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.5f);
            
            return material;
        }
    }
}