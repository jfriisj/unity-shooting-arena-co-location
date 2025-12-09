using UnityEngine;

namespace ArenaDrone.Fixes
{
    /// <summary>
    /// Suppresses common OVR interaction profile warnings that don't affect functionality
    /// These warnings occur when action sets aren't attached but are expected during initialization
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class OVRInteractionProfileFix : MonoBehaviour
    {
        private void Awake()
        {
            // Register for log callbacks to filter out expected warnings
            Application.logMessageReceived += OnLogMessageReceived;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            // Filter out the expected OVR interaction profile warnings
            if (type == LogType.Warning && condition.Contains("[OVRPlugin]") && 
                condition.Contains("[XR_ERROR_ACTIONSET_NOT_ATTACHED]: xrGetCurrentInteractionProfile"))
            {
                // This is an expected warning during initialization - suppress it
                return;
            }
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
        }
    }
}