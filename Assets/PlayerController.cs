using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 8f;
    public float acceleration = 20f;
    public float jumpForce = 5f;
    public float drag = 1f;
    public float extraGravity = 20f;

    [HideInInspector] public float controlMultiplier = 1f;

    public Transform cameraTransform;

    

    private Vector2 moveInput;
    private Rigidbody rb;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        
        rb = GetComponent<Rigidbody>();

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        rb.linearDamping = drag;
        rb.angularDamping = 0.5f;
    }

    void FixedUpdate()
    {
        Move();

        // Add stronger downward force
        rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
    }

    void Move()
    {
        // Get camera directions
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        // Flatten them
        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        // Camera-relative movement
        Vector3 move = forward * moveInput.y + right * moveInput.x;

        Vector3 targetVelocity = move * moveSpeed;
        Vector3 velocity = rb.linearVelocity;

        Vector3 velocityChange = targetVelocity - new Vector3(velocity.x, 0, velocity.z);

        rb.AddForce(velocityChange * acceleration * controlMultiplier, ForceMode.Acceleration);
    }

    // INPUT SYSTEM

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
}