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
    public float switchCooldown = 0.25f;
    private float lastSwitchTime = -999f;

    public MagnetPolarity currentPolarity = MagnetPolarity.Attract;

    public enum Enabled { ON, OFF }
    public Enabled currentEnabled = Enabled.OFF;

    public Animator animator;
    private Rigidbody rb;
    private PlayerController playerController;

    public float externalDisableUntil = 0f;

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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(this.gameObject.transform.position, range);
    }
    // ── Toggle ─────────────────────────────────────────────────────

    public void TurnOn()
    {
        currentEnabled = Enabled.ON;

        // Restore last polarity visual
        if (colorRoutine != null) StopCoroutine(colorRoutine);
        colorRoutine = StartCoroutine(AnimateColor(
            currentPolarity == MagnetPolarity.Attract ? attractGradient : repelGradient
        ));

        animator.SetTrigger(currentPolarity == MagnetPolarity.Attract ? "ToAttract" : "ToRepel");
    }

    public void TurnOff()
    {
        currentEnabled = Enabled.OFF;

        if (colorRoutine != null) StopCoroutine(colorRoutine);

        // Grey out the renderer to show it's off
        if (targetRenderer != null)
            targetRenderer.material.color = Color.gray;

        if (playerController != null)
            playerController.controlMultiplier = 1f;
    }

    // ── Polarity ───────────────────────────────────────────────────

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

    public void OnAttract()
    {
        if (!CanSwitch()) return;
        if (currentPolarity == MagnetPolarity.Attract) return;

        RegisterSwitch();

        currentPolarity = MagnetPolarity.Attract;
        currentEnabled  = Enabled.ON;

        animator.SetTrigger("ToAttract");

        if (colorRoutine != null) StopCoroutine(colorRoutine);
        colorRoutine = StartCoroutine(AnimateColor(attractGradient));
    }

    public void OnRepel()
    {
        if (!CanSwitch()) return;
        if (currentPolarity == MagnetPolarity.Repel) return;

        RegisterSwitch();

        currentPolarity = MagnetPolarity.Repel;
        currentEnabled  = Enabled.ON;

        animator.SetTrigger("ToRepel");

        if (colorRoutine != null) StopCoroutine(colorRoutine);
        colorRoutine = StartCoroutine(AnimateColor(repelGradient));
    }

    // ── Magnetism ──────────────────────────────────────────────────

    void ApplyMagnetism()
    {
        if (Time.time < externalDisableUntil)
        return;
        
        Collider[] hits    = Physics.OverlapSphere(transform.position, range);
        
        bool magnetActive  = false;

        foreach (Collider hit in hits)
        {
            MetalObject metal = hit.GetComponent<MetalObject>();
            if (metal == null) continue;

            Rigidbody targetRb = metal.rb;
            if (targetRb == null) continue;

            Vector3 toCenter = hit.transform.position - transform.position;
            float distance   = toCenter.magnitude;
            if (distance < 0.05f) continue;

            Vector3 dir = toCenter.normalized;

            bool attract;

            if (metal.usePolarity)
            {
                if (metal.objectType == MetalObject.ObjectType.Prop)
                {
                    // PROPS: same polarity attracts
                    attract = currentPolarity == metal.polarity;
                }
                else // Enemy
                {
                    // ENEMIES: opposite polarity attracts
                    attract = currentPolarity != metal.polarity;
                }
            }
            else
            {
                attract = currentPolarity == MagnetPolarity.Attract;
            }

            Vector3 forceDir       = attract ? dir : -dir;
            float targetStrength   = metal.magneticStrength;
            float distance01       = Mathf.Clamp01(distance / range);
            float falloff          = 1f - distance01;
            falloff               *= falloff;
            float force            = baseForce * falloff + 10f;
            float strengthFactor   = magneticStrength / (magneticStrength + targetStrength);
            strengthFactor         = Mathf.Clamp(strengthFactor, 0.1f, 1f);

            targetRb.AddForce(-forceDir * force * strengthFactor, ForceMode.Acceleration);

            float feedback = 1f - strengthFactor;
            rb.AddForce(forceDir * force * 0.2f * feedback, ForceMode.Acceleration);

            magnetActive = true;

            if (distance < snapDistance)
                targetRb.AddForce(-forceDir * force * 2f * strengthFactor, ForceMode.Acceleration);

            if (!attract && distance < 4f)
                targetRb.linearVelocity += -forceDir * (repelBoost * strengthFactor);
        }

        if (playerController != null)
            playerController.controlMultiplier = magnetActive ? 0.5f : 1f;
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