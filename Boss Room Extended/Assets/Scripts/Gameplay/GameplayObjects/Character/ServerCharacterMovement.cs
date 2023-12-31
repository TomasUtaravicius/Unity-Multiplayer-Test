
using System;
using Unity.BossRoom.Gameplay.Configuration;
using Unity.BossRoom.Navigation;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;
using KinematicCharacterController;

namespace Unity.BossRoom.Gameplay.GameplayObjects.Character
{
    public enum MovementState
    {
        Idle = 0,
        PathFollowing = 1,
        Charging = 2,
        Knockback = 3,
        Interact = 4,
    }

    /// <summary>
    /// Component responsible for moving a character on the server side based on inputs.
    /// </summary>
    /*[RequireComponent(typeof(NetworkCharacterState), typeof(NavMeshAgent), typeof(ServerCharacter)), RequireComponent(typeof(Rigidbody))]*/
    public class ServerCharacterMovement : NetworkBehaviour
    {
        [SerializeField]
        NavMeshAgent m_NavMeshAgent;

        [SerializeField]
        bool isNPC = true;

        [SerializeField]
        CharacterController m_CharacterController;
        [SerializeField]
        KinematicCharacterController m_KinematicCharacterController;
        [SerializeField]
        KinematicCharacterMotor m_KinematicMotor;
        [SerializeField]
        Rigidbody m_Rigidbody;

        [SerializeField]
        Animator m_Animator;

        private NavigationSystem m_NavigationSystem;

        private DynamicNavPath m_NavPath;

        public MovementState m_MovementState;

        MovementStatus m_PreviousState;

        [SerializeField]
        private ServerCharacter m_CharLogic;

        // when we are in charging and knockback mode, we use these additional variables
        private float m_ForcedSpeed;
        private float m_SpecialModeDurationRemaining;


        // Character inputs
        private Vector3 m_MovementInput;
        private Quaternion m_CameraInput;
        private bool m_JumpInput;
        private bool m_CrouchInput;

        // this one is specific to knockback mode
        private Vector3 m_KnockbackVector;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool TeleportModeActivated { get; set; }

        const float k_CheatSpeed = 20;

        public bool SpeedCheatActivated { get; set; }
#endif
        //Example
        void Awake()
        {
            // disable this NetworkBehavior until it is spawned
            enabled = false;
            m_KinematicMotor.enabled = false;
            m_KinematicCharacterController.enabled = false;
            m_MovementInput = Vector3.zero;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Only enable server component on servers
                enabled = true;
                m_KinematicMotor.enabled = true;
                m_KinematicCharacterController.enabled = true;
                // On the server enable navMeshAgent and initialize
                m_NavMeshAgent.enabled = true;
               
                Debug.Log("Enabling stuff");
                m_NavigationSystem = GameObject.FindGameObjectWithTag(NavigationSystem.NavigationSystemTag).GetComponent<NavigationSystem>();
                m_NavPath = new DynamicNavPath(m_NavMeshAgent, m_NavigationSystem);
            }
        }

        /// <summary>
        /// Sets a movement target. We will path to this position, avoiding static obstacles.
        /// </summary>
        /// <param name="position">Position in world space to path to. </param>
        public void SetMovementTarget(Vector3 position)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (TeleportModeActivated)
            {
                Teleport(position);
                return;
            }
#endif
            Debug.Log("Setting Movement state to PathFollowing");
            m_MovementState = MovementState.PathFollowing;
            m_NavPath.SetTargetPosition(position);
        }
        public void SetMovementInput(Vector3 movement, Quaternion camera, bool jump, bool crouch)
        {
            if (m_Animator.GetBool("IsInteracting"))
                return;

            m_MovementState = MovementState.PathFollowing;
            m_MovementInput = movement;
            m_CameraInput = camera;
            m_JumpInput = jump;
            m_CrouchInput = crouch;
        }

        public void StartForwardCharge(float speed, float duration)
        {
            m_NavPath.Clear();
            Debug.Log("Setting Movement state to Charging");
            m_MovementState = MovementState.Charging;
            m_ForcedSpeed = speed;
            m_SpecialModeDurationRemaining = duration;
        }
        public void StartInteraction()
        {
            Debug.Log("Setting Movement state to Interacting");
           
            m_MovementState = MovementState.Interact;
            m_Animator.SetBool("IsInteracting", true);
        }

        public void StartKnockback(Vector3 knocker, float speed, float duration)
        {
            m_NavPath.Clear();
            Debug.Log("Setting Movement state to Knockback");
            m_MovementState = MovementState.Knockback;
            m_KnockbackVector = transform.position - knocker;
            m_ForcedSpeed = speed;
            m_SpecialModeDurationRemaining = duration;
        }

        /// <summary>
        /// Follow the given transform until it is reached.
        /// </summary>
        /// <param name="followTransform">The transform to follow</param>
        public void FollowTransform(Transform followTransform)
        {
            if(isNPC)
            {
                Debug.Log("Setting Movement state to PathFollowing");
                m_MovementState = MovementState.PathFollowing;
                m_NavPath.FollowTransform(followTransform);
            }
            
        }

        /// <summary>
        /// Returns true if the current movement-mode is unabortable (e.g. a knockback effect)
        /// </summary>
        /// <returns></returns>
        public bool IsPerformingForcedMovement()
        {
            return m_MovementState == MovementState.Knockback || m_MovementState == MovementState.Charging;
        }

        /// <summary>
        /// Returns true if the character is actively moving, false otherwise.
        /// </summary>
        /// <returns></returns>
        public bool IsMoving()
        {
            return m_MovementState != MovementState.Idle;
        }

        /// <summary>
        /// Cancels any moves that are currently in progress.
        /// </summary>
        public void CancelMove()
        {
            m_NavPath?.Clear();
            Debug.Log("Setting Movement state to Idle");
            m_MovementState = MovementState.Idle;
        }

        /// <summary>
        /// Instantly moves the character to a new position. NOTE: this cancels any active movement operation!
        /// This does not notify the client that the movement occurred due to teleportation, so that needs to
        /// happen in some other way, such as with the custom action visualization in DashAttackActionFX. (Without
        /// this, the clients will animate the character moving to the new destination spot, rather than instantly
        /// appearing in the new spot.)
        /// </summary>
        /// <param name="newPosition">new coordinates the character should be at</param>
        public void Teleport(Vector3 newPosition)
        {
            CancelMove();
            if (!m_NavMeshAgent.Warp(newPosition))
            {
                // warping failed! We're off the navmesh somehow. Weird... but we can still teleport
                Debug.LogWarning($"NavMeshAgent.Warp({newPosition}) failed!", gameObject);
                transform.position = newPosition;
            }

            m_Rigidbody.position = transform.position;
            m_Rigidbody.rotation = transform.rotation;
        }

        private void FixedUpdate()
        {
            PerformMovement();

            var currentState = GetMovementStatus(m_MovementState);
            if (m_PreviousState != currentState)
            {
                m_CharLogic.MovementStatus.Value = currentState;
                m_PreviousState = currentState;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (m_NavPath != null)
            {
                m_NavPath.Dispose();
            }
            if (IsServer)
            {
                // Disable server components when despawning
                enabled = false;
                if(isNPC)
                {
                    m_NavMeshAgent.enabled = false;
                }
                
            }
        }
        public void PerformInteractiveMovement(Vector3 movementVector)
        {

            m_KinematicCharacterController.MaxStableMoveSpeed = 50f;
            m_KinematicCharacterController.StableMovementSharpness = 50f;
            m_KinematicCharacterController.OrientationSharpness = 50f;
            PlayerCharacterInputs characterInputs = new PlayerCharacterInputs();

            // Build the CharacterInputs struct

            /*  characterInputs.MoveAxisForward = 0;
              characterInputs.MoveAxisRight = 0;
              characterInputs.CameraRotation = Quaternion.identity;
              characterInputs.JumpDown = false;
              characterInputs.CrouchDown = false;
              characterInputs.CrouchUp = false;


              movementVector = movementVector * 10;*/
            //characterInputs.MoveVector

            AICharacterInputs animationInput = new AICharacterInputs();
            animationInput.MoveVector = movementVector;
 
            animationInput.LookVector = this.transform.forward;

            Debug.LogWarning("Rotation: " + this.transform.forward);
            m_KinematicCharacterController.SetInputs(ref animationInput);
           // m_KinematicCharacterController.UpdateVelocity(ref movementVector, Time.fixedDeltaTime);
          //  m_KinematicCharacterController.SetInputs(ref characterInputs);
            m_Rigidbody.position = transform.position;
            m_Rigidbody.rotation = transform.rotation;
        }
        private void PerformMovement()
        {
            if (m_MovementState == MovementState.Idle)
                return;

            if (m_MovementState == MovementState.Interact)
                return;

            Vector3 movementVector;
            

            if (m_MovementState == MovementState.Charging)
            {
                // if we're done charging, stop moving
                m_SpecialModeDurationRemaining -= Time.fixedDeltaTime;
                if (m_SpecialModeDurationRemaining <= 0)
                {
                    m_MovementState = MovementState.Idle;
                    return;
                }

                var desiredMovementAmount = m_ForcedSpeed * Time.fixedDeltaTime;
                movementVector = transform.forward * desiredMovementAmount;
            }
            else if (m_MovementState == MovementState.Knockback)
            {
                m_SpecialModeDurationRemaining -= Time.fixedDeltaTime;
                if (m_SpecialModeDurationRemaining <= 0)
                {
                    m_MovementState = MovementState.Idle;
                    return;
                }

                var desiredMovementAmount = m_ForcedSpeed * Time.fixedDeltaTime;
                movementVector = m_KnockbackVector * desiredMovementAmount;
            }
            else
            {
                if(!isNPC)
                {
                    movementVector = new Vector3(m_MovementInput.x, 0.0f, m_MovementInput.z);
                    // Normalize the movement direction to ensure consistent speed in all directions
                    movementVector *= GetBaseMovementSpeed() * Time.fixedDeltaTime;

                    if (movementVector.magnitude > 1.0f)
                    {
                        movementVector.Normalize();
                    }

                    m_KinematicCharacterController.MaxStableMoveSpeed = GetBaseMovementSpeed();
                    m_KinematicCharacterController.StableMovementSharpness = GetBaseMovementSpeed();
                    m_KinematicCharacterController.OrientationSharpness = GetBaseMovementSpeed();
                    PlayerCharacterInputs characterInputs = new PlayerCharacterInputs();

                    Debug.LogError("Regular movement state runs");
                     // Build the CharacterInputs struct
                     characterInputs.MoveAxisForward = m_MovementInput.z;
                     characterInputs.MoveAxisRight = m_MovementInput.x;
                     characterInputs.CameraRotation = m_CameraInput;
                     characterInputs.JumpDown = m_JumpInput;
                     characterInputs.CrouchDown = false;
                     characterInputs.CrouchUp = false;

                     m_KinematicCharacterController.SetInputs(ref characterInputs);
                     m_Rigidbody.position = transform.position;
                     m_Rigidbody.rotation = transform.rotation;

                    // If we didn't move stop moving.
                    if (movementVector == Vector3.zero)
                    {
                        m_MovementState = MovementState.Idle;
                        return;
                    }
                }
                else
                {
                    var desiredMovementAmount = GetBaseMovementSpeed() * Time.fixedDeltaTime;
                    movementVector = m_NavPath.MoveAlongPath(desiredMovementAmount);

                    // If we didn't move stop moving.
                    if (movementVector == Vector3.zero)
                    {
                        m_MovementState = MovementState.Idle;
                        return;
                    }

                }
                
            }
            /*
            else
            {
                var desiredMovementAmount = GetBaseMovementSpeed() * Time.fixedDeltaTime;
                movementVector = m_NavPath.MoveAlongPath(desiredMovementAmount);

                // If we didn't move stop moving.
                if (movementVector == Vector3.zero)
                {
                    m_MovementState = MovementState.Idle;
                    return;
                }
            }*/

            //m_NavMeshAgent.Move(movementVector);
            //m_CharacterController.Move(movementVector);

            // Use the movement input from your SetMovementInput function
            // to set the movement direction.

            

            // Apply the desired movement speed

            if(isNPC)
            {
                // Move the character using Character Controller
                m_CharacterController.Move(movementVector);
                transform.rotation = Quaternion.LookRotation(movementVector);
                // After moving adjust the position of the dynamic rigidbody.
                m_Rigidbody.position = transform.position;
                m_Rigidbody.rotation = transform.rotation;
            }
            
        }

        /// <summary>
        /// Retrieves the speed for this character's class.
        /// </summary>
        private float GetBaseMovementSpeed()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (SpeedCheatActivated)
            {
                return k_CheatSpeed;
            }
#endif
            CharacterClass characterClass = GameDataSource.Instance.CharacterDataByType[m_CharLogic.CharacterType];
            Assert.IsNotNull(characterClass, $"No CharacterClass data for character type {m_CharLogic.CharacterType}");
            return characterClass.Speed;
        }

        /// <summary>
        /// Determines the appropriate MovementStatus for the character. The
        /// MovementStatus is used by the client code when animating the character.
        /// </summary>
        private MovementStatus GetMovementStatus(MovementState movementState)
        {
            switch (movementState)
            {
                case MovementState.Idle:
                    return MovementStatus.Idle;
                case MovementState.Knockback:
                    return MovementStatus.Uncontrolled;
                default:
                    return MovementStatus.Normal;
            }
        }
    }
}
