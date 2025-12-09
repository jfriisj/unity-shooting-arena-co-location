using Unity.Netcode;
using UnityEngine;
using MRMotifs.Shooting;

#if UNITY_ANDROID && !UNITY_EDITOR
using Oculus.Avatar2;
#endif

namespace Shooting
{
    /// <summary>
    /// Adapter that integrates Meta's realistic avatars with the shooting game mechanics.
    /// This script gets attached to Meta avatar instances to add shooting functionality.
    /// </summary>
    public class MetaAvatarShootingAdapter : NetworkBehaviour
    {
        [Header("Shooting Components")]
        [SerializeField] private PlayerHealthMotif m_playerHealth;
        [SerializeField] private PlayerStatsMotif m_playerStats;
        [SerializeField] private ShootingPlayerMotif m_shootingPlayer;
        [SerializeField] private ShootingHUDMotif m_shootingHUD;
        
        [Header("Avatar Integration")]
        [SerializeField] private Component m_avatarEntity;
        [SerializeField] private Transform m_leftHandBone;
        [SerializeField] private Transform m_rightHandBone;
        [SerializeField] private Transform m_headBone;
        
        [Header("Weapon Attachment")]
        [SerializeField] private Transform m_weaponAttachPoint;
        [SerializeField] private Vector3 m_weaponOffset = new Vector3(0, 0, 0.05f);
        [SerializeField] private Vector3 m_weaponRotationOffset = new Vector3(-90, 180, 0);
        
        private OVRCameraRig m_cameraRig;
        private bool m_isInitialized = false;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // Find the local camera rig
            m_cameraRig = FindFirstObjectByType<OVRCameraRig>();
            
            // Get the avatar entity if not assigned
            if (m_avatarEntity == null)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                m_avatarEntity = GetComponent<Oculus.Avatar2.OvrAvatarEntity>();
#else
                // In editor, try to find any avatar-related component
                m_avatarEntity = GetComponent<Transform>(); // Fallback to Transform
#endif
            }
                
            InitializeShootingComponents();
            
