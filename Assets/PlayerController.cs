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

    [Header("Gravity")]
    public float extraGravity   = 20f;
    public float fallMultiplier = 2.5f;
    public float riseMultiplier = 1.2f;


    [HideInInspector] public float controlMultiplier = 1f;

    public Transform cameraTransform;

    [Header("References")]
    public MagnetController magnetController;
    public UpgradeManager upgradeManager;

    [Header("Keybinds")]
    public KeyCode magnetToggleKey = KeyCode.Q;

    [Header("Sprint")]
    public float sprintSpeedBonus = 5f;   // added on top of moveSpeed when sprinting
    [SerializeField] private bool isSprinting = false;

    private Vector2 moveInput;
    private Rigidbody rb;

    [Header("Magnetism")]
    public MagnetPolarity polarity = MagnetPolarity.Repel;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;

        rb = GetComponent<Rigidbody>();

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        rb.linearDamping  = drag;
        rb.angularDamping = 0.5f;
    }

    void FixedUpdate()
{
    if (rb.linearDamping == 0f && rb.linearVelocity.magnitude < moveSpeed * 1.5f)
        rb.linearDamping = drag;

    Move();
    ApplyGravity();
}

    void ApplyGravity()
    {
        if (rb.linearVelocity.y < 0f)
            rb.AddForce(Vector3.down * extraGravity * fallMultiplier, ForceMode.Acceleration);
        else if (rb.linearVelocity.y > 0f)
            rb.AddForce(Vector3.down * extraGravity * riseMultiplier, ForceMode.Acceleration);
        else
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

        Vector3 move = forward * moveInput.y + right * moveInput.x;

        float currentSpeed     = isSprinting ? moveSpeed + sprintSpeedBonus : moveSpeed;
        Vector3 targetVelocity = move * currentSpeed;
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        // Don't brake if we're moving faster than our own max speed (e.g. launched)
        if (horizontalVelocity.magnitude > currentSpeed && move.magnitude < 0.1f)
            return;

        Vector3 velocityChange = targetVelocity - horizontalVelocity;

        if (controlMultiplier > 0.05f)
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

    // Called automatically by Input System — bind a "Sprint" action to Left Shift
   public void OnSprint(InputValue value)
    {
        isSprinting = value.isPressed;
    }

    public void OnToggleMagnet()
    {
        if (magnetController == null) return;

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