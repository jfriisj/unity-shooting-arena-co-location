// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using Meta.XR.Samples;

namespace MRMotifs.Shooting
{
    /// <summary>
    /// Provides haptic feedback when the player touches walls or room mesh.
    /// Attach to player GameObject with CharacterController.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerWallFeedback : MonoBehaviour
    {
        [Header("Haptic Settings")]
        [Tooltip("Amplitude of haptic feedback (0-1).")]
        [SerializeField] private float m_hapticAmplitude = 0.3f;

        [Tooltip("Enable haptic feedback.")]
        [SerializeField] private bool m_enableHaptic = true;

        [Tooltip("Cooldown time between haptic triggers (seconds).")]
        [Range(0.05f, 1f)]
        [SerializeField] private float m_hapticCooldown = 0.2f;

        [Header("Collision Settings")]
        [Tooltip("Layer mask for wall detection.")]
        [SerializeField] private LayerMask m_wallLayers = ~0;

        private CharacterController m_controller;
        private float m_lastHapticTime;

        private void Awake()
        {
            m_controller = GetComponent<CharacterController>();
            if (m_controller == null)
            {
                Debug.LogWarning("[PlayerWallFeedback] CharacterController not found!");
            }
        }

        private void Update()
        {
            if (!m_enableHaptic || m_controller == null)
            {
                return;
            }

            // CharacterController.collisionFlags tells us what we hit
            if ((m_controller.collisionFlags & CollisionFlags.Sides) != 0)
            {
                // Player is touching a wall
                if (Time.time - m_lastHapticTime > m_hapticCooldown)
                {
                    TriggerHapticFeedback();
                    m_lastHapticTime = Time.time;
                }
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Additional check for specific wall layers
            if (((1 << hit.gameObject.layer) & m_wallLayers) != 0 || 
                hit.gameObject.CompareTag("RoomMesh"))
            {
                if (m_enableHaptic && Time.time - m_lastHapticTime > m_hapticCooldown)
                {
                    TriggerHapticFeedback();
                    m_lastHapticTime = Time.time;
                }
            }
        }

        private void TriggerHapticFeedback()
        {
            // Trigger on both controllers
            OVRInput.SetControllerVibration(m_hapticAmplitude, m_hapticAmplitude, OVRInput.Controller.LTouch);
            OVRInput.SetControllerVibration(m_hapticAmplitude, m_hapticAmplitude, OVRInput.Controller.RTouch);
        }
    }
}
