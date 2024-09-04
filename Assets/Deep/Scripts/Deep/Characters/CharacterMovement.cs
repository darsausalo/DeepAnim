using KinematicCharacterController;
using UnityEngine;

namespace AurigaGames.Deep.Characters
{
    [RequireComponent(typeof(KinematicCharacterMotor))]
    [DisallowMultipleComponent]
    public class CharacterMovement : MonoBehaviour, ICharacterController
    {
        private KinematicCharacterMotor _motor;
        private Vector3 _moveInput;
        private Vector3 _lookInput;

        private bool _jumpRequested;
        private bool _jumpConsumed;
        private bool _jumpedThisFrame;
        private float _timeSinceJumpRequested = Mathf.Infinity;
        private float _timeSinceLastAbleToJump;
        private bool _doubleJumpConsumed;
        private bool _canWallJump;
        private Vector3 _wallJumpNormal;

        // TODO: replicable variables
        private Vector3 _velocity;
        private bool _isGrounded;
        private bool _hasMoveInput;

        [Header("Stable Movement")] public float MaxStableMoveSpeed = 10f;
        public float StableMovementLerpTime = 0.1f;

        [Header("Air Movement")] public float MaxAirMoveSpeed = 10f;
        public float AirAccelerationSpeed = 5f;
        public float Drag = 0.1f;

        [Header("Jumping")] public bool AllowJumpingWhenSliding;

        public bool AllowDoubleJump;
        public bool AllowWallJump;

        public float JumpSpeed = 10f;
        public float JumpPreGroundingGraceTime;
        public float JumpPostGroundingGraceTime;

        [Header("Misc")] public Vector3 Gravity = new(0, -30f, 0);

        public KinematicCharacterMotor Motor => _motor;

        public Vector3 Velocity => _velocity;

        public bool IsGrounded => _isGrounded;
        public bool HasMoveInput => _hasMoveInput;

        public bool JumpConsumed => _jumpConsumed;

        private void Awake()
        {
            _motor = GetComponent<KinematicCharacterMotor>();
        }

        private void Start()
        {
            _motor.CharacterController = this;
        }

        public void SetMoveInput(in Vector3 moveInput)
        {
            _moveInput = moveInput;
        }

        public void SetLookInput(in Vector3 lookInput)
        {
            _lookInput = lookInput;
        }

        public void RequestJump()
        {
            _jumpRequested = true;
            _timeSinceJumpRequested = 0.0f;
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_lookInput != Vector3.zero)
            {
                currentRotation = Quaternion.LookRotation(_lookInput, _motor.CharacterUp);
            }
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            Vector3 targetMovementVelocity;
            if (_motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity =
                    _motor.GetDirectionTangentToSurface(currentVelocity, _motor.GroundingStatus.GroundNormal) *
                    currentVelocity.magnitude;

                var inputRight = Vector3.Cross(_moveInput, _motor.CharacterUp);
                var reorientedInput = Vector3.Cross(_motor.GroundingStatus.GroundNormal, inputRight).normalized *
                                      _moveInput.magnitude;
                targetMovementVelocity = reorientedInput * MaxStableMoveSpeed;

                var stableMovementLerpPct =
                    1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / StableMovementLerpTime) * deltaTime);

                currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, stableMovementLerpPct);
            }
            else
            {
                if (_moveInput.sqrMagnitude > 0f)
                {
                    targetMovementVelocity = _moveInput * MaxAirMoveSpeed;

                    if (_motor.GroundingStatus.FoundAnyGround)
                    {
                        var perpendicularObstructionNormal =
                            Vector3.Cross(Vector3.Cross(_motor.CharacterUp, _motor.GroundingStatus.GroundNormal),
                                _motor.CharacterUp).normalized;
                        targetMovementVelocity =
                            Vector3.ProjectOnPlane(targetMovementVelocity, perpendicularObstructionNormal);
                    }

                    var velocityDiff = Vector3.ProjectOnPlane(targetMovementVelocity - currentVelocity, Gravity);
                    currentVelocity += velocityDiff * (AirAccelerationSpeed * deltaTime);
                }

                currentVelocity += Gravity * deltaTime;

                currentVelocity *= (1f / (1f + (Drag * deltaTime)));
            }

            // Handle jumping
            {
                _jumpedThisFrame = false;
                _timeSinceJumpRequested += deltaTime;
                if (_jumpRequested)
                {
                    // Handle double jump
                    if (AllowDoubleJump)
                    {
                        if (_jumpConsumed && !_doubleJumpConsumed &&
                            (AllowJumpingWhenSliding
                                ? !_motor.GroundingStatus.FoundAnyGround
                                : !_motor.GroundingStatus.IsStableOnGround))
                        {
                            _motor.ForceUnground();

                            // Add to the return velocity and reset jump state
                            currentVelocity += (_motor.CharacterUp * JumpSpeed) -
                                               Vector3.Project(currentVelocity, _motor.CharacterUp);
                            _jumpRequested = false;
                            _doubleJumpConsumed = true;
                            _jumpedThisFrame = true;
                        }
                    }

                    // See if we actually are allowed to jump
                    if (_canWallJump ||
                        !_jumpConsumed && ((AllowJumpingWhenSliding
                                               ? _motor.GroundingStatus.FoundAnyGround
                                               : _motor.GroundingStatus.IsStableOnGround) ||
                                           _timeSinceLastAbleToJump <= JumpPostGroundingGraceTime))
                    {
                        // Calculate jump direction before ungrounding
                        var jumpDirection = _motor.CharacterUp;
                        if (_canWallJump)
                        {
                            jumpDirection = _wallJumpNormal;
                        }
                        else if (_motor.GroundingStatus.FoundAnyGround && !_motor.GroundingStatus.IsStableOnGround)
                        {
                            jumpDirection = _motor.GroundingStatus.GroundNormal;
                        }

                        // Makes the character skip ground probing/snapping on its next update. 
                        // If this line weren't here, the character would remain snapped to the ground when trying to jump. Try commenting this line out and see.
                        _motor.ForceUnground();

                        // Add to the return velocity and reset jump state
                        currentVelocity += (jumpDirection * JumpSpeed) -
                                           Vector3.Project(currentVelocity, _motor.CharacterUp);
                        _jumpRequested = false;
                        _jumpConsumed = true;
                        _jumpedThisFrame = true;
                    }
                }

                // Reset wall jump
                _canWallJump = false;
            }
        }

        public void BeforeCharacterUpdate(float deltaTime)
        {
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            if (_motor.GroundingStatus.IsStableOnGround != _motor.LastGroundingStatus.IsStableOnGround)
            {
                _isGrounded = _motor.GroundingStatus.IsStableOnGround;
            }
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            // Update velocities
            _velocity = _motor.BaseVelocity;
            _hasMoveInput = _moveInput.normalized.magnitude > Mathf.Epsilon;

            // Handle jump-related values
            {
                // Handle jumping pre-ground grace period
                if (_jumpRequested && _timeSinceJumpRequested > JumpPreGroundingGraceTime)
                {
                    _jumpRequested = false;
                }

                if (AllowJumpingWhenSliding
                    ? _motor.GroundingStatus.FoundAnyGround
                    : _motor.GroundingStatus.IsStableOnGround)
                {
                    // If we're on a ground surface, reset jumping values
                    if (!_jumpedThisFrame)
                    {
                        _doubleJumpConsumed = false;
                        _jumpConsumed = false;
                    }

                    _timeSinceLastAbleToJump = 0f;
                }
                else
                {
                    // Keep track of time since we were last able to jump (for grace period)
                    _timeSinceLastAbleToJump += deltaTime;
                }
            }
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            return true;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
            // We can wall jump only if we are not stable on ground and are moving against an obstruction
            if (AllowWallJump && !_motor.GroundingStatus.IsStableOnGround && !hitStabilityReport.IsStable)
            {
                _canWallJump = true;
                _wallJumpNormal = hitNormal;
            }
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            Vector3 atCharacterPosition,
            Quaternion atCharacterRotation,
            ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }
    }
}