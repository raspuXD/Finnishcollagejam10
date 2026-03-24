using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Regen")]
    public float regenDelay = 3f;  // seconds after last hit before regen resumes
    private float lastHitTime = -999f;
    public bool CanRegen => Time.time >= lastHitTime + regenDelay;

    public UnityEvent<float> onHealthChanged;
    public UnityEvent<string> onDeath;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage, string causeTag = "Unknown")
    {
        currentHealth  = Mathf.Max(currentHealth - damage, 0f);
        lastHitTime    = Time.time;  // reset regen buffer on every hit

        onHealthChanged?.Invoke(GetHealthNormalized());

        if (currentHealth <= 0f)
            Die(causeTag);
    }

    public void Heal(float amount)
    {
        if (!CanRegen) return;  // blocked until buffer expires

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        onHealthChanged?.Invoke(GetHealthNormalized());
    }

    public void SetMaxHealth(float newMax)
    {
        float ratio   = currentHealth / maxHealth;
        maxHealth     = newMax;
        currentHealth = maxHealth * ratio;
        onHealthChanged?.Invoke(GetHealthNormalized());
    }

    void Die(string causeTag)
    {
        onDeath?.Invoke(causeTag);
    }

    public float GetHealthNormalized() => currentHealth / maxHealth;
}