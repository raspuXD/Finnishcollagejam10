using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    public UnityEvent<float> onHealthChanged;  // normalized 0-1
    public UnityEvent<string> onDeath;
    


    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage, string causeTag = "Unknown")
    {
        currentHealth -= damage;
        currentHealth  = Mathf.Max(currentHealth, 0f);

        onHealthChanged?.Invoke(GetHealthNormalized());

        if (currentHealth <= 0f)
            Die(causeTag);
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        onHealthChanged?.Invoke(GetHealthNormalized());
    }

    public void SetMaxHealth(float newMax)
    {
        float ratio   = currentHealth / maxHealth;
        maxHealth     = newMax;
        currentHealth = maxHealth * ratio;  // scale current health proportionally
        onHealthChanged?.Invoke(GetHealthNormalized());
    }

    void Die(string causeTag)
    {
        onDeath?.Invoke(causeTag);
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public float GetHealthNormalized() => currentHealth / maxHealth;
}