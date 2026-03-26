using UnityEngine;

public class ContactDamage : MonoBehaviour
{
    [Header("Settings")]
    public float damage = 10f;
    public float cooldown = 1f;
    public float minVelocity = 2f; // must be moving toward player to deal damage

    private float lastHitTime;

    void OnCollisionEnter(Collision collision)
    {
        if (Time.time < lastHitTime + cooldown) return;

        PlayerHealth player = collision.collider.GetComponent<PlayerHealth>();
        if (player == null) return;

        // Only deal damage if enemy is actually moving toward the player
        float approachSpeed = collision.relativeVelocity.magnitude;
        if (approachSpeed < minVelocity) return;

        player.TakeDamage(damage);
        lastHitTime = Time.time;
    }
}