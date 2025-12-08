using Unity.Netcode;
using UnityEngine;

namespace MRMotifs.Shooting.Avatars
{
    public class SimpleAvatarSpawner : NetworkBehaviour
    {
        [SerializeField] private NetworkObject m_avatarPrefab;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Spawn avatar for the host
                SpawnAvatar(NetworkManager.Singleton.LocalClientId);
                
                // Subscribe to client connection events
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            SpawnAvatar(clientId);
        }

        private void SpawnAvatar(ulong clientId)
        {
            if (m_avatarPrefab == null)
            {
                Debug.LogError("Avatar Prefab is not assigned in SimpleAvatarSpawner!");
                return;
            }

            // Instantiate the avatar
            // For colocation, we spawn at origin (0,0,0) because the camera rig will be aligned to the shared anchor
            // which effectively places the avatar in the correct physical location relative to the shared space.
            var avatarInstance = Instantiate(m_avatarPrefab, Vector3.zero, Quaternion.identity);
            
            // Spawn it on the network and give ownership to the client
            avatarInstance.SpawnAsPlayerObject(clientId, true);
            
            Debug.Log($"Spawned avatar for client {clientId}");
        }
    }
}
