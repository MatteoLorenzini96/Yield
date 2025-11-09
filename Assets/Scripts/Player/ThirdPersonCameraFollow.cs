using UnityEngine;

public class ThirdPersonCameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform _target;
    [SerializeField] private Vector3 _offset = new Vector3(0f, 3f, -6f);

    [Header("Camera Settings")]
    [SerializeField] private float _rotationSpeed = 5f;
    [SerializeField] private float _smoothSpeed = 10f;
    [SerializeField] private float _minYAngle = -35f;
    [SerializeField] private float _maxYAngle = 60f;

    private float _mouseX;
    private float _mouseY;

    private void Start()
    {
        // Blocca e nasconde il cursore all'avvio
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (_target == null)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        HandleCameraRotation();
        FollowTarget();
    }

    private void HandleCameraRotation()
    {
        _mouseX += Input.GetAxis("Mouse X") * _rotationSpeed;
        _mouseY -= Input.GetAxis("Mouse Y") * _rotationSpeed;
        _mouseY = Mathf.Clamp(_mouseY, _minYAngle, _maxYAngle);
    }

    private void FollowTarget()
    {
        Quaternion rotation = Quaternion.Euler(_mouseY, _mouseX, 0f);
        Vector3 desiredPosition = _target.position + rotation * _offset;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed * Time.deltaTime);
        transform.LookAt(_target.position + Vector3.up * 1.5f);
    }
}
