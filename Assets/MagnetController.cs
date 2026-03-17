using UnityEngine;

public class MagnetController : MonoBehaviour
{
    [Header("Magnet Settings")]
    public float magneticStrength = 500f;
    public float baseForce = 60f;
    public float range = 20f;

    [Header("Feel Tuning")]
    public float repelBoost = 12f;
    public float snapDistance = 3f;

    public enum Polarity { Attract, Repel }
    public Polarity currentPolarity = Polarity.Attract;

    private Rigidbody rb;
    private PlayerController playerController;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();
    }

    void FixedUpdate()
    {
        ApplyMagnetism();
    }

    void ApplyMagnetism()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range);

        bool magnetActive = false;

        foreach (Collider hit in hits)
        {
            MetalObject metal = hit.GetComponent<MetalObject>();
            if (metal == null) continue;

            Rigidbody targetRb = metal.rb;
            if (targetRb == null) continue;

            // Direction to object center
            Vector3 toCenter = hit.transform.position - transform.position;
            float distance = toCenter.magnitude;

            if (distance < 0.05f) continue;

            Vector3 dir = toCenter.normalized;

            // 🔥 Clean direction logic (FIXED)
            Vector3 forceDir = currentPolarity == Polarity.Attract ? dir : -dir;

            float targetStrength = metal.magneticStrength;

            // Smooth falloff
            float distance01 = Mathf.Clamp01(distance / range);
            float falloff = 1f - distance01;
            falloff *= falloff;

            float force = baseForce * falloff;

            // Constant pull (no dead zone)
            force += 10f;

            // 🔥 Strength scaling (mass-like)
            float strengthRatio = magneticStrength / targetStrength;
            strengthRatio = Mathf.Clamp(strengthRatio, 0.2f, 3f);

            float playerRatio = targetStrength / magneticStrength;
            playerRatio = Mathf.Clamp(playerRatio, 0.5f, 3f);

            // 🔥 ORIGINAL "who moves" logic (preserved)
            if (targetStrength < magneticStrength)
            {
                // Object moves (opposite direction)
                targetRb.AddForce(-forceDir * force * strengthRatio, ForceMode.Acceleration);
            }
            else
            {
                // Player moves
                rb.AddForce(forceDir * force * playerRatio, ForceMode.Acceleration);

                // Flying feel
                rb.linearVelocity += forceDir * (force * 0.02f * playerRatio);

                magnetActive = true;

                // Snap when close
                if (distance < snapDistance)
                {
                    rb.AddForce(forceDir * force * 3f * playerRatio, ForceMode.Acceleration);
                    rb.linearVelocity += forceDir * 5f * playerRatio;
                }

                // Repel boost
                if (currentPolarity == Polarity.Repel && distance < 4f)
                {
                    float boost = repelBoost * playerRatio;
                    rb.linearVelocity += forceDir * boost;
                }
            }
        }

        // Reduce control while magnet active
        if (playerController != null)
        {
            playerController.controlMultiplier = magnetActive ? 0.5f : 1f;
        }
    }

    public void OnAttract()
    {
        currentPolarity = Polarity.Attract;
    }

    public void OnRepel()
    {
        currentPolarity = Polarity.Repel;
    }
}