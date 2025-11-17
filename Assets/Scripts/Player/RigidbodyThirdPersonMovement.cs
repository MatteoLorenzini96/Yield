using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyThirdPersonMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float baseWalkSpeed = 3f;
    [SerializeField] private float baseRunSpeed = 6f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Jump Settings")]
    [SerializeField] private float baseJumpForce = 5f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private FullnessController fullnessController;

    [Header("Boost Modifier")]
    [SerializeField] private float speedBoostModifier = 1f; // default 1 = nessun boost

    private Rigidbody _rigidbody;
    private Animator animator;

    private Vector3 _moveDirection;
    private float _currentSpeed;
    private bool _isGrounded;
    private float _fullnessPercent;

    public float BaseWalkSpeed
    {
        get => baseWalkSpeed;
        set => baseWalkSpeed = value;
    }

    public float BaseRunSpeed
    {
        get => baseRunSpeed;
        set => baseRunSpeed = value;
    }

    public float SpeedBoostModifier
    {
        get => speedBoostModifier;
        set => speedBoostModifier = value;
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.freezeRotation = true;

        animator = GetComponentInChildren<Animator>();
        if (animator == null)
            Debug.LogError("Manca l'Animator!");

        if (fullnessController == null)
            Debug.LogError("Assegna il FullnessController");
    }

    private void Update()
    {
        HandleInput();
        UpdateFullnessEffect();
        CheckGround();
        ApplyBetterJumpPhysics();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        Move();
        Rotate();
    }

    private void HandleInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;

        _moveDirection = (camForward.normalized * vertical + camRight.normalized * horizontal).normalized;

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        _currentSpeed = isRunning ? baseRunSpeed : baseWalkSpeed;

        if (Input.GetKeyDown(KeyCode.Space))
            TryJump();
    }

    private void Move()
    {
        if (_moveDirection.sqrMagnitude > 0.01f)
        {
            Vector3 targetVelocity = _moveDirection * _currentSpeed;
            Vector3 velocityChange = targetVelocity - new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
            _rigidbody.AddForce(velocityChange * acceleration, ForceMode.Acceleration);
        }
        else
        {
            Vector3 flatVelocity = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
            _rigidbody.AddForce(-flatVelocity * acceleration, ForceMode.Acceleration);
        }
    }

    private void Rotate()
    {
        if (_moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    private void CheckGround()
    {
        Vector3 origin = groundCheck != null ? groundCheck.position : transform.position + Vector3.up * 0.1f;
        float distance = groundCheck != null ? groundCheckDistance : 0.6f;
        _isGrounded = Physics.Raycast(origin, Vector3.down, distance, groundMask);
    }

    private void TryJump()
    {
        if (!_isGrounded) return;

        float fullness = fullnessController != null ? fullnessController.CurrentFullness : 1f;
        _fullnessPercent = Mathf.InverseLerp(-1f, 1f, fullness);

        if (_fullnessPercent < 0.25f) return;

        float jumpForce = baseJumpForce * Mathf.Lerp(0.4f, 1f, _fullnessPercent);
        _rigidbody.linearVelocity = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
        _rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void ApplyBetterJumpPhysics()
    {
        if (!_isGrounded)
        {
            if (_rigidbody.linearVelocity.y < 0)
                _rigidbody.AddForce(Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * _rigidbody.mass, ForceMode.Force);
            else if (_rigidbody.linearVelocity.y > 0 && !Input.GetKey(KeyCode.Space))
                _rigidbody.AddForce(Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * _rigidbody.mass, ForceMode.Force);
        }
    }

    private void UpdateFullnessEffect()
    {
        if (fullnessController == null) return;

        _fullnessPercent = Mathf.InverseLerp(-1f, 1f, fullnessController.CurrentFullness);

        baseWalkSpeed = Mathf.Lerp(1f, 3f, _fullnessPercent) * speedBoostModifier;
        baseRunSpeed = Mathf.Lerp(2f, 6f, _fullnessPercent) * speedBoostModifier;

        if (_fullnessPercent <= 0.05f)
        {
            baseWalkSpeed = 0.5f * speedBoostModifier;
            baseRunSpeed = 0.5f * speedBoostModifier;
        }

        if (animator != null)
            animator.SetBool("IsAlmostEmpty", _fullnessPercent < 0.25f);
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        Vector3 horizontalVelocity = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
        float speed = horizontalVelocity.magnitude;
        animator.SetFloat("Speed", speed);

        float verticalVelocity = _rigidbody.linearVelocity.y;
        animator.SetFloat("VerticalVelocity", verticalVelocity);

        animator.SetBool("IsGrounded", _isGrounded);
    }
}
