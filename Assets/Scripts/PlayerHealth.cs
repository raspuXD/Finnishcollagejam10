using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 500f;
    [SerializeField] private float currentHealth;

    [Header("Regen")]
    public float regenDelay    = 3f;
    public float regenRate     = 0f;
    public float regenTickRate = 1f;

    private float lastHitTime = -999f;
    private bool  isDead      = false;

    public bool CanRegen => Time.time >= lastHitTime + regenDelay && regenRate > 0f;

    public UnityEvent<float>  onHealthChanged;
    public UnityEvent<string> onDeath;
    public UnityEvent<float>  OnHeal;

    [SerializeField] private HealthBar healthBar;

    void Awake()
    {
        currentHealth = maxHealth;
        healthBar.UpdateHealthBar(maxHealth, currentHealth);
    }

    void Start()
    {
        StartCoroutine(RegenLoop());
    }

    IEnumerator RegenLoop()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(regenTickRate);

            // Skip if dead, full health, or buffer active
            if (isDead) yield break;
            if (!CanRegen) continue;
            if (currentHealth >= maxHealth) continue;


            AudioManager.Instance.PlaySFX("Heal");
            currentHealth = Mathf.Min(currentHealth + regenRate * regenTickRate, maxHealth);
            UpdateHealthBar();
            OnHeal?.Invoke(GetHealthNormalized());
            onHealthChanged?.Invoke(GetHealthNormalized());
        }
    }

    public void UpdateHealthBar()
    {
        healthBar.UpdateHealthBar(maxHealth, currentHealth);
        
    }

    public void TakeDamage(float damage, string causeTag = "Unknown")
    {
        if (isDead) return;
        

        currentHealth = Mathf.Max(currentHealth - damage, 0f);
        lastHitTime   = Time.time;

        onHealthChanged?.Invoke(GetHealthNormalized());

        UpdateHealthBar();
        AudioManager.Instance.PlaySFX("TookDamage");

        if (currentHealth <= 0f)
            Die(causeTag);
    }

    public void Heal(float amount)
    {
        if (isDead) return;


        AudioManager.Instance.PlaySFX("Heal");
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        
        onHealthChanged?.Invoke(GetHealthNormalized());
        UpdateHealthBar();
    }

    public void SetMaxHealth(float newMax)
    {
        float ratio   = currentHealth / maxHealth;
        maxHealth     = newMax;
        currentHealth = maxHealth * ratio;
        onHealthChanged?.Invoke(GetHealthNormalized());
        UpdateHealthBar();
    }

    void Die(string causeTag)
    {
        isDead = true;
        onDeath?.Invoke(causeTag);
    }

    public float GetHealthNormalized() => currentHealth / maxHealth;
}