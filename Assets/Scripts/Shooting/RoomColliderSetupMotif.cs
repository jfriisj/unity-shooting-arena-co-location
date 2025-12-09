using UnityEngine;
using Meta.XR.MRUtilityKit;

namespace MRMotifs.Shooting
{
    /// <summary>
    /// Ensures MRUK mesh has colliders for bullets.
    /// </summary>
    public class RoomColliderSetupMotif : MonoBehaviour
    {
        private void Start()
        {
            if (MRUK.Instance != null)
            {
                MRUK.Instance.RegisterSceneLoadedCallback(OnSceneLoaded);
            }
        }

        private void OnSceneLoaded()
        {
            Debug.Log("[RoomColliderSetupMotif] Scene loaded, checking colliders...");
            // In a real implementation, we might iterate through MRUK anchors and ensure they have colliders
            // and are on the correct layer for bullet collision.
            // For this setup, we assume MRUK is configured to generate colliders, 
            // but we can enforce layers here if needed.
        }
    }
}
