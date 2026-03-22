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

    [Header("Force Tuning")]
    public float baseForce = 20f;
    public float strengthInfluence = 0.002f; // how much strength affects force
    public float falloffPower = 2f; // higher = sharper dropoff

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

            if (!(usePolarity || other.usePolarity))
                continue;

            bool attract = true;

            if (usePolarity && other.usePolarity)
            {
                attract = polarity != other.polarity;
            }

            Vector3 forceDir = attract ? dir : -dir;

            // --- NEW: strength affects actual force ---
            float avgStrength = (magneticStrength + other.magneticStrength) * 0.5f;

            // logarithmic-ish scaling (prevents tiny differences from exploding)
            float strengthFactor = Mathf.Sqrt(avgStrength);

            // OR use this if you want even softer:
            // float strengthFactor = Mathf.Log10(avgStrength + 1f) * 10f;

            float strengthForce = strengthFactor * strengthInfluence;

            // distance falloff
            float distance01 = Mathf.Clamp01(distance / range);
            float falloff = Mathf.Pow(1f - distance01, falloffPower);

            float finalForce = baseForce + strengthForce * falloff;

            // keep your "who moves more" logic
            float ratio = magneticStrength / (other.magneticStrength + 0.001f);

            // soften it
            float influence = Mathf.Pow(ratio, 0.5f); // sqrt softens differences

            float thisFactor = 1f / (1f + influence);
            float otherFactor = 1f - thisFactor;

            rb.AddForce(forceDir * finalForce * thisFactor, ForceMode.Acceleration);
            other.rb.AddForce(-forceDir * finalForce * otherFactor, ForceMode.Acceleration);
        }
    }
}