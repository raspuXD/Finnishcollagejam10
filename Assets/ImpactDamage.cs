using UnityEngine;

public class ImpactDamage : MonoBehaviour
{
    public float damageMultiplier = 2f;
    public float minImpactVelocity = 3f;
    public float maxDamage = 50f;
    public float hitCooldown = 0.1f;

    private float lastHitTime;
    private Vector3 lastVelocity;
    private Rigidbody rb;
    private MetalObject magnetObject;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        magnetObject = GetComponent<MetalObject>();
    }

    void FixedUpdate()
    {
        lastVelocity = rb.linearVelocity;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (Time.time < lastHitTime + hitCooldown)
            return;

        float velocityBefore = lastVelocity.magnitude;
        float relativeVelocity = collision.relativeVelocity.magnitude;
        float velocityLoss = velocityBefore - relativeVelocity;

        float damage = velocityLoss * damageMultiplier;
        damage = Mathf.Clamp(damage, 0f, maxDamage);

        // -------------------------
        // PROP → STRICT FILTER (clean hits only)
        // -------------------------
        if (magnetObject != null && magnetObject.objectType == MetalObject.ObjectType.Prop)
        {
            if (velocityLoss < minImpactVelocity)
                return;

            if (relativeVelocity < minImpactVelocity)
                return;

            ContactPoint contact = collision.contacts[0];
            Vector3 normal = contact.normal;

            float alignment = Vector3.Dot(lastVelocity.normalized, -normal);
            if (alignment < 0.5f)
                return;

            EnemyHealth enemy = collision.collider.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                lastHitTime = Time.time;
                Debug.Log("ENEMY DAMAGE: " + damage);
            }

            return;
        }

        // -------------------------
        // ENEMY → LOOSE FILTER (for wall slams)
        // -------------------------
        if (magnetObject != null && magnetObject.objectType == MetalObject.ObjectType.Enemy)
        {
            // Ignore player (handled elsewhere)
            if (collision.collider.GetComponent<PlayerHealth>() != null)
                return;

            float enemyVelocity = lastVelocity.magnitude;

            if (enemyVelocity < minImpactVelocity)
                return;


            // -------------------------
            // CHECK WHAT WE HIT
            // -------------------------

            bool hitWall = collision.collider.CompareTag("Wall");
            bool hitFloor = collision.collider.CompareTag("Floor");

            // Optional: if you ONLY want wall damage
            if (hitFloor)
                return;


            float enemyDamage = enemyVelocity * damageMultiplier * 0.5f;

            // Optional bonus for walls
            if (hitWall)
            {
                enemyDamage *= 1.5f;
            }

            enemyDamage = Mathf.Clamp(enemyDamage, 0f, maxDamage);

            EnemyHealth self = GetComponent<EnemyHealth>();
            if (self != null)
            {
                self.TakeDamage(enemyDamage);
                lastHitTime = Time.time;
                Debug.Log("ENEMY DAMAGE: " + enemyDamage);
            }
        }
    }
}