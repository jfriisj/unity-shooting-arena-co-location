// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using UnityEngine;
using Meta.XR.Samples;
using MRMotifs.Shared;

namespace MRMotifs.Shooting
{
    /// <summary>
    /// Simple AI state machine for drone behavior.
    /// Based on Discover/DroneRage EnemyBehaviour.cs but greatly simplified.
    /// Only runs on master client to reduce network overhead.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    [RequireComponent(typeof(DroneMotif))]
    public class DroneAIMotif : MonoBehaviour
    {
        /// <summary>
        /// AI states for drone behavior.
        /// </summary>
        public enum AIState
        {
            Entering,   // Flying into the room from spawn
            Hovering,   // Idle hover behavior
            Chasing,    // Moving toward target player
            Dying       // Death behavior
        }

        [Header("=== AI SETTINGS ===")]
        
        [SerializeField, Tooltip("Distance to start chasing player")]
        [Range(1f, 10f)]
        private float m_chaseDistance = 5f;
        
        [SerializeField, Tooltip("Distance to maintain from player")]
        [Range(0.5f, 3f)]
        private float m_preferredDistance = 2f;
        
        [SerializeField, Tooltip("How often to switch between hover and chase")]
        [Range(1f, 10f)]
        private float m_behaviorSwitchInterval = 3f;
        
        [SerializeField, Tooltip("Speed when entering the room")]
        [Range(2f, 8f)]
        private float m_enterSpeed = 5f;

        /// <summary>
        /// Current AI state.
        /// </summary>
        public AIState CurrentState { get; private set; } = AIState.Entering;

        private DroneMotif m_drone;
        private Transform m_transform;
        private Vector3 m_enterTargetPosition;
        private float m_stateTimer = 0f;
        private float m_nextBehaviorSwitch = 0f;
        private bool m_isInitialized = false;

        private void Awake()
        {
            m_drone = GetComponent<DroneMotif>();
            m_transform = transform;
        }

        private void Start()
        {
            // Only run AI on master client
            if (!m_drone.Runner.IsMasterClient())
            {
                enabled = false;
                return;
            }

            Initialize();
        }

        private void Initialize()
        {
            DebugLogger.Shooting($"Drone AI initialized | state={CurrentState}", this);
            
            // Set initial enter target (center of room at mid height)
            var roomCenter = Vector3.zero;
            var roomHeight = 1.5f;
            
            // Try to get room center from MRUK if available
            if (Meta.XR.MRUtilityKit.MRUK.Instance?.Rooms?.Count > 0)
            {
                var room = Meta.XR.MRUtilityKit.MRUK.Instance.Rooms[0];
                roomCenter = room.transform.position;
                roomHeight = room.transform.position.y + 1.5f;
            }
            
            m_enterTargetPosition = new Vector3(roomCenter.x, roomHeight, roomCenter.z);
            m_nextBehaviorSwitch = Time.time + m_behaviorSwitchInterval;
            m_isInitialized = true;
            
            ChangeState(AIState.Entering);
        }

        private void Update()
        {
            if (!m_isInitialized || m_drone.Health <= 0f)
            {
                if (CurrentState != AIState.Dying)
                {
                    ChangeState(AIState.Dying);
                }
                return;
            }

            m_stateTimer += Time.deltaTime;
            UpdateCurrentState();
        }

        private void UpdateCurrentState()
        {
            switch (CurrentState)
            {
                case AIState.Entering:
                    UpdateEnteringState();
                    break;
                    
                case AIState.Hovering:
                    UpdateHoveringState();
                    break;
                    
                case AIState.Chasing:
                    UpdateChasingState();
                    break;
                    
                case AIState.Dying:
                    UpdateDyingState();
                    break;
            }
        }

        private void UpdateEnteringState()
        {
            // Move towards enter target position at increased speed
            var originalSpeed = m_drone.GetType().GetField("m_chaseSpeed", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (originalSpeed != null)
            {
                var currentSpeed = (float)originalSpeed.GetValue(m_drone);
                originalSpeed.SetValue(m_drone, m_enterSpeed);
                
                var reachedTarget = m_drone.FlyTowards(m_enterTargetPosition);
                
                originalSpeed.SetValue(m_drone, currentSpeed);
                
                if (reachedTarget || m_stateTimer > 10f) // Timeout after 10 seconds
                {
                    ChangeState(AIState.Hovering);
                }
            }
            else
            {
                // Fallback if reflection fails
                var reachedTarget = m_drone.FlyTowards(m_enterTargetPosition);
                if (reachedTarget || m_stateTimer > 10f)
                {
                    ChangeState(AIState.Hovering);
                }
            }
        }

        private void UpdateHoveringState()
        {
            // Hover around current position
            m_drone.HoverAround(m_transform.position);
            
            // Check if should switch to chasing
            if (Time.time >= m_nextBehaviorSwitch)
            {
                var nearestPlayer = FindNearestPlayer();
                if (nearestPlayer != null)
                {
                    var distance = Vector3.Distance(m_transform.position, nearestPlayer.transform.position);
                    if (distance <= m_chaseDistance)
                    {
                        ChangeState(AIState.Chasing);
                    }
                }
                
                m_nextBehaviorSwitch = Time.time + m_behaviorSwitchInterval;
            }
        }

        private void UpdateChasingState()
        {
            var nearestPlayer = FindNearestPlayer();
            if (nearestPlayer == null)
            {
                ChangeState(AIState.Hovering);
                return;
            }

            var playerPosition = nearestPlayer.transform.position;
            var distance = Vector3.Distance(m_transform.position, playerPosition);
            
            // If too close, hover instead of getting closer
            if (distance <= m_preferredDistance)
            {
                m_drone.HoverAround(m_transform.position);
            }
            else
            {
                // Move towards player but maintain preferred distance
                var directionToPlayer = (playerPosition - m_transform.position).normalized;
                var targetPosition = playerPosition - directionToPlayer * m_preferredDistance;
                targetPosition.y += 0.5f; // Stay slightly above player height
                
                m_drone.FlyTowards(targetPosition);
            }
            
            // Periodically consider switching back to hovering
            if (Time.time >= m_nextBehaviorSwitch)
            {
                if (distance > m_chaseDistance || UnityEngine.Random.value < 0.3f)
                {
                    ChangeState(AIState.Hovering);
                }
                
                m_nextBehaviorSwitch = Time.time + m_behaviorSwitchInterval;
            }
        }

        private void UpdateDyingState()
        {
            // Stop all movement, let physics take over
            // This state is mostly handled by DroneMotif itself
        }

        private PlayerHealthMotif FindNearestPlayer()
        {
            var players = FindObjectsByType<PlayerHealthMotif>(FindObjectsSortMode.None);
            PlayerHealthMotif nearestPlayer = null;
            float nearestDistance = float.MaxValue;

            foreach (var player in players)
            {
                if (player.CurrentHealth <= 0f) continue; // Skip dead players
                
                var distance = Vector3.Distance(m_transform.position, player.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestPlayer = player;
                }
            }

            return nearestPlayer;
        }

        private void ChangeState(AIState newState)
        {
            if (CurrentState == newState) return;
            
            DebugLogger.Shooting($"AI state change | {CurrentState} → {newState}", this);
            
            CurrentState = newState;
            m_stateTimer = 0f;
            
            // State entry logic
            switch (newState)
            {
                case AIState.Entering:
                    // Set target to room center
                    break;
                    
                case AIState.Hovering:
                    // Reset behavior switch timer
                    m_nextBehaviorSwitch = Time.time + UnityEngine.Random.Range(1f, m_behaviorSwitchInterval);
                    break;
                    
                case AIState.Chasing:
                    // Reset behavior switch timer
                    m_nextBehaviorSwitch = Time.time + UnityEngine.Random.Range(1f, m_behaviorSwitchInterval);
                    break;
                    
                case AIState.Dying:
                    // Disable further AI updates
                    enabled = false;
                    break;
            }
        }

        /// <summary>
        /// Force the drone to enter chase state (for testing/debugging).
        /// </summary>
        [ContextMenu("Force Chase State")]
        public void ForceChaseState()
        {
            ChangeState(AIState.Chasing);
        }

        /// <summary>
        /// Force the drone to enter hover state (for testing/debugging).
        /// </summary>
        [ContextMenu("Force Hover State")]
        public void ForceHoverState()
        {
            ChangeState(AIState.Hovering);
        }

        private void OnDisable()
        {
            DebugLogger.Shooting($"Drone AI disabled | state={CurrentState}", this);
        }

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!m_isInitialized) return;
            
            // Visualize state-specific behavior
            switch (CurrentState)
            {
                case AIState.Entering:
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(m_enterTargetPosition, 0.5f);
                    Gizmos.DrawLine(transform.position, m_enterTargetPosition);
                    break;
                    
                case AIState.Hovering:
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(transform.position, 1f);
                    break;
                    
                case AIState.Chasing:
                    var nearestPlayer = FindNearestPlayer();
                    if (nearestPlayer != null)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(transform.position, nearestPlayer.transform.position);
                        Gizmos.DrawWireSphere(nearestPlayer.transform.position, m_preferredDistance);
                    }
                    break;
            }
            
            // Visualize chase distance
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, m_chaseDistance);
        }
        #endif
    }
}
#endif