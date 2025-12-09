using UnityEditor;
using UnityEngine;

namespace Fusion
{
    /// <summary>
    /// Minimal stub for Fusion Unity Editor assembly to prevent Burst compilation errors
    /// </summary>
    public class FusionUnityEditorStub
    {
        [MenuItem("Fusion Stubs/Info")]
        public static void ShowInfo()
        {
            Debug.Log("Fusion Unity Editor stub loaded - this prevents assembly compilation warnings");
        }
    }
}