            if (IsOwner)
            {
                SetupLocalPlayer();
            }
            else
            {
                SetupRemotePlayer();
            }
        }
        
        private void InitializeShootingComponents()
        {
            // Add shooting components if they don't exist
            if (m_playerHealth == null)
            {
                m_playerHealth = gameObject.GetComponent<PlayerHealthMotif>();
                if (m_playerHealth == null)
                    m_playerHealth = gameObject.AddComponent<PlayerHealthMotif>();
            }
            
            if (m_playerStats == null)
            {
                m_playerStats = gameObject.GetComponent<PlayerStatsMotif>();
                if (m_playerStats == null)
                    m_playerStats = gameObject.AddComponent<PlayerStatsMotif>();
            }
            
            if (m_shootingPlayer == null)
            {
                m_shootingPlayer = gameObject.GetComponent<ShootingPlayerMotif>();
                if (m_shootingPlayer == null)
                    m_shootingPlayer = gameObject.AddComponent<ShootingPlayerMotif>();
            }
            
            // Only add HUD for local player
            if (IsOwner && m_shootingHUD == null)
            {
                m_shootingHUD = gameObject.GetComponent<ShootingHUDMotif>();
                if (m_shootingHUD == null)
                    m_shootingHUD = gameObject.AddComponent<ShootingHUDMotif>();
            }
            
            // Configure the shooting player with avatar bone references
            if (m_shootingPlayer != null)
            {
                SetupWeaponAttachment();
            }
            
            m_isInitialized = true;
        }
        
        private void SetupLocalPlayer()
        {
            // Configure for local player
            if (m_avatarEntity != null)
            {
                // Make local avatar semi-transparent or use first-person view
                SetAvatarVisibility(0.3f);
            }
            
            // Setup HUD
            if (m_shootingHUD != null)
            {
                m_shootingHUD.enabled = true;
            }
        }
        
        private void SetupRemotePlayer()
        {
            // Configure for remote player
            if (m_avatarEntity != null)
            {
                // Full opacity for remote players
                SetAvatarVisibility(1.0f);
            }
            
            // Disable HUD for remote players
            if (m_shootingHUD != null)
            {
                m_shootingHUD.enabled = false;
            }
        }
        
        private void SetupWeaponAttachment()
        {
            // Create weapon attachment point if it doesn't exist
            if (m_weaponAttachPoint == null)
            {
                GameObject attachPoint = new GameObject("WeaponAttachPoint");
                attachPoint.transform.SetParent(transform);
                m_weaponAttachPoint = attachPoint.transform;
            }
            
            // Position the attachment point relative to the right hand
            UpdateWeaponAttachmentPoint();
        }
        
        private void UpdateWeaponAttachmentPoint()
        {
            if (m_weaponAttachPoint == null) return;
            
            // If we have avatar bones, use right hand position
            if (m_rightHandBone != null)
            {
                m_weaponAttachPoint.position = m_rightHandBone.position + m_rightHandBone.TransformDirection(m_weaponOffset);
                m_weaponAttachPoint.rotation = m_rightHandBone.rotation * Quaternion.Euler(m_weaponRotationOffset);
            }
            // Fallback to camera rig right hand anchor
            else if (m_cameraRig != null && m_cameraRig.rightHandAnchor != null)
            {
                var rightHand = m_cameraRig.rightHandAnchor;
                m_weaponAttachPoint.position = rightHand.position + rightHand.TransformDirection(m_weaponOffset);
                m_weaponAttachPoint.rotation = rightHand.rotation * Quaternion.Euler(m_weaponRotationOffset);
            }
        }
        
        private void SetAvatarVisibility(float alpha)
        {
            if (m_avatarEntity == null) return;
            
            // Find all renderers in the avatar and adjust their alpha
            var renderers = m_avatarEntity.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.materials)
                {
                    if (material.HasProperty("_Color"))
                    {
                        Color color = material.color;
                        color.a = alpha;
                        material.color = color;
                    }
                    
                    // Enable transparency if alpha < 1
                    if (alpha < 1.0f)
                    {
                        material.SetFloat("_Mode", 3); // Set to transparent mode
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt("_ZWrite", 0);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.EnableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = 3000;
                    }
                }
            }
        }
        
        private void Update()
        {
            if (!m_isInitialized || !IsOwner) return;
            
            // Update weapon attachment point for local player
            UpdateWeaponAttachmentPoint();
            
            // Update avatar position to match headset (this ensures proper colocation)
            if (m_cameraRig != null && m_cameraRig.centerEyeAnchor != null)
            {
                // Position the avatar at the headset position
                Vector3 headPosition = m_cameraRig.centerEyeAnchor.position;
                Vector3 headForward = m_cameraRig.centerEyeAnchor.forward;
                
                // Project head position to floor level for avatar position
                Vector3 avatarPosition = new Vector3(headPosition.x, transform.position.y, headPosition.z);
                transform.position = avatarPosition;
                
                // Rotate avatar to face the same direction as the headset
                Vector3 forwardDirection = new Vector3(headForward.x, 0, headForward.z).normalized;
                if (forwardDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(forwardDirection);
                }
            }
        }
        
        // Public methods for shooting system integration
        public Transform GetWeaponAttachPoint()
        {
            return m_weaponAttachPoint;
        }
        
        public Vector3 GetLeftHandPosition()
        {
            if (m_leftHandBone != null)
                return m_leftHandBone.position;
            else if (m_cameraRig != null && m_cameraRig.leftHandAnchor != null)
                return m_cameraRig.leftHandAnchor.position;
            return transform.position;
        }
        
        public Vector3 GetRightHandPosition()
        {
            if (m_rightHandBone != null)
                return m_rightHandBone.position;
            else if (m_cameraRig != null && m_cameraRig.rightHandAnchor != null)
                return m_cameraRig.rightHandAnchor.position;
            return transform.position;
        }
        
        public Vector3 GetHeadPosition()
        {
            if (m_headBone != null)
                return m_headBone.position;
            else if (m_cameraRig != null && m_cameraRig.centerEyeAnchor != null)
                return m_cameraRig.centerEyeAnchor.position;
            return transform.position + Vector3.up * 1.8f;
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Draw weapon attachment point
            if (m_weaponAttachPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(m_weaponAttachPoint.position, Vector3.one * 0.1f);
                Gizmos.DrawRay(m_weaponAttachPoint.position, m_weaponAttachPoint.forward * 0.2f);
            }
            
            // Draw hand positions
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(GetLeftHandPosition(), 0.05f);
            Gizmos.DrawWireSphere(GetRightHandPosition(), 0.05f);
            
            // Draw head position
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(GetHeadPosition(), 0.1f);
        }
#endif
    }
}