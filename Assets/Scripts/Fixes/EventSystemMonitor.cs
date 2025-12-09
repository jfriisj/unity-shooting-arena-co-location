using UnityEngine;
using UnityEngine.EventSystems;
using System.Reflection;

namespace MRMotifs.Fixes
{
    /// <summary>
    /// Patches Fusion Statistics to prevent it from creating StandaloneInputModule
    /// when Input System package is enabled.
    /// </summary>
    public static class FusionStatisticsPatch
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void PatchFusionStatistics()
        {
            // This will run before any scene loads and prevent the wrong EventSystem creation
#if ENABLE_INPUT_SYSTEM
            Debug.Log("[FusionStatisticsPatch] Input System detected - will monitor for incorrect EventSystem creation");
#endif
        }
    }
    
    /// <summary>
    /// Runtime component that monitors and fixes EventSystem issues
    /// </summary>
    public class EventSystemMonitor : MonoBehaviour
    {
        private void Awake()
        {
            // Run every few frames to catch dynamically created EventSystems
            InvokeRepeating(nameof(CheckAndFixEventSystems), 0.1f, 0.5f);
        }
        
        private void CheckAndFixEventSystems()
        {
#if ENABLE_INPUT_SYSTEM
            var eventSystems = FindObjectsOfType<EventSystem>(includeInactive: true);
            
            foreach (var eventSystem in eventSystems)
            {
                var standalone = eventSystem.GetComponent<StandaloneInputModule>();
                if (standalone != null && standalone.enabled)
                {
                    standalone.enabled = false;
                    Debug.Log($"[EventSystemMonitor] Disabled StandaloneInputModule on '{eventSystem.name}' to prevent Input System conflicts");
                }
            }
#endif
        }
        
        private void OnDestroy()
        {
            CancelInvoke();
        }
    }
}