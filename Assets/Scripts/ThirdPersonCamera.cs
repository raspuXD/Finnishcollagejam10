using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("References")]
    public Transform target;
    public Transform pivot;

    [Header("Input")]
    public InputActionReference lookAction; // Assign in inspector

    [Header("Rotation")]
    public float sensitivity = 200f;
    public float minSensitivity = 1f; // Prevent 0
    public float minPitch = -30f;
    public float maxPitch = 75f;

    [Header("Position")]
    public float distance = 5f;
    public float height = 1.5f;
    public float followSmoothness = 10f;

    private float yaw;
    private float pitch;

    private const string sensitivityKey = "MouseSensitivity";

    void OnEnable()
    {
        if (lookAction != null)
            lookAction.action.Enable();
    }

    void OnDisable()
    {
        if (lookAction != null)
            lookAction.action.Disable();
    }

    void Start()
    {
        // Load saved sensitivity
        sensitivity = PlayerPrefs.GetFloat(sensitivityKey, sensitivity);

        // Safety clamp (fix slider = 0 issue)
        sensitivity = Mathf.Max(sensitivity, minSensitivity);

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    void Update()
    {
        Vector3 targetPos = target.position + Vector3.up * height;
        pivot.position = Vector3.Lerp(
            pivot.position,
            targetPos,
            followSmoothness * Time.deltaTime
        );
    }

    void LateUpdate()
    {
        HandleRotation();

        Vector3 desiredPosition = pivot.position - pivot.forward * distance;

        if (Physics.Linecast(pivot.position, desiredPosition, out RaycastHit hit))
            transform.position = hit.point;
        else
            transform.position = desiredPosition;

        transform.LookAt(pivot);
    }

    void HandleRotation()
    {
        if (lookAction == null) return;

        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();

        float mouseX = lookInput.x * sensitivity * Time.deltaTime;
        float mouseY = lookInput.y * sensitivity * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        pivot.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    // UI Slider
    public void SetSensitivity(float value)
    {
        sensitivity = Mathf.Max(value, minSensitivity); // prevent 0
        PlayerPrefs.SetFloat(sensitivityKey, sensitivity);
        PlayerPrefs.Save();
    }
}