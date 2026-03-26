using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image healthBarSprite;

    public void UpdateHealthBar(float maxHealth, float currentHealth)
    {
        healthBarSprite.fillAmount = currentHealth / maxHealth;
    }
}
