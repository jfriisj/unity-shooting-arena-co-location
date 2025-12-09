using UnityEngine;

namespace ArenaShooter.Fixes
{
    /// <summary>
    /// Runtime component that applies all project fixes when the scene starts
    /// </summary>
    public class ProjectFixesRunner : MonoBehaviour
    {
        [Header("Fix Settings")]
        [SerializeField] private bool runMetaXRFixes = true;
        [SerializeField] private bool runScriptCompilationFixes = true;
        [SerializeField] private bool runURPFixes = true;
        [SerializeField] private bool runOnAwake = true;
        
        private void Awake()
        {
            if (runOnAwake)
            {
                RunAllFixes();
            }
        }
        
        [ContextMenu("Run All Fixes")]
        public void RunAllFixes()
        {
            Debug.Log("[ProjectFixesRunner] Running all project fixes...");
            
            if (runMetaXRFixes)
            {
#if UNITY_EDITOR
                MetaXRProjectSetupFix.FixMetaXRProjectSettings();
#endif
            }
            
            if (runScriptCompilationFixes)
            {
#if UNITY_EDITOR
                ScriptCompilationFix.FixScriptCompilationIssues();
#endif
            }
            
            if (runURPFixes)
            {
#if UNITY_EDITOR
                URPRenderGraphFix.FixURPRenderGraphSettings();
#endif
            }
            
            Debug.Log("[ProjectFixesRunner] All project fixes completed!");
        }
    }
}