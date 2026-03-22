using UnityEngine;
using System.Collections;

public class MagnetController : MonoBehaviour
{
    [Header("Magnet Settings")]
    public float magneticStrength = 500f;
    public float baseForce = 40f;
    public float range = 20f;

    [Header("Force Tuning")]
    public float strengthInfluence = 0.002f;
    public float falloffPower = 2f;

    [Header("Feel Tuning")]
    public float repelMultiplier = 1.5f;
    public float attractMultiplier = 1f;
    public float snapDistance = 3f;

    [Header("Polarity Switch Settings")]
    public float switchCooldown = 0.25f;

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
            if (metal == null || metal.rb == null) continue;

            Vector3 toTarget = hit.transform.position - transform.position;
            float distance = toTarget.magnitude;

            if (distance < 0.05f) continue;

            Vector3 dir = toTarget.normalized;

            bool attract = currentPolarity != metal.polarity;
            Vector3 forceDir = attract ? dir : -dir;

            // --- FORCE ---
            float distance01 = Mathf.Clamp01(distance / range);
            float falloff = Mathf.Pow(1f - distance01, falloffPower);

            float avgStrength = (magneticStrength + metal.magneticStrength) * 0.5f;
            float strengthFactor = Mathf.Sqrt(avgStrength);

            float force = (baseForce + strengthFactor * strengthInfluence) * falloff;

            force *= attract ? attractMultiplier : repelMultiplier;

            // --- SOFT INFLUENCE (THIS FIXES YOUR ISSUE) ---
            float ratio = magneticStrength / (metal.magneticStrength + 0.001f);
            float influence = Mathf.Pow(ratio, 0.5f); // soften differences

            // --- influence split ---
            float playerFactor = 1f / (1f + influence);
            float objectFactor = 1f - playerFactor;

            // --- resistance (KEY FIX) ---
            float playerResistance = 1f;

            // ratio: how strong object is compared to player
            float relativeStrength = metal.magneticStrength / (magneticStrength + 0.001f);

            // if object is weaker, heavily reduce its effect on player
            playerResistance = Mathf.Pow(relativeStrength, 2f);

            // apply forces
            rb.AddForce(forceDir * force * playerFactor * playerResistance, ForceMode.Acceleration);
            metal.rb.AddForce(-forceDir * force * objectFactor, ForceMode.Acceleration);

            // feedback
            if (playerFactor > 0.6f)
                magnetActive = true;

            if (distance < snapDistance && attract)
            {
                rb.AddForce(forceDir * force * playerFactor * 2f, ForceMode.Acceleration);
            }
        }

        if (playerController != null)
        {
            playerController.controlMultiplier = magnetActive ? 0.5f : 1f;
        }
    }

    public void OnAttract()
    {
        if (!CanSwitch()) return;

        RegisterSwitch();

        currentPolarity = MagnetPolarity.Attract;
        currentEnabled = Enabled.ON;

        animator.SetTrigger("ToAttract");

        if (colorRoutine != null)
            StopCoroutine(colorRoutine);

        colorRoutine = StartCoroutine(AnimateColor(attractGradient));
    }

    public void OnRepel()
    {
        if (!CanSwitch()) return;

        RegisterSwitch();

        currentPolarity = MagnetPolarity.Repel;
        currentEnabled = Enabled.ON;

        animator.SetTrigger("ToRepel");

        if (colorRoutine != null)
            StopCoroutine(colorRoutine);

        colorRoutine = StartCoroutine(AnimateColor(repelGradient));
    }

    IEnumerator AnimateColor(Gradient gradient)
    {
        float time = 0f;

        while (time < colorTransitionTime)
        {
            float t = time / colorTransitionTime;
            targetRenderer.material.color = gradient.Evaluate(t);

            time += Time.deltaTime;
            yield return null;
        }

        targetRenderer.material.color = gradient.Evaluate(1f);
    }
}