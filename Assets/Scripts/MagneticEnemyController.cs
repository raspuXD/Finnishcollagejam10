using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MetalObject))]
public class MagneticEnemyController : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    private MagnetController magnet;

    [Header("Movement")]
    public float maxSpeed     = 6f;
    public float acceleration = 8f;
    public float extraGravity = 10f;
    public float fallMultiplier = 2.5f;
    public float riseMultiplier = 1.2f;

    [Header("Ground Check")]
    public float groundCheckDistance = 1.2f;
    public LayerMask groundLayer;

    [Header("Air Behavior")]
    public float airDrag = 0.2f;

    [Header("Magnetic Control")]
    public float insideControl         = 0.05f;
    public float controlLoseSpeed      = 10f;
    public float controlRecoverSpeed   = 0.5f;
    public float recoverSpeedThreshold = 4f;

    [Header("Wall Climbing")]
    [SerializeField] private bool wasOnWall;
    public float wallCheckDistance = 0.8f;
    public float wallClimbSpeed    = 4f;
    public float wallGravityScale  = 0.1f;
    public float wallAttachForce   = 15f;
    public float wallDetectAngle   = 60f;
    public LayerMask wallLayer;

    private Rigidbody rb;
    private bool    isGrounded;
    private bool    isOnWall;
    private Vector3 wallNormal;
    private Vector3 wallSurfaceUp;

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
        if (player == null || magnet == null) return;

        CheckGround();
        CheckWall();

        bool  inMagnetRange  = IsInMagnetRange();
        float currentSpeed   = rb.linearVelocity.magnitude;
        bool  travellingFast = currentSpeed > recoverSpeedThreshold;

        float targetControl;
        float speed;

        if (inMagnetRange)
        {
            targetControl = insideControl;
            speed         = controlLoseSpeed;
        }
        else if (travellingFast)
        {
            targetControl = controlFactor;
            speed         = 0f;
        }
        else
        {
            targetControl = 1f;
            speed         = controlRecoverSpeed;
        }

        controlFactor = Mathf.MoveTowards(
            controlFactor,
            targetControl,
            speed * Time.fixedDeltaTime
        );

        if (wasOnWall && !isOnWall)
            rb.AddForce(wallNormal * wallAttachForce, ForceMode.Impulse);

        wasOnWall = isOnWall;

        if (isOnWall)
        {
            WallMovement(controlFactor);
        }
        else if (isGrounded)
        {
            ApplyGravity();
            GroundMovement(controlFactor);
        }
        else
        {
            ApplyGravity();
            AirMovement();
        }

        LimitSpeed();
    }

    // ── Gravity ───────────────────────────────────────────────────

    void ApplyGravity()
    {
        if (rb.linearVelocity.y < 0f)
            rb.AddForce(Vector3.down * extraGravity * fallMultiplier, ForceMode.Acceleration);
        else if (rb.linearVelocity.y > 0f)
            rb.AddForce(Vector3.down * extraGravity * riseMultiplier, ForceMode.Acceleration);
        else
            rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
    }

    // ── Ground / Wall Detection ───────────────────────────────────

    void CheckGround()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        isGrounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, groundLayer);

        Debug.DrawRay(origin, Vector3.down * groundCheckDistance,
            isGrounded ? Color.green : Color.red);
    }

    void CheckWall()
    {
        isOnWall   = false;
        wallNormal = Vector3.zero;

        Vector3 toPlayer = (player.position - transform.position).normalized;

        Vector3 origin = transform.position;
        if (wallNormal != Vector3.zero)
            origin += wallNormal * 0.15f;

        Debug.DrawRay(origin, toPlayer * wallCheckDistance, Color.white);

        if (Physics.Raycast(origin, toPlayer, out RaycastHit hit, wallCheckDistance, wallLayer))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);

            if (angle >= wallDetectAngle && angle <= 180f - wallDetectAngle)
            {
                wallNormal    = hit.normal;
                isOnWall      = true;
                wallSurfaceUp = Vector3.ProjectOnPlane(toPlayer, wallNormal).normalized;

                Debug.DrawRay(hit.point, hit.normal,          Color.blue);
                Debug.DrawRay(origin,    wallNormal,           Color.cyan);
                Debug.DrawRay(origin,    wallSurfaceUp * 2f,   Color.yellow);
            }
        }
    }

    bool IsInMagnetRange()
    {
        if (magnet.currentEnabled == MagnetController.Enabled.OFF)
            return false;

        float dist = Vector3.Distance(transform.position, player.position);
        return dist <= magnet.range;
    }

    // ── Movement ──────────────────────────────────────────────────

    void OnCollisionEnter(Collision collision)
    {

        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.DrawRay(contact.point, contact.normal, Color.red, 1f);
        }
    }

    void GroundMovement(float control)
    {
        if (control < 0.01f) return;

        Vector3 toPlayer = (player.position - transform.position);
        toPlayer.y = 0f;

        if (toPlayer.magnitude < 0.1f) return;

        Vector3 dir             = toPlayer.normalized;
        Vector3 desiredVelocity = dir * maxSpeed;
        Vector3 velocityChange  = desiredVelocity - rb.linearVelocity;
        velocityChange.y = 0f;

        rb.AddForce(velocityChange * acceleration * control, ForceMode.Acceleration);

        if (control > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                control * 10f * Time.fixedDeltaTime
            );
        }
    }

    void WallMovement(float control)
    {
        if (control < 0.01f) return;

        // Wall uses its own reduced gravity, no fall/rise multiplier
        Vector3 gravityOnWall = Vector3.down * extraGravity * wallGravityScale;
        rb.AddForce(gravityOnWall, ForceMode.Acceleration);

        if (control > 0.5f)
            rb.AddForce(-wallNormal * wallAttachForce, ForceMode.Acceleration);

        Vector3 desiredVelocity     = wallSurfaceUp * wallClimbSpeed;
        Vector3 currentWallVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, wallNormal);
        Vector3 velocityChange      = (desiredVelocity - currentWallVelocity) * acceleration * control;

        rb.AddForce(velocityChange, ForceMode.Acceleration);

        if (wallSurfaceUp != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(wallSurfaceUp, -wallNormal);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                control * 8f * Time.fixedDeltaTime
            );
        }

        Debug.DrawRay(transform.position, wallSurfaceUp * 2f, Color.yellow);
    }

    void AirMovement()
    {
        // Reserved for future air control
    }

    void LimitSpeed()
    {
        if (controlFactor < 0.5f) return;

        Vector3 horizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (horizontal.magnitude > maxSpeed)
        {
            horizontal        = horizontal.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(horizontal.x, rb.linearVelocity.y, horizontal.z);
        }
    }
}