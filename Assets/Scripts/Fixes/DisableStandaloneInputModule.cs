using UnityEngine;
using UnityEngine.EventSystems;

namespace MRMotifs.Fixes
{
    /// <summary>
    /// Simple fix for the Input System error spam by disabling StandaloneInputModule
    /// when Input System package is active.
    /// </summary>
    [DefaultExecutionOrder(-1000)] // Execute early
    public class DisableStandaloneInputModule : MonoBehaviour
    {
        private void Awake()
        {
            // Find all StandaloneInputModule components and disable them
            var standaloneModules = FindObjectsOfType<StandaloneInputModule>(includeInactive: true);
            
            foreach (var module in standaloneModules)
            {
                // Only disable if Input System is active
#if ENABLE_INPUT_SYSTEM
                module.enabled = false;
                Debug.Log($"[DisableStandaloneInputModule] Disabled StandaloneInputModule on {module.name} to prevent Input System conflicts");
#endif
            }
        }
        
        private void Start()
        {
            // Double-check during Start in case new ones were created
            var standaloneModules = FindObjectsOfType<StandaloneInputModule>(includeInactive: true);
            
            foreach (var module in standaloneModules)
            {
#if ENABLE_INPUT_SYSTEM
                if (module.enabled)
                {
                    module.enabled = false;
                    Debug.Log($"[DisableStandaloneInputModule] Disabled late-created StandaloneInputModule on {module.name}");
                }
#endif
            }
        }
    }
}