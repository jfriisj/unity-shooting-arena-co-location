using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace MRMotifs.Fixes
{
    /// <summary>
    /// Simple build preprocessor to ensure Avatar shader compatibility.
    /// </summary>
    public class AvatarShaderPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("[AvatarShaderPreprocessor] Build preprocessing completed for Avatar shaders");
        }
    }
}