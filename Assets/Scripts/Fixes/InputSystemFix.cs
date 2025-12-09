using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace MRMotifs.Fixes
{
    /// <summary>
    /// Fixes Input System compatibility issues by ensuring proper EventSystem setup
    /// </summary>
    public class InputSystemFix : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void FixEventSystemForInputSystem()
        {
            var eventSystem = FindFirstObjectByType<EventSystem>();
            
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
            }
            else
            {
                // EventSystem exists, check if it has the wrong input module
                var standalone = eventSystem.GetComponent<StandaloneInputModule>();
                if (standalone != null)
                {
#if ENABLE_INPUT_SYSTEM
                    // Remove StandaloneInputModule and add InputSystemUIInputModule
                    DestroyImmediate(standalone);
                    if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                    {
                        eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                        Debug.Log("[InputSystemFix] Replaced StandaloneInputModule with InputSystemUIInputModule");
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
                    }
#endif
                }
            }
        }
    }
}