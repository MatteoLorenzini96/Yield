using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyThirdPersonMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    private Rigidbody _rigidbody;
    private Vector3 _moveDirection;
    private float _currentSpeed;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.freezeRotation = true;
    }

    private void Update()
    {
        HandleInput();
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

        // Calcola direzione rispetto alla camera
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        _moveDirection = (camForward.normalized * vertical + camRight.normalized * horizontal).normalized;

        // Cambia velocit� in base al tasto premuto
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        _currentSpeed = isRunning ? runSpeed : walkSpeed;
    }

    private void Move()
    {
        if (_moveDirection.sqrMagnitude > 0.01f)
        {
            // Velocit� _target
            Vector3 targetVelocity = _moveDirection * _currentSpeed;
            Vector3 velocityChange = targetVelocity - _rigidbody.linearVelocity;
            velocityChange.y = 0f;

            // Applica forza proporzionale alla differenza
            _rigidbody.AddForce(velocityChange * acceleration, ForceMode.Acceleration);
        }
        else
        {
            // Riduce gradualmente la velocit� se non ci si muove
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
}
