
using UnityEngine;
using UnityEngine.Events;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    private bool isDead;

    public UnityEvent<string> onDeath;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage, string causeTag = "Unknown")
    {
        if (isDead) return;

        currentHealth -= damage;

        if (currentHealth <= 0f)
            Die(causeTag);
    }

    void Die(string causeTag)
    {
        isDead = true;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.RegisterKill(causeTag);

        AudioManager.Instance.PlaySFX3D(causeTag, this.gameObject, 1f, 105f);
        Debug.Log(causeTag);
        onDeath?.Invoke(causeTag);
        Destroy(gameObject, 0.1f);
    }

    public float GetHealthNormalized() => currentHealth / maxHealth;
}