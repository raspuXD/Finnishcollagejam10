using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("References")]
    public Transform target;
    public Transform pivot;

    [Header("Rotation")]
    public float sensitivity  = 200f;
    public float minPitch     = -30f;
    public float maxPitch     = 75f;

    [Header("Position")]
    public float distance         = 5f;
    public float height           = 1.5f;
    public float followSmoothness = 10f;

    private float yaw;
    private float pitch;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        yaw   = angles.y;
        pitch = angles.x;
    }

    void Update()
    {
        // Follow the player in sync with physics
        Vector3 targetPos = target.position + Vector3.up * height;
        pivot.position = Vector3.Lerp(
            pivot.position,
            targetPos,
            followSmoothness * Time.fixedDeltaTime
        );
    }

    void LateUpdate()
    {
        HandleRotation();

        // Camera collision
        Vector3 desiredPosition = pivot.position - pivot.forward * distance;

        if (Physics.Linecast(pivot.position, desiredPosition, out RaycastHit hit))
            transform.position = hit.point;
        else
            transform.position = desiredPosition;

        transform.LookAt(pivot);
    }

    void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        yaw   += mouseX;
        pitch -= mouseY;
        pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);

        pivot.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}