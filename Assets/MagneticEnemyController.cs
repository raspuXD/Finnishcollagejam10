using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MetalObject))]
public class MagneticEnemyController : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    private MagnetController magnet;

    [Header("Movement")]
    public float maxSpeed = 6f;
    public float acceleration = 8f;
    public float extraGravity = 10f;

    [Header("Ground Check")]
    public float groundCheckDistance = 1.2f;
    public LayerMask groundLayer;

    [Header("Air Behavior")]
    public float airDrag = 0.2f;

    [Header("Magnetic Control")]
    public float insideControl = 0.05f;      // tiny control inside field
    public float controlLoseSpeed = 10f;      // fast drop when entering field
    public float controlRecoverSpeed = 0.5f;  // slow regain after slowing down
    public float recoverSpeedThreshold = 4f;  // must be below this speed to recover

    private Rigidbody rb;
    private bool isGrounded;

    private float controlFactor = 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindWithTag("Player");
            if (found != null)
                player = found.transform;
        }

        if (player != null)
            magnet = player.GetComponent<MagnetController>();
    }

    void FixedUpdate()
    {
        if (player == null || magnet == null)
            return;

        CheckGround();

        bool inMagnetRange = IsInMagnetRange();

        rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);

        // Use total velocity so fast vertical falls also block recovery
        float currentSpeed = rb.linearVelocity.magnitude;
        bool travellingFast = currentSpeed > recoverSpeedThreshold;

        float targetControl;
        float speed;

        if (inMagnetRange)
        {
            // Inside field: rapidly lose control
            targetControl = insideControl;
            speed = controlLoseSpeed;
        }
        else if (travellingFast)
        {
            // Flung out and still moving fast: freeze control where it is
            targetControl = controlFactor;
            speed = 0f;
        }
        else
        {
            // Slowed down enough: gradually recover control
            targetControl = 1f;
            speed = controlRecoverSpeed;
        }

        controlFactor = Mathf.MoveTowards(
            controlFactor,
            targetControl,
            speed * Time.fixedDeltaTime
        );

        // Movement
        if (isGrounded)
        {
            GroundMovement(controlFactor);
        }
        else
        {
            AirMovement();
        }

        LimitSpeed();
    }

    void CheckGround()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        isGrounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
        Debug.DrawRay(transform.position, Vector3.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
    }

    bool IsInMagnetRange()
    {
        if (magnet.currentEnabled == MagnetController.Enabled.OFF)
            return false;

        float dist = Vector3.Distance(transform.position, player.position);
        return dist <= magnet.range;
    }

    void GroundMovement(float control)
    {
        if (control < 0.01f)
            return;

        Vector3 toPlayer = (player.position - transform.position);
        toPlayer.y = 0f;

        if (toPlayer.magnitude < 0.1f) return;

        Vector3 dir = toPlayer.normalized;

        Vector3 desiredVelocity = dir * maxSpeed;

        Vector3 velocityChange = (desiredVelocity - rb.linearVelocity);
        velocityChange.y = 0f;

        Vector3 force = velocityChange * acceleration * control;

        rb.AddForce(force, ForceMode.Acceleration);

        if (control > 0.1f && dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                control * 10f * Time.fixedDeltaTime
            );
        }
    }

    void AirMovement()
    {
        /*
        Vector3 vel = rb.linearVelocity;
        Vector3 horizontal = new Vector3(vel.x, 0f, vel.z);
        horizontal *= (1f - airDrag * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(horizontal.x, vel.y, horizontal.z);
        */
    }

    void LimitSpeed()
    {
        // Only limit speed when enemy has meaningful control (prevents killing flings)
        if (controlFactor < 0.5f)
            return;

        Vector3 horizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (horizontal.magnitude > maxSpeed)
        {
            horizontal = horizontal.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(horizontal.x, rb.linearVelocity.y, horizontal.z);
        }
    }
}