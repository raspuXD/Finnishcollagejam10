using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 8f;
    public float acceleration = 20f;

    public float jumpForce = 5f;
    public int MaxJump = 1;
    private int CurrentJumps;

    public float drag = 1f;
    public float extraGravity = 20f;

    [HideInInspector] public float controlMultiplier = 1f;

    public Transform cameraTransform;

    [Header("References")]
    public MagnetController magnetController;
    public UpgradeManager upgradeManager;

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
        rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
    }

    void Move()
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right   = cameraTransform.right;

        forward.y = 0;
        right.y   = 0;

        forward.Normalize();
        right.Normalize();

        Vector3 move           = forward * moveInput.y + right * moveInput.x;
        Vector3 targetVelocity = move * moveSpeed;
        Vector3 velocity       = rb.linearVelocity;
        Vector3 velocityChange = targetVelocity - new Vector3(velocity.x, 0, velocity.z);

        rb.AddForce(velocityChange * acceleration * controlMultiplier, ForceMode.Acceleration);
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump()
    {
        if (CurrentJumps < MaxJump)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            CurrentJumps++;
        }
    }

    public void OnToggleMagnet()
    {
        if (magnetController == null) return;

        // Check if magnet toggle is unlocked
        if (upgradeManager != null)
        {
            Upgrade toggleUpgrade = upgradeManager.GetUpgrade("magnet_toggle");
            if (toggleUpgrade == null || toggleUpgrade.currentLevel == 0)
            {
                Debug.Log("Magnet toggle not unlocked yet.");
                return;
            }
        }

        if (magnetController.currentEnabled == MagnetController.Enabled.ON)
            magnetController.TurnOff();
        else
            magnetController.TurnOn();
    }

    public void OnOpenUpgrades()
    {
        if (upgradeManager != null)
            upgradeManager.ToggleUpgradeMenu();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contacts[0].normal.y > 0.5f)
            CurrentJumps = 0;
    }
}