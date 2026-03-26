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

    [Header("Sound Cooldowns")]
    public float smallHitSoundCooldown  = 0.15f;
    public float mediumHitSoundCooldown = 0.4f;
    public float bigHitSoundCooldown    = 0.6f;

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
        switch (strength)
        {
            case HitStrength.Small:
                if (!AudioManager.Instance.IsSFXOnCooldown("SmallHit", smallHitSoundCooldown))
                {
                    AudioManager.Instance.PlaySFX3D("SmallHit", this.gameObject, 1f, 35f);
                    AudioManager.Instance.RegisterSFXPlayed("SmallHit");
                }
                lastHitTime = Time.time + smallHitSoundCooldown;
                // TODO: light particles
                break;

            case HitStrength.Medium:
                if (!AudioManager.Instance.IsSFXOnCooldown("MediumHit", mediumHitSoundCooldown))
                {
                    AudioManager.Instance.PlaySFX3D("MediumHit", this.gameObject, 1f, 35f);
                    AudioManager.Instance.RegisterSFXPlayed("MediumHit");
                }
                lastHitTime = Time.time + mediumHitSoundCooldown;
                // TODO: medium VFX
                break;

            case HitStrength.Big:
                if (!AudioManager.Instance.IsSFXOnCooldown("BigHit", bigHitSoundCooldown))
                {
                    AudioManager.Instance.PlaySFX3D("BigHit", this.gameObject, 1f, 100f);
                    AudioManager.Instance.RegisterSFXPlayed("BigHit");
                }
                lastHitTime = Time.time + bigHitSoundCooldown;
                // TODO: heavy VFX, screen shake
                break;
        }
    }

    void OnCollisionEnter(Collision collision)
{
    if (Time.time < lastHitTime + hitCooldown)
        return;

    string hitTag = collision.collider.tag;

    float velocityBefore   = lastVelocity.magnitude;
    float relativeVelocity = collision.relativeVelocity.magnitude;
    float velocityLoss     = velocityBefore - relativeVelocity;

    TagImpactSettings tagSetting = GetTagSettings(hitTag);

    float minVelocity   = tagSetting != null ? tagSetting.minVelocity    : 0f;
    float tagMultiplier = tagSetting != null ? tagSetting.damageMultiplier : 1f;

    ContactPoint contact          = collision.contacts[0];
    Vector3      normal           = contact.normal;
    float        alignment        = Vector3.Dot(lastVelocity.normalized, -normal);
    float        alignmentMultiplier = Mathf.Clamp01((alignment - 0.3f) * 2f);

    float damage = velocityLoss * baseDamageMultiplier * tagMultiplier * alignmentMultiplier;
    damage = Mathf.Clamp(damage, 0f, maxDamage);

    // ── PROP ────────────────────────────────────────────────────────
    if (magnetObject != null && magnetObject.objectType == MetalObject.ObjectType.Prop)
    {
        if (velocityLoss      < minVelocity) return;
        if (relativeVelocity  < minVelocity) return;
        if (alignmentMultiplier <= 0f)        return;

        // Prop hits enemy
        EnemyHealth enemy = collision.collider.GetComponent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage, hitTag);
            HitStrength strength = GetHitStrength(damage);
            HandleHitEffect(strength, contact.point, normal);
            lastHitTime = alignment < 0.6f ? Time.time + 0.2f : Time.time;
            Debug.Log("PROP HIT ENEMY: " + damage);
            return;
        }

        // Prop hits player
        PlayerHealth player = collision.collider.GetComponent<PlayerHealth>();
        if (player != null)
        {
            player.TakeDamage(damage, "Prop");
            HitStrength strength = GetHitStrength(damage);
            HandleHitEffect(strength, contact.point, normal);
            lastHitTime = Time.time;
            Debug.Log("PROP HIT PLAYER: " + damage);
            return;
        }

        return;
    }

    // ── ENEMY ────────────────────────────────────────────────────────
    if (magnetObject != null && magnetObject.objectType == MetalObject.ObjectType.Enemy)
    {
        float enemyVelocity = lastVelocity.magnitude;
        if (enemyVelocity < minVelocity) return;

        float enemyDamage = enemyVelocity * baseDamageMultiplier * tagMultiplier * 0.5f
                            * alignmentMultiplier;
        enemyDamage = Mathf.Clamp(enemyDamage, 0f, maxDamage);
        enemyDamage = Mathf.Round(enemyDamage);
        
        // Enemy hits player
        PlayerHealth player = collision.collider.GetComponent<PlayerHealth>();
        if (player != null)
        {
            player.TakeDamage(enemyDamage, "Enemy");
            HitStrength strength = GetHitStrength(enemyDamage);
            HandleHitEffect(strength, contact.point, normal);
            lastHitTime = Time.time;
            Debug.Log("ENEMY HIT PLAYER: " + enemyDamage);
            return;
        }

        // Enemy hits environment — damages itself
        EnemyHealth self = GetComponent<EnemyHealth>();
        if (self != null)
        {
            self.TakeDamage(enemyDamage, hitTag);
            HitStrength strength = GetHitStrength(enemyDamage);
            HandleHitEffect(strength, contact.point, normal);
            lastHitTime = alignment < 0.6f ? Time.time + 0.2f : Time.time;
        }
    }
}
}