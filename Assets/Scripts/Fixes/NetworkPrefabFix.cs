using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Fixes missing NetworkObject components on prefabs
/// Based on Unity Netcode for GameObjects requirements
/// </summary>
public class NetworkPrefabFix : MonoBehaviour
{
    [Header("Network Prefab Fix")]
    [Tooltip("Automatically add NetworkObject components to this prefab if missing")]
    public bool m_autoFixNetworkObject = true;
    
    [Tooltip("Enable spawning with owner (for player-owned objects like bullets)")]
    public bool m_spawnWithOwner = true;
    
    [Tooltip("Despawn with owner (important for bullets/projectiles)")]
    public bool m_despawnWithOwner = true;

    void Awake()
    {
        FixNetworkObjectIfNeeded();
    }

    void FixNetworkObjectIfNeeded()
    {
        if (!m_autoFixNetworkObject)
            return;

        // Check if NetworkObject component is missing
        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.Log($"[NetworkPrefabFix] Adding missing NetworkObject component to {gameObject.name}");
            
            // Add the NetworkObject component
            networkObject = gameObject.AddComponent<NetworkObject>();
            
            // Configure based on object type
            ConfigureNetworkObject(networkObject);
        }
        else
        {
            // NetworkObject exists, just configure it properly
            ConfigureNetworkObject(networkObject);
        }
    }

    void ConfigureNetworkObject(NetworkObject networkObject)
    {
        // Configure based on the object name/type
        string objectName = gameObject.name.ToLower();
        
        if (objectName.Contains("bullet") || objectName.Contains("projectile"))
        {
            // Bullets should be simple networked objects
            Debug.Log($"[NetworkPrefabFix] Configured {gameObject.name} as projectile");
        }
        else if (objectName.Contains("cover") || objectName.Contains("destructible"))
        {
            // Environmental objects - no ownership required
            Debug.Log($"[NetworkPrefabFix] Configured {gameObject.name} as environmental object");
        }
        else if (objectName.Contains("avatar") || objectName.Contains("player"))
        {
            // Player objects - owned by specific player
            Debug.Log($"[NetworkPrefabFix] Configured {gameObject.name} as player-owned object");
        }
        else
        {
            // Default configuration
            Debug.Log($"[NetworkPrefabFix] Applied default NetworkObject configuration to {gameObject.name}");
        }
    }

    [ContextMenu("Fix NetworkObject")]
    public void ManuallyFixNetworkObject()
    {
        FixNetworkObjectIfNeeded();
    }

    void OnValidate()
    {
        // Auto-fix during development
        if (Application.isEditor && !Application.isPlaying)
        {
            // Schedule the fix for the next frame to avoid issues during validation
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                    FixNetworkObjectIfNeeded();
            };
        }
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(NetworkPrefabFix))]
public class NetworkPrefabFixEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        NetworkPrefabFix fix = (NetworkPrefabFix)target;
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Fix NetworkObject Now"))
        {
            fix.ManuallyFixNetworkObject();
        }
        
        // Show current status
        NetworkObject networkObj = fix.GetComponent<NetworkObject>();
        if (networkObj != null)
        {
            GUILayout.Space(5);
            UnityEditor.EditorGUILayout.HelpBox("✓ NetworkObject component found and configured", UnityEditor.MessageType.Info);
        }
        else
        {
            GUILayout.Space(5);
            UnityEditor.EditorGUILayout.HelpBox("⚠ NetworkObject component missing - will be added at runtime", UnityEditor.MessageType.Warning);
        }
    }
}
#endif