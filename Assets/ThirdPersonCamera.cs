using UnityEngine;
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("References")]
    public Transform target;        // The ball (player)
    public Transform pivot;         // Empty object for rotation

    [Header("Rotation")]
    public float sensitivity = 200f;
    public float minPitch = -30f;
    public float maxPitch = 75f;

    [Header("Position")]
    public float distance = 5f;
    public float height = 1.5f;
    public float followSmoothness = 10f;

    private float yaw;
    private float pitch;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    void LateUpdate()
    {
        HandleRotation();
        FollowTarget();
    }

    void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        pivot.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void FollowTarget()
    {
        // Move pivot to player smoothly
        Vector3 targetPos = target.position + Vector3.up * height;

        pivot.position = Vector3.Lerp(
            pivot.position,
            targetPos,
            followSmoothness * Time.deltaTime
        );

        // --- CAMERA COLLISION ---
        RaycastHit hit;
        Vector3 desiredPosition = pivot.position - pivot.forward * distance;

        if (Physics.Linecast(pivot.position, desiredPosition, out hit))
        {
            transform.position = hit.point;
        }
        else
        {
            transform.position = desiredPosition;
        }

        // Always look at pivot
        transform.LookAt(pivot);
    }
}