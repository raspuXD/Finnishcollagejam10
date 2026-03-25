using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RegenVFX : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;
    public Volume postProcessVolume;

    [Header("Pulse Settings")]
    public float pulseIntensity  = 0.4f;  // peak vignette intensity on pulse
    public float pulseInTime     = 0.1f;  // seconds to reach peak
    public float pulseOutTime    = 0.4f;  // seconds to fade back down
    public float idleIntensity   = 0.15f; // resting intensity while regen is active

    private Vignette vignette;
    private Coroutine pulseRoutine;
    private bool isActive = false;

    void Awake()
    {
        if (postProcessVolume != null)
            postProcessVolume.profile.TryGet(out vignette);

        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHeal.AddListener(OnHeal);
            playerHealth.onHealthChanged.AddListener(OnHealthChanged);
        }
    }

    void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHeal.RemoveListener(OnHeal);
            playerHealth.onHealthChanged.RemoveListener(OnHealthChanged);
        }
    }

    void OnHeal(float healthNormalized)
    {
        // Activate on first heal
        if (!isActive)
        {
            isActive = true;
            gameObject.SetActive(true);

            if (vignette != null)
                vignette.intensity.value = idleIntensity;
        }

        // Pulse on every heal tick
        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        pulseRoutine = StartCoroutine(Pulse());
    }

    void OnHealthChanged(float healthNormalized)
    {
        // Deactivate when full health
        if (healthNormalized >= 1f && isActive)
        {
            isActive = false;

            if (pulseRoutine != null)
                StopCoroutine(pulseRoutine);

            if (vignette != null)
                vignette.intensity.value = 0f;

            gameObject.SetActive(false);
        }
    }

    IEnumerator Pulse()
    {
        if (vignette == null) yield break;

        // Ramp up
        float t = 0f;
        float start = vignette.intensity.value;

        while (t < pulseInTime)
        {
            vignette.intensity.value = Mathf.Lerp(start, pulseIntensity, t / pulseInTime);
            t += Time.deltaTime;
            yield return null;
        }

        vignette.intensity.value = pulseIntensity;

        // Ramp down to idle
        t = 0f;
        while (t < pulseOutTime)
        {
            vignette.intensity.value = Mathf.Lerp(pulseIntensity, idleIntensity, t / pulseOutTime);
            t += Time.deltaTime;
            yield return null;
        }

        vignette.intensity.value = idleIntensity;
    }
}