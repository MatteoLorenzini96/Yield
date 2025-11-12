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
    [SerializeField] private float fallMultiplier = 2.5f;     // aumenta la gravità in caduta
    [SerializeField] private float lowJumpMultiplier = 2f;    // aumenta la gravità se si rilascia presto il salto
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private FullnessController fullnessController;

    private Rigidbody _rigidbody;
    private Vector3 _moveDirection;
    private float _currentSpeed;
    private bool _isGrounded;
    private float _fullnessPercent; // da 0 (vuoto) a 1 (pieno)

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.freezeRotation = true;

        if (fullnessController == null)
            Debug.LogError("Assegna il FullnessController");
    }

    private void Update()
    {
        HandleInput();
        UpdateFullnessEffect();
        CheckGround();
        ApplyBetterJumpPhysics();
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
        {
            TryJump();
        }
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

        // (DEBUG disattivato)
        // Debug.DrawRay(origin, Vector3.down * distance, _isGrounded ? Color.green : Color.red);
    }

    private void TryJump()
    {
        if (!_isGrounded)
            return;

        float fullness = fullnessController != null ? fullnessController.CurrentFullness : 1f;
        _fullnessPercent = Mathf.InverseLerp(-1f, 1f, fullness);

        if (_fullnessPercent < 0.25f)
            return;

        float jumpForce = baseJumpForce * Mathf.Lerp(0.4f, 1f, _fullnessPercent);
        _rigidbody.linearVelocity = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z); // reset verticale
        _rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void ApplyBetterJumpPhysics()
    {
        // Solo se non è a terra
        if (!_isGrounded)
        {
            if (_rigidbody.linearVelocity.y < 0)
            {
                // Caduta più veloce
                _rigidbody.AddForce(Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * _rigidbody.mass, ForceMode.Force);
            }
            else if (_rigidbody.linearVelocity.y > 0 && !Input.GetKey(KeyCode.Space))
            {
                // Se rilasci lo spazio prima della fine del salto, salta meno in alto
                _rigidbody.AddForce(Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * _rigidbody.mass, ForceMode.Force);
            }
        }
    }

    private void UpdateFullnessEffect()
    {
        if (fullnessController == null) return;

        _fullnessPercent = Mathf.InverseLerp(-1f, 1f, fullnessController.CurrentFullness);

        // Scala velocità base in base alla fullness
        baseWalkSpeed = Mathf.Lerp(1f, 3f, _fullnessPercent);
        baseRunSpeed = Mathf.Lerp(2f, 6f, _fullnessPercent);

        // Stato “trascinamento”
        if (_fullnessPercent <= 0.05f)
        {
            baseWalkSpeed = 0.5f;
            baseRunSpeed = 0.5f;
            // animator?.SetBool("IsCrawling", true);
        }
        else
        {
            // animator?.SetBool("IsCrawling", false);
        }
    }
}
