
using UnityEngine;



public class PlayerController : MonoBehaviour
{
    public float MaxForwardVelocity = 8f;        // Max run speed.
    public float MinTurnVelocity = 400f;         // Turn velocity when moving at maximum speed.
    public float MaxTurnVelocity = 1200f;        // Turn velocity when stationary.
    Animator _anim;
    CharacterController _characterController;
    bool _isGrounded = true;
    bool _previouslyGrounded;
    const float kAirborneTurnSpeedProportion = 5.4f;
    const float kGroundedRayDistance = 1f;
    const float kJumpAbortSpeed = 10f;
    const float kMinEnemyDotCoeff = 0.2f;
    const float kInverseOneEighty = 1f / 180f;
    const float kStickingGravityProportion = 0.3f;
    const float kGroundAcceleration = 20f;
    const float kGroundDeceleration = 25f;

    Material materialUnderFoot;
    float _forwardVelocity;
    float _desiredForwardSpeed;
    float _verticalVelocity;
    
    Quaternion _targetDirection;    // direction we want to move towards
    float _angleDiff;               // delta between targetRotation and current roataion
    Quaternion _targetRotation;
    Vector2 _moveInput;

    protected bool IsMoveInput
    {
        get { return !Mathf.Approximately(_moveInput.sqrMagnitude, 0f); }
    }


	void Awake()
    {
        _anim = GetComponent<Animator>();
        _characterController = GetComponent<CharacterController>();
        _targetDirection = Quaternion.Euler(0, 90, 0);
    }

    void Update()
    {
        _moveInput = new Vector2(
            Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical")
        );
        _anim.SetFloat("horizontal", _moveInput.x);
        _anim.SetFloat("vertical", _moveInput.y);
    }

    void FixedUpdate()
    {
        // CacheAnimatorState();

        // UpdateInputBlocking();

        // EquipMeleeWeapon(IsWeaponEquiped());

        // m_Animator.SetFloat(m_HashStateTime, Mathf.Repeat(m_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f));
        // m_Animator.ResetTrigger(m_HashMeleeAttack);

        // if (m_Input.Attack && canAttack)
            // m_Animator.SetTrigger(m_HashMeleeAttack);

        CalculateForwardMovement();
        // CalculateVerticalMovement();

        SetTargetRotation();

        // if (IsOrientationUpdated() && IsMoveInput)
        //     UpdateOrientation();
        UpdateOrientation();

        // PlayAudio();

        // TimeoutToIdle();

        _previouslyGrounded = _isGrounded;
    }


    // Called each physics step (so long as the Animator component is set to Animate Physics) after FixedUpdate to override root motion.
    void OnAnimatorMove()
    {
        if (_anim == null)
            return;
        Vector3 movement;
        // if (_isGrounded)
        if (true)
        {
            // find ground
            RaycastHit hit;
            Ray ray = new Ray(transform.position + Vector3.up * kGroundedRayDistance * 0.5f, -Vector3.up);
            if (Physics.Raycast(ray, out hit, kGroundedRayDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                // project velocity on plane
                var deltaPosition = _anim.deltaPosition;
                movement = Vector3.ProjectOnPlane(_anim.deltaPosition, hit.normal);
                
                // store material under foot
                Renderer groundRenderer = hit.collider.GetComponentInChildren<Renderer>();
                materialUnderFoot = groundRenderer ? groundRenderer.sharedMaterial : null;
            }
            else
            {
                // fail safe incase ray does not collide
                movement = _anim.deltaPosition;
                materialUnderFoot = null;
            }
        }
        else
        {
            // in air, use the forward velocity
            movement = _forwardVelocity * transform.forward * Time.deltaTime;
        }
        // Rotate the transform of the character controller by the animation's root rotation.
        _characterController.transform.rotation *= _anim.deltaRotation;

        // Add to the movement with the calculated vertical speed.
        movement += _verticalVelocity * Vector3.up * Time.deltaTime;

        // Move the character controller.
        _characterController.Move(movement);

        // After the movement store whether or not the character controller is grounded.
        _isGrounded = _characterController.isGrounded;

        // If Ellen is not on the ground then send the vertical speed to the animator.
        // This is so the vertical speed is kept when landing so the correct landing animation is played.
        if (!_isGrounded)
            _anim.SetFloat("VerticalVelocity", _verticalVelocity);

        // Send whether or not Ellen is on the ground to the animator.
        _anim.SetBool("OnGround", _isGrounded);
    }

    void SetTargetRotation()
    {
        // Create three variables, move input local to the player, flattened forward direction of the camera and a local target rotation.
        Vector2 moveInput = _moveInput;
        Vector3 localMovementDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        
        // Vector3 forward = Quaternion.Euler(0f, cameraSettings.Current.m_XAxis.Value, 0f) * Vector3.forward;
        Vector3 forward = _targetDirection * Vector3.forward;
        forward.y = 0f;
        forward.Normalize();

        Quaternion targetRotation;
        
        // If the local movement direction is the opposite of forward then the target rotation should be towards the camera.
        if (Mathf.Approximately(Vector3.Dot(localMovementDirection, Vector3.forward), -1.0f))
        {
            targetRotation = Quaternion.LookRotation(-forward);
        }
        else
        {
            // Otherwise the rotation should be the offset of the input from the camera's forward.
            Quaternion cameraToInputOffset = Quaternion.FromToRotation(Vector3.forward, localMovementDirection);
            targetRotation = Quaternion.LookRotation(cameraToInputOffset * forward);
        }

        // The desired forward direction of Ellen.
        Vector3 resultingForward = targetRotation * Vector3.forward;

        // Find the difference between the current rotation of the player and the desired rotation of the player in radians.
        float angleCurrent = Mathf.Atan2(transform.forward.x, transform.forward.z) * Mathf.Rad2Deg;
        float targetAngle = Mathf.Atan2(resultingForward.x, resultingForward.z) * Mathf.Rad2Deg;

        _angleDiff = Mathf.DeltaAngle(angleCurrent, targetAngle);
        _targetRotation = targetRotation;
    }    
    void UpdateOrientation()
    {
        _anim.SetFloat("AngleDeltaRad", _angleDiff * Mathf.Deg2Rad);

        Vector3 localInput = new Vector3(_moveInput.x, 0f, _moveInput.y);
        float groundedTurnSpeed = Mathf.Lerp(MaxTurnVelocity, MinTurnVelocity, _forwardVelocity / _desiredForwardSpeed);
        float actualTurnSpeed = _isGrounded ? groundedTurnSpeed : Vector3.Angle(transform.forward, localInput) * kInverseOneEighty * kAirborneTurnSpeedProportion * groundedTurnSpeed;
        _targetRotation = Quaternion.RotateTowards(transform.rotation, _targetRotation, actualTurnSpeed * Time.deltaTime);

        transform.rotation = _targetRotation;
    }

    void CalculateForwardMovement()
    {
        // Cache the move input and cap it's magnitude at 1.
        Vector2 moveInput = _moveInput;
        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();

        // Calculate the speed intended by input.
        _desiredForwardSpeed = moveInput.magnitude * MaxForwardVelocity;

        // Determine change to speed based on whether there is currently any move input.
        float acceleration = IsMoveInput ? kGroundAcceleration : kGroundDeceleration;

        // Adjust the forward speed towards the desired speed.
        _forwardVelocity = Mathf.MoveTowards(_forwardVelocity, _desiredForwardSpeed, acceleration * Time.deltaTime);

        // Set the animator parameter to control what animation is being played.
        _anim.SetFloat("ForwardVelocity", _forwardVelocity);
    }


}