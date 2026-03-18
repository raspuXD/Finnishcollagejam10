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

    public MagnetPolarity currentPolarity = MagnetPolarity.Attract;

    public enum Enabled { ON, OFF }
    public Enabled currentEnabled = Enabled.OFF;

    private Rigidbody rb;
    private PlayerController playerController;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();
    }

    void FixedUpdate()
    {
        if (currentEnabled == Enabled.OFF)
        {
            if (playerController != null)
                playerController.controlMultiplier = 1f;

            return;
        }

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

            Vector3 toCenter = hit.transform.position - transform.position;
            float distance = toCenter.magnitude;

            if (distance < 0.05f) continue;

            Vector3 dir = toCenter.normalized;

            // 🔥 Polarity interaction
            bool attract = currentPolarity != metal.polarity;
            Vector3 forceDir = attract ? dir : -dir;

            float targetStrength = metal.magneticStrength;

            float distance01 = Mathf.Clamp01(distance / range);
            float falloff = 1f - distance01;
            falloff *= falloff;

            float force = baseForce * falloff + 10f;

            float strengthRatio = Mathf.Clamp(magneticStrength / targetStrength, 0.2f, 3f);
            float playerRatio = Mathf.Clamp(targetStrength / magneticStrength, 0.5f, 3f);

            if (targetStrength < magneticStrength)
            {
                targetRb.AddForce(-forceDir * force * strengthRatio, ForceMode.Acceleration);
            }
            else
            {
                rb.AddForce(forceDir * force * playerRatio, ForceMode.Acceleration);
                rb.linearVelocity += forceDir * (force * 0.02f * playerRatio);

                magnetActive = true;

                if (distance < snapDistance)
                {
                    rb.AddForce(forceDir * force * 3f * playerRatio, ForceMode.Acceleration);
                    rb.linearVelocity += forceDir * 5f * playerRatio;
                }

                if (!attract && distance < 4f)
                {
                    rb.linearVelocity += forceDir * (repelBoost * playerRatio);
                }
            }
        }

        if (playerController != null)
        {
            playerController.controlMultiplier = magnetActive ? 0.5f : 1f;
        }
    }

    public void OnAttract()
    {
        currentPolarity = MagnetPolarity.Attract;
        currentEnabled = Enabled.ON;
    }

    public void OnRepel()
    {
        currentPolarity = MagnetPolarity.Repel;
        currentEnabled = Enabled.ON;
    }
}