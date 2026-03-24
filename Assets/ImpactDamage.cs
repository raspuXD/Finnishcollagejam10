using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TagImpactSettings
{
    public string tag;
    public float minVelocity = 3f;
    public float damageMultiplier = 1f;
}

public enum HitStrength
{
    Small,
    Medium,
    Big
}

public class ImpactDamage : MonoBehaviour
{
    [Header("Global Damage Settings")]
    public float baseDamageMultiplier = 2f;
    public float maxDamage = 50f;
    public float hitCooldown = 0.1f;

    [Header("Hit Strength Thresholds")]
    public float smallHitThreshold = 5f;
    public float mediumHitThreshold = 15f;
    // anything above medium = Big

    [Header("Tag-Based Settings")]
    public List<TagImpactSettings> tagSettings = new List<TagImpactSettings>();

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

    TagImpactSettings GetTagSettings(string tag)
    {
        foreach (var setting in tagSettings)
        {
            if (setting.tag == tag)
                return setting;
        }
        return null;
    }

    HitStrength GetHitStrength(float damage)
    {
        if (damage < smallHitThreshold)  return HitStrength.Small;
        if (damage < mediumHitThreshold) return HitStrength.Medium;
        return HitStrength.Big;
    }

    void HandleHitEffect(HitStrength strength, Vector3 point, Vector3 normal)
    {
        // This is your expansion point

        switch (strength)
        {
            case HitStrength.Small:
                Debug.Log("SMALL HIT");
                // TODO: light particles, small sound
                break;

            case HitStrength.Medium:
                Debug.Log("MEDIUM HIT");
                // TODO: medium VFX, stronger sound
                break;

            case HitStrength.Big:
                Debug.Log("BIG HIT");
                // TODO: heavy VFX, screen shake, decals
                break;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (Time.time < lastHitTime + hitCooldown)
            return;

        float velocityBefore = lastVelocity.magnitude;
        float relativeVelocity = collision.relativeVelocity.magnitude;
        float velocityLoss = velocityBefore - relativeVelocity;

        TagImpactSettings tagSetting = GetTagSettings(collision.collider.tag);

        float minVelocity = tagSetting != null ? tagSetting.minVelocity : 0f;
        float tagMultiplier = tagSetting != null ? tagSetting.damageMultiplier : 1f;

        ContactPoint contact = collision.contacts[0];
        Vector3 normal = contact.normal;

        float alignment = Vector3.Dot(lastVelocity.normalized, -normal);
        float alignmentMultiplier = Mathf.Clamp01((alignment - 0.3f) * 2f);

        float damage = velocityLoss * baseDamageMultiplier * tagMultiplier * alignmentMultiplier;
        damage = Mathf.Clamp(damage, 0f, maxDamage);

        // -------------------------
        // PROP → STRICT FILTER
        // -------------------------
        if (magnetObject != null && magnetObject.objectType == MetalObject.ObjectType.Prop)
        {
            if (velocityLoss < minVelocity) return;
            if (relativeVelocity < minVelocity) return;
            if (alignmentMultiplier <= 0f) return;

            EnemyHealth enemy = collision.collider.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);

                // HIT EFFECT
                HitStrength strength = GetHitStrength(damage);
                HandleHitEffect(strength, contact.point, normal);

                if (alignment < 0.6f)
                    lastHitTime = Time.time + 0.2f;
                else
                    lastHitTime = Time.time;

                Debug.Log("ENEMY DAMAGE: " + damage);
            }

            return;
        }

        // -------------------------
        // ENEMY → LOOSE FILTER
        // -------------------------
        if (magnetObject != null && magnetObject.objectType == MetalObject.ObjectType.Enemy)
        {
            if (collision.collider.GetComponent<PlayerHealth>() != null)
                return;

            float enemyVelocity = lastVelocity.magnitude;
            if (enemyVelocity < minVelocity)
                return;

            float enemyDamage = enemyVelocity * baseDamageMultiplier * tagMultiplier * 0.5f;
            enemyDamage *= alignmentMultiplier;
            enemyDamage = Mathf.Clamp(enemyDamage, 0f, maxDamage);

            EnemyHealth self = GetComponent<EnemyHealth>();
            if (self != null)
            {
                self.TakeDamage(enemyDamage);

                // HIT EFFECT
                HitStrength strength = GetHitStrength(enemyDamage);
                HandleHitEffect(strength, contact.point, normal);

                if (alignment < 0.6f)
                    lastHitTime = Time.time + 0.2f;
                else
                    lastHitTime = Time.time;

            }
        }
    }
}