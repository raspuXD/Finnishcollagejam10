using UnityEngine;
using System.Collections;
public class MagnetController : MonoBehaviour
{
    [Header("Magnet Settings")]
    public float magneticStrength = 500f;
    public float baseForce = 60f;
    public float range = 20f;

    [Header("Feel Tuning")]
    public float repelBoost = 12f;
    public float snapDistance = 3f;

    [Header("Polarity Switch Settings")]
    public float switchCooldown = 0.25f; // time between allowed switches

    private float lastSwitchTime = -999f;

    public MagnetPolarity currentPolarity = MagnetPolarity.Attract;

    public enum Enabled { ON, OFF }
    public Enabled currentEnabled = Enabled.OFF;

    public Animator animator;
    private Rigidbody rb;
    private PlayerController playerController;

    [Header("Visuals")]
    public Renderer targetRenderer;
    public Gradient attractGradient;
    public Gradient repelGradient;
    public float colorTransitionTime = 0.3f;

    private Coroutine colorRoutine;

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

    bool CanSwitch()
    {
        return Time.time >= lastSwitchTime + switchCooldown;
    }

    void RegisterSwitch()
    {
        lastSwitchTime = Time.time;

        // Optional: clear triggers to avoid stacking issues
        animator.ResetTrigger("ToAttract");
        animator.ResetTrigger("ToRepel");
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
        if (!CanSwitch())
            return;

        RegisterSwitch();

        currentPolarity = MagnetPolarity.Attract;
        currentEnabled = Enabled.ON;

        animator.SetTrigger("ToAttract");

        if (colorRoutine != null)
            StopCoroutine(colorRoutine);

        colorRoutine = StartCoroutine(AnimateColor(attractGradient));
    }
    IEnumerator AnimateColor(Gradient gradient)
    {
        float time = 0f;

        while (time < colorTransitionTime)
        {
            float t = time / colorTransitionTime;

            Color col = gradient.Evaluate(t);
            targetRenderer.material.color = col;

            time += Time.deltaTime;
            yield return null;
        }

        // ensure final color
        targetRenderer.material.color = gradient.Evaluate(1f);
    }
    public void OnRepel()
    {
        if (!CanSwitch())
            return;

        RegisterSwitch();

        currentPolarity = MagnetPolarity.Repel;
        currentEnabled = Enabled.ON;

        animator.SetTrigger("ToRepel");

        if (colorRoutine != null)
            StopCoroutine(colorRoutine);

        colorRoutine = StartCoroutine(AnimateColor(repelGradient));
    }
}