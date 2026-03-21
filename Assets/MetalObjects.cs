using UnityEngine;

public class MetalObject : MonoBehaviour
{
    public enum ObjectType
    {
        Prop,
        Enemy
    }

    [Header("Type")]
    public ObjectType objectType = ObjectType.Prop;

    [Header("Magnet Settings")]
    public float magneticStrength = 1000f;
    public float range = 10f;

    public bool usePolarity = true;
    public MagnetPolarity polarity = MagnetPolarity.Attract;

    [HideInInspector] public Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        ApplyMagnetism();
    }

    void ApplyMagnetism()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range);

        foreach (Collider hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            MetalObject other = hit.GetComponent<MetalObject>();
            if (other == null || other.rb == null) continue;

            Vector3 toOther = other.transform.position - transform.position;
            float distance = toOther.magnitude;

            if (distance < 0.05f) continue;

            Vector3 dir = toOther.normalized;

            bool attract = (usePolarity && other.usePolarity)
                ? polarity != other.polarity
                : true;

            Vector3 forceDir = attract ? dir : -dir;

            float total = magneticStrength + other.magneticStrength;

            float thisFactor = other.magneticStrength / total;
            float otherFactor = magneticStrength / total;

            float distance01 = Mathf.Clamp01(distance / range);
            float falloff = 1f - distance01;
            falloff *= falloff;

            float force = 50f * falloff;

            rb.AddForce(forceDir * force * thisFactor, ForceMode.Acceleration);
            other.rb.AddForce(-forceDir * force * otherFactor, ForceMode.Acceleration);
        }
    }
}