using UnityEngine;

public class ExtraGravity : MonoBehaviour
{
    public float extraGravity    = 10f;
    public float fallMultiplier  = 2.5f;  // ramps up when falling
    public float riseMultiplier  = 1.2f;  // slight drag on the way up

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (rb.linearVelocity.y < 0f)
        {
            // Falling — ramp gravity harder the faster we drop
            rb.AddForce(Vector3.down * extraGravity * fallMultiplier, ForceMode.Acceleration);
        }
        else if (rb.linearVelocity.y > 0f)
        {
            // Rising — light extra pull to cut floatiness at the peak
            rb.AddForce(Vector3.down * extraGravity * riseMultiplier, ForceMode.Acceleration);
        }
        else
        {
            rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
        }
    }
}