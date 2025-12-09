using Unity.Netcode;
using UnityEngine;
using MRMotifs.Shooting;

#if UNITY_ANDROID && !UNITY_EDITOR
using Oculus.Avatar2;
#endif

#if META_XR_BUILDING_BLOCKS
using Meta.XR.BuildingBlocks;
#endif

namespace Shooting
{
    /// <summary>
    /// Manages the integration between Meta's Networked Avatar system and the shooting game.
    /// Automatically adds shooting functionality to spawned Meta avatars.
    /// </summary>
    public class MetaAvatarGameIntegration : MonoBehaviour
    {
        [Header("Integration Settings")]
        [SerializeField] private bool m_autoAttachShootingComponents = true;
        [SerializeField] private bool m_enableLocalPlayerHUD = true;
        [SerializeField] private float m_localPlayerAvatarAlpha = 0.3f;
        
        private Component m_networkedAvatarBB;
        
        private void Awake()
        {
            // Find the Networked Avatar Building Block
#if META_XR_BUILDING_BLOCKS
            m_networkedAvatarBB = FindFirstObjectByType<NetworkedAvatarBuildingBlock>();
#else
            m_networkedAvatarBB = GameObject.Find("[BuildingBlock] Networked Avatar")?.GetComponent<Component>();
#endif
            
            if (m_networkedAvatarBB == null)
            {
                Debug.LogError("MetaAvatarGameIntegration: No NetworkedAvatarBuildingBlock found in scene!");
                return;
            }
            
            // Subscribe to avatar spawn events
            // Note: This assumes the NetworkedAvatarBuildingBlock has events or we can hook into NetworkObject spawns
            SetupAvatarSpawnListening();
        }
        
        private void SetupAvatarSpawnListening()
        {
            // Listen for network object spawns to detect avatar spawning
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            }
            else
            {
                // If NetworkManager isn't ready yet, wait for it
                StartCoroutine(WaitForNetworkManager());
            }
        }
        
        private System.Collections.IEnumerator WaitForNetworkManager()
        {
            while (NetworkManager.Singleton == null)
            {
                yield return null;
            }
            
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
        
        private void OnClientConnected(ulong clientId)
        {
            // Start monitoring for avatar spawns
            StartCoroutine(MonitorForAvatarSpawns());
        }
        
        private System.Collections.IEnumerator MonitorForAvatarSpawns()
        {
            // Check for newly spawned avatars every frame for a short period
            float checkDuration = 5f; // Check for 5 seconds after connection
            float elapsed = 0f;
            
            while (elapsed < checkDuration)
            {
                // Find all Meta avatars that don't have the shooting adapter yet
#if UNITY_ANDROID && !UNITY_EDITOR
                var avatars = FindObjectsOfType<Oculus.Avatar2.OvrAvatarEntity>();
#else
                // In editor, look for objects with "Avatar" in the name
                var allObjects = FindObjectsOfType<GameObject>();
                var avatars = System.Array.FindAll(allObjects, obj => obj.name.Contains("Avatar") && obj.GetComponent<NetworkObject>() != null);
#endif
                
                foreach (var avatar in avatars)
                {
#if UNITY_ANDROID && !UNITY_EDITOR
                    var networkObject = avatar.GetComponent<NetworkObject>();
                    var adapter = avatar.GetComponent<MetaAvatarShootingAdapter>();
                    
                    // If this is a networked avatar without the adapter, add it
                    if (networkObject != null && adapter == null && m_autoAttachShootingComponents)
                    {
                        AddShootingComponentsToAvatar(avatar.gameObject);
                    }
#else
                    var networkObject = avatar.GetComponent<NetworkObject>();
                    var adapter = avatar.GetComponent<MetaAvatarShootingAdapter>();
                    
                    // If this is a networked avatar without the adapter, add it
                    if (networkObject != null && adapter == null && m_autoAttachShootingComponents)
                    {
                        AddShootingComponentsToAvatar(avatar);
                    }
#endif
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        
        private void AddShootingComponentsToAvatar(GameObject avatarObject)
        {
            Debug.Log($"Adding shooting components to Meta avatar: {avatarObject.name}");
            
            // Add the shooting adapter
            var adapter = avatarObject.AddComponent<MetaAvatarShootingAdapter>();
            
            // The adapter will handle adding the other components in its OnNetworkSpawn
            
            // Find and configure specific bones for weapon attachment
            ConfigureAvatarBones(avatarObject, adapter);
        }
        
        private void ConfigureAvatarBones(GameObject avatarObject, MetaAvatarShootingAdapter adapter)
        {
            // Try to find specific bones in the Meta avatar hierarchy
            // This is optional - the adapter will fall back to camera rig positions
            
#if UNITY_ANDROID && !UNITY_EDITOR
            var avatarEntity = avatarObject.GetComponent<Oculus.Avatar2.OvrAvatarEntity>();
            if (avatarEntity != null)
            {
                // The AvatarEntity should have bone references
                // This may require specific knowledge of Meta's avatar bone hierarchy
                // For now, we'll let the adapter use camera rig fallbacks
                
                Debug.Log("Avatar entity found, using camera rig for bone positions");
            }
#else
            Debug.Log("Editor mode: using camera rig for bone positions");
#endif
        }
        
        // Public method to manually add shooting components to an avatar
        public void AddShootingToAvatar(GameObject avatarObject)
        {
            if (avatarObject.GetComponent<MetaAvatarShootingAdapter>() == null)
            {
                AddShootingComponentsToAvatar(avatarObject);
            }
        }
        
        // Public method to get all avatars with shooting capabilities
        public MetaAvatarShootingAdapter[] GetShootingAvatars()
        {
            return FindObjectsOfType<MetaAvatarShootingAdapter>();
        }
        
        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            }
        }
        
#if UNITY_EDITOR
        [ContextMenu("Find and Setup Existing Avatars")]
        private void SetupExistingAvatars()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var avatars = FindObjectsOfType<Oculus.Avatar2.OvrAvatarEntity>();
#else
            var allObjects = FindObjectsOfType<GameObject>();
            var avatars = System.Array.FindAll(allObjects, obj => obj.name.Contains("Avatar") && obj.GetComponent<NetworkObject>() != null);
#endif
            Debug.Log($"Found {avatars.Length} existing Meta avatars");
            
            foreach (var avatar in avatars)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (avatar.GetComponent<MetaAvatarShootingAdapter>() == null)
                {
                    AddShootingComponentsToAvatar(avatar.gameObject);
                }
#else
                if (avatar.GetComponent<MetaAvatarShootingAdapter>() == null)
                {
                    AddShootingComponentsToAvatar(avatar);
                }
#endif
            }
        }
#endif
    }
}