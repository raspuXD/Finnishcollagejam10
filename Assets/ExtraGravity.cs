using UnityEngine;

public class ExtraGravity : MonoBehaviour
{
    public float extraGravity = 10f;
    private Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
    }
}
