using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

namespace Shooting
{
    /// <summary>
    /// Handles avatar head/hand tracking synchronization across the network for co-located players.
    /// Based on MR Motifs example: AvatarMovementHandlerMotif.cs
    /// </summary>
    public class AvatarMovementHandlerMotif : NetworkBehaviour
    {
        [Header("Avatar Body Parts")]
        [SerializeField] private Transform m_headTransform;
        [SerializeField] private Transform m_leftHandTransform;
        [SerializeField] private Transform m_rightHandTransform;
        
        [Header("Tracking Settings")]
        [SerializeField] private float m_smoothingSpeed = 10f;
        [SerializeField] private bool m_trackHeadRotation = true;
        [SerializeField] private bool m_trackHandRotations = true;
        
        // Networked head tracking data
        [System.Serializable]
        public struct NetworkedPose : INetworkSerializable
        {
            public Vector3 position;
            public Quaternion rotation;
            
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref position);
                serializer.SerializeValue(ref rotation);
            }
        }
        
        private NetworkVariable<NetworkedPose> m_headPose = new NetworkVariable<NetworkedPose>();
        private NetworkVariable<NetworkedPose> m_leftHandPose = new NetworkVariable<NetworkedPose>();
        private NetworkVariable<NetworkedPose> m_rightHandPose = new NetworkVariable<NetworkedPose>();
        
        private OVRCameraRig m_cameraRig;
        private Transform m_centerEyeAnchor;
        private Transform m_leftHandAnchor;
        private Transform m_rightHandAnchor;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // Find the OVR Camera Rig for tracking
            m_cameraRig = FindFirstObjectByType<OVRCameraRig>();
            if (m_cameraRig != null)
            {
                m_centerEyeAnchor = m_cameraRig.centerEyeAnchor;
                m_leftHandAnchor = m_cameraRig.leftHandAnchor;
                m_rightHandAnchor = m_cameraRig.rightHandAnchor;
            }
            
            // Create body parts if they don't exist
            CreateAvatarBodyParts();
            
            // Only the owner should send tracking data
            if (!IsOwner)
            {
                enabled = false;
            }
        }
        
        private void CreateAvatarBodyParts()
        {
            if (m_headTransform == null)
            {
                // Create head representation
                GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.name = "Head";
                head.transform.SetParent(transform);
                head.transform.localScale = Vector3.one * 0.25f;
                m_headTransform = head.transform;
                
                // Make it slightly transparent for local player
                if (IsOwner)
                {
                    var renderer = head.GetComponent<Renderer>();
                    var material = renderer.material;
                    material.color = new Color(0, 1, 0, 0.3f);
                    material.SetFloat("_Mode", 3); // Set to transparent mode
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 3000;
                }
                else
                {
                    // Different color for remote players
                    head.GetComponent<Renderer>().material.color = Color.blue;
                }
            }
            
            if (m_leftHandTransform == null)
            {
                GameObject leftHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leftHand.name = "LeftHand";
                leftHand.transform.SetParent(transform);
                leftHand.transform.localScale = Vector3.one * 0.1f;
                m_leftHandTransform = leftHand.transform;
                leftHand.GetComponent<Renderer>().material.color = Color.red;
            }
            
            if (m_rightHandTransform == null)
            {
                GameObject rightHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rightHand.name = "RightHand";
                rightHand.transform.SetParent(transform);
                rightHand.transform.localScale = Vector3.one * 0.1f;
                m_rightHandTransform = rightHand.transform;
                rightHand.GetComponent<Renderer>().material.color = Color.red;
            }
        }
        
        private void Update()
        {
            if (!IsOwner || m_cameraRig == null)
                return;
                
            SendTrackingData();
        }
        
        private void SendTrackingData()
        {
            // Send head tracking data
            if (m_centerEyeAnchor != null && IsServer)
            {
                m_headPose.Value = new NetworkedPose
                {
                    position = m_centerEyeAnchor.position,
                    rotation = m_trackHeadRotation ? m_centerEyeAnchor.rotation : Quaternion.identity
                };
            }
            
            // Send hand tracking data
            if (m_leftHandAnchor != null && IsServer)
            {
                m_leftHandPose.Value = new NetworkedPose
                {
                    position = m_leftHandAnchor.position,
                    rotation = m_trackHandRotations ? m_leftHandAnchor.rotation : Quaternion.identity
                };
            }
            
            if (m_rightHandAnchor != null && IsServer)
            {
                m_rightHandPose.Value = new NetworkedPose
                {
                    position = m_rightHandAnchor.position,
                    rotation = m_trackHandRotations ? m_rightHandAnchor.rotation : Quaternion.identity
                };
            }
        }
        
        private void LateUpdate()
        {
            if (IsOwner)
                return;
                
            // Apply received tracking data for remote players
            ApplyRemoteTrackingData();
        }
        
        private void ApplyRemoteTrackingData()
        {
            // Apply head tracking
            if (m_headTransform != null)
            {
                var headPose = m_headPose.Value;
                m_headTransform.position = Vector3.Lerp(m_headTransform.position, headPose.position, m_smoothingSpeed * Time.deltaTime);
                if (m_trackHeadRotation)
                {
                    m_headTransform.rotation = Quaternion.Lerp(m_headTransform.rotation, headPose.rotation, m_smoothingSpeed * Time.deltaTime);
                }
            }
            
            // Apply hand tracking
            if (m_leftHandTransform != null)
            {
                var leftHandPose = m_leftHandPose.Value;
                m_leftHandTransform.position = Vector3.Lerp(m_leftHandTransform.position, leftHandPose.position, m_smoothingSpeed * Time.deltaTime);
                if (m_trackHandRotations)
                {
                    m_leftHandTransform.rotation = Quaternion.Lerp(m_leftHandTransform.rotation, leftHandPose.rotation, m_smoothingSpeed * Time.deltaTime);
                }
            }
            
            if (m_rightHandTransform != null)
            {
                var rightHandPose = m_rightHandPose.Value;
                m_rightHandTransform.position = Vector3.Lerp(m_rightHandTransform.position, rightHandPose.position, m_smoothingSpeed * Time.deltaTime);
                if (m_trackHandRotations)
                {
                    m_rightHandTransform.rotation = Quaternion.Lerp(m_rightHandTransform.rotation, rightHandPose.rotation, m_smoothingSpeed * Time.deltaTime);
                }
            }
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Draw tracking debug info
            if (m_headTransform != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(m_headTransform.position, 0.15f);
            }
            
            if (m_leftHandTransform != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(m_leftHandTransform.position, Vector3.one * 0.1f);
            }
            
            if (m_rightHandTransform != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(m_rightHandTransform.position, Vector3.one * 0.1f);
            }
        }
#endif
    }
}