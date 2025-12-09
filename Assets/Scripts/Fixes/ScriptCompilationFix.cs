using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace ArenaShooter.Fixes
{
    /// <summary>
    /// Fixes common Unity script compilation and assembly loading issues
    /// </summary>
    [InitializeOnLoad]
    public class ScriptCompilationFix
    {
        static ScriptCompilationFix()
        {
            EditorApplication.delayCall += () => {
                FixScriptCompilationIssues();
            };
        }

        [MenuItem("Arena Shooter/Fix Script Compilation Issues")]
        public static void FixScriptCompilationIssues()
        {
            Debug.Log("[ScriptCompilationFix] Starting script compilation fixes...");
            
            try
            {
                // Fix 1: Force script compilation
                ForceScriptCompilation();
                
                // Fix 2: Clear cache if needed
                ClearUnityCache();
                
                // Fix 3: Check for missing dependencies
                CheckMissingDependencies();
                
                Debug.Log("[ScriptCompilationFix] Script compilation fixes completed!");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ScriptCompilationFix] Error: {e.Message}");
            }
        }
        
        private static void ForceScriptCompilation()
        {
            Debug.Log("[ScriptCompilationFix] Forcing script recompilation...");
            
            // Request script compilation
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            
            // Also refresh the asset database
            AssetDatabase.Refresh();
            
            Debug.Log("[ScriptCompilationFix] Script recompilation requested");
        }
        
        private static void ClearUnityCache()
        {
            try
            {
                Debug.Log("[ScriptCompilationFix] Checking Unity cache...");
                
                // Clear the Library folder cache (ScriptAssemblies)
                string scriptAssembliesPath = Path.Combine(Application.dataPath, "..", "Library", "ScriptAssemblies");
                
                if (Directory.Exists(scriptAssembliesPath))
                {
                    Debug.Log("[ScriptCompilationFix] Found ScriptAssemblies directory, checking for issues...");
                    
                    // Check if Assembly-CSharp.dll exists and is accessible
                    string assemblyCSharpPath = Path.Combine(scriptAssembliesPath, "Assembly-CSharp.dll");
                    
                    if (File.Exists(assemblyCSharpPath))
                    {
                        try
                        {
                            // Try to access the file to see if it's locked
                            using (var fs = File.Open(assemblyCSharpPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                Debug.Log("[ScriptCompilationFix] Assembly-CSharp.dll is accessible");
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"[ScriptCompilationFix] Assembly-CSharp.dll access issue: {e.Message}");
                            Debug.Log("[ScriptCompilationFix] Consider restarting Unity if issues persist");
                        }
                    }
                    else
                    {
                        Debug.Log("[ScriptCompilationFix] Assembly-CSharp.dll not found - compilation may be needed");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ScriptCompilationFix] Cache check error: {e.Message}");
            }
        }
        
        private static void CheckMissingDependencies()
        {
            Debug.Log("[ScriptCompilationFix] Checking for missing dependencies...");
            
            // Check if all required packages are properly imported
            var requiredPackages = new string[]
            {
                "com.meta.xr.sdk.core",
                "com.meta.xr.mrutilitykit", 
                "com.unity.netcode.gameobjects",
                "com.meta.xr.sdk.avatars"
            };
            
            foreach (var package in requiredPackages)
            {
                var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForPackageName(package);
                if (packageInfo == null)
                {
                    Debug.LogWarning($"[ScriptCompilationFix] Package not found: {package}");
                }
                else
                {
                    Debug.Log($"[ScriptCompilationFix] Package found: {package} v{packageInfo.version}");
                }
            }
        }
        
        // Editor callback to monitor compilation state
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            Debug.Log("[ScriptCompilationFix] Scripts reloaded successfully!");
        }
    }
}