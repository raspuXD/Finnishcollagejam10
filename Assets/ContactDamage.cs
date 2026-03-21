using UnityEngine;

public class ContactDamage : MonoBehaviour
{
    public float damage = 10f;
    public float cooldown = 0.2f;

    private float lastHitTime;

    void OnCollisionStay(Collision collision)
    {
        if (Time.time < lastHitTime + cooldown)
            return;

        PlayerHealth player = collision.collider.GetComponent<PlayerHealth>();
        if (player != null)
        {
            player.TakeDamage(damage);
            lastHitTime = Time.time;
        }
    }
}