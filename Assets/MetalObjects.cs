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

    Renderer rend;
    MaterialPropertyBlock mpb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    void FixedUpdate()
    {
        ApplyMagnetism();
        UpdateShader();
    }

    void UpdateShader()
    {
        rend.GetPropertyBlock(mpb);

        // Polarity value (-1 or +1)
        float polarityValue = polarity == MagnetPolarity.Attract ? -1f : 1f;

        // Tint strength ONLY when polarity is used
        float tint = usePolarity ? 0.3f : 0f;

        mpb.SetFloat("_Polarity", polarityValue);
        mpb.SetFloat("_TintStrength", tint);

        rend.SetPropertyBlock(mpb);
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

            if (!usePolarity || !other.usePolarity)
                continue;

            bool attract;

            if (objectType == ObjectType.Prop || other.objectType == ObjectType.Prop)
                attract = polarity == other.polarity;
            else
                attract = polarity != other.polarity;

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