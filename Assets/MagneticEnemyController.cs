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

    [Header("Ground Check")]
    public float groundCheckDistance = 1.2f;
    public LayerMask groundLayer;

    [Header("Air Behavior")]
    public float airDrag = 0.2f;

    [Header("Magnetic Control")]
    public float insideControl        = 0.05f;
    public float controlLoseSpeed     = 10f;
    public float controlRecoverSpeed  = 0.5f;
    public float recoverSpeedThreshold = 4f;

    [Header("Wall Climbing")]
    public float wallCheckDistance    = 0.8f;
    public float wallClimbSpeed       = 4f;
    public float wallGravityScale     = 0.1f;
    public float wallAttachForce      = 15f;
    public float wallDetectAngle      = 60f;
    public LayerMask wallLayer;

    private Rigidbody rb;
    private bool  isGrounded;
    private bool  isOnWall;
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

        bool inMagnetRange  = IsInMagnetRange();
        float currentSpeed  = rb.linearVelocity.magnitude;
        bool travellingFast = currentSpeed > recoverSpeedThreshold;

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

        if (isOnWall)
        {
            WallMovement(controlFactor);
        }
        else if (isGrounded)
        {
            rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
            GroundMovement(controlFactor);
        }
        else
        {
            rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
            AirMovement();
        }

        LimitSpeed();
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
        Debug.Log("OnWall: " + isOnWall);
        isOnWall   = false;
        wallNormal = Vector3.zero;

        float bestAngle = 0f;

        // Directions to scan for walls
        Vector3[] directions =
        {
            transform.forward,
            -transform.forward,
            transform.right,
            -transform.right,
            (transform.forward + transform.right).normalized,
            (transform.forward - transform.right).normalized,
            (-transform.forward + transform.right).normalized,
            (-transform.forward - transform.right).normalized
        };

        foreach (var dir in directions)
        {
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, wallCheckDistance, wallLayer))
            {
                Vector3 normal = hit.normal;

                float angle = Vector3.Angle(normal, Vector3.up);

                // Reject floor/ceiling
                if (angle < wallDetectAngle || angle > 180f - wallDetectAngle)
                    continue;

                if (angle > bestAngle)
                {
                    bestAngle  = angle;
                    wallNormal = normal;
                }

                Debug.DrawRay(hit.point, hit.normal, Color.blue);
            }
        }

        if (wallNormal != Vector3.zero)
        {
            isOnWall = true;

            wallSurfaceUp = Vector3.ProjectOnPlane(
                (player.position - transform.position).normalized,
                wallNormal
            ).normalized;

            Debug.DrawRay(transform.position, wallNormal, Color.cyan);
            Debug.DrawRay(transform.position, wallSurfaceUp * 2f, Color.yellow);
        }
    }

    bool IsInMagnetRange()
    {
        if (magnet.currentEnabled == MagnetController.Enabled.OFF)
            return false;

        float dist = Vector3.Distance(transform.position, player.position);
        return dist <= magnet.range;
    }

    // ── Movement ────────────────────────────────────────────

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Hit: " + collision.gameObject.name);
        Debug.Log("Contact count: " + collision.contactCount);

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

        Vector3 gravityOnWall = Vector3.down * extraGravity * wallGravityScale;
        rb.AddForce(gravityOnWall, ForceMode.Acceleration);

        rb.AddForce(-wallNormal * wallAttachForce, ForceMode.Acceleration);

        Vector3 desiredVelocity = wallSurfaceUp * wallClimbSpeed;

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
            horizontal = horizontal.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(horizontal.x, rb.linearVelocity.y, horizontal.z);
        }
    }
}