using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;

namespace MRMotifs.Shooting
{
    /// <summary>
    /// Synchronizes the player's networked representation with their physical HMD.
    /// Handles local/remote visual separation (hiding head for local player).
    /// </summary>
    [RequireComponent(typeof(NetworkTransform))]
    public class PlayerRigMotif : NetworkBehaviour
    {
        [Header("Visuals")]
        [Tooltip("The visual representation of the player's head (mesh + collider).")]
        [SerializeField] private GameObject m_headVisuals;

        [Tooltip("Offset from the camera position to the visual center.")]
        [SerializeField] private Vector3 m_headOffset = Vector3.zero;

        private Transform m_cameraTransform;

        private void Awake()
        {
            // Find the main camera (HMD)
            if (Camera.main != null)
            {
                m_cameraTransform = Camera.main.transform;
            }
            else
            {
                Debug.LogWarning("[PlayerRigMotif] Main Camera not found in scene!");
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                // Local Player: Hide visuals to prevent seeing inside own head
                // We disable the Renderer but keep the Collider active so we can still be hit
                if (m_headVisuals != null)
                {
                    var renderers = m_headVisuals.GetComponentsInChildren<Renderer>();
                    foreach (var r in renderers) r.enabled = false;
                }
            }
            else
            {
                // Remote Player: Ensure visuals are enabled
                if (m_headVisuals != null)
                {
                    m_headVisuals.SetActive(true);
                    var renderers = m_headVisuals.GetComponentsInChildren<Renderer>();
                    foreach (var r in renderers) r.enabled = true;
                }
            }
        }

        private void Update()
        {
            // Only the owner updates the position
            if (IsOwner && m_cameraTransform != null)
            {
                // Sync root position/rotation to HMD
                transform.position = m_cameraTransform.position + m_headOffset;
                transform.rotation = m_cameraTransform.rotation;
            }
        }
    }
}
