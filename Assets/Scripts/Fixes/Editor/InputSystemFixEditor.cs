using UnityEngine;
using UnityEngine.EventSystems;
using UnityEditor;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace MRMotifs.Editor
{
    /// <summary>
    /// Editor utility to fix Input System EventSystem issues
    /// </summary>
    public static class InputSystemFixEditor
    {
        [MenuItem("MR Motifs/Fix Input System EventSystem")]
        public static void FixEventSystemForInputSystem()
        {
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            
            if (eventSystem == null)
            {
                // No EventSystem found, create one with Input System support
                var eventSystemGO = new GameObject("EventSystem (Input System)");
                eventSystemGO.AddComponent<EventSystem>();
                
#if ENABLE_INPUT_SYSTEM
                eventSystemGO.AddComponent<InputSystemUIInputModule>();
                Debug.Log("[InputSystemFix] Created EventSystem with InputSystemUIInputModule");
#else
                eventSystemGO.AddComponent<StandaloneInputModule>();
                Debug.Log("[InputSystemFix] Created EventSystem with StandaloneInputModule (Legacy)");
#endif
                
                // Mark scene dirty so it saves
                EditorUtility.SetDirty(eventSystemGO);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
            else
            {
                // EventSystem exists, check if it has the wrong input module
                var standalone = eventSystem.GetComponent<StandaloneInputModule>();
                if (standalone != null)
                {
#if ENABLE_INPUT_SYSTEM
                    // Remove StandaloneInputModule and add InputSystemUIInputModule
                    Object.DestroyImmediate(standalone);
                    if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                    {
                        eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                        Debug.Log("[InputSystemFix] Replaced StandaloneInputModule with InputSystemUIInputModule");
                        EditorUtility.SetDirty(eventSystem.gameObject);
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                    }
#endif
                }
                else
                {
#if ENABLE_INPUT_SYSTEM
                    // Check if InputSystemUIInputModule is present
                    if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                    {
                        eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                        Debug.Log("[InputSystemFix] Added InputSystemUIInputModule to existing EventSystem");
                        EditorUtility.SetDirty(eventSystem.gameObject);
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                    }
                    else
                    {
                        Debug.Log("[InputSystemFix] EventSystem already has InputSystemUIInputModule - no changes needed");
                    }
#endif
                }
            }
        }
        
        [MenuItem("MR Motifs/Debug/Show Current Input Modules")]
        public static void ShowCurrentInputModules()
        {
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            
            if (eventSystem == null)
            {
                Debug.Log("[InputSystemFix] No EventSystem found in scene");
                return;
            }
            
            var modules = eventSystem.GetComponents<BaseInputModule>();
            Debug.Log($"[InputSystemFix] EventSystem '{eventSystem.name}' has {modules.Length} input modules:");
            
            foreach (var module in modules)
            {
                Debug.Log($"  - {module.GetType().Name} (enabled: {module.enabled})");
            }
        }
    }
}