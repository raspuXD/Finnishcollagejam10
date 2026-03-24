using UnityEngine;
using System;
using Unity.VisualScripting;
using System.Runtime.CompilerServices;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    [Header("Tokens")]
    public int tokens;

    [Header("References")]
    public PlayerController player;
    public PlayerHealth health;
    public MagnetController magnet;

    [Header("Upgrade Levels")]
    public int maxHealthLevel;
    public int regenLevel;
    public int jumpForceLevel;
    public int maxJumpsLevel;
    public int moveSpeedLevel;
    public int accelerationLevel;
    public int magnetStrengthLevel;
    public int magnetRangeLevel;
    public int magnetUnlockLevel;

    [Header("Canvases")]
    public GameObject PlayCanvas;
    public GameObject UpgradeCanvas;
    public MouseUnlock mouseUnlocker;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.onLevelUp.AddListener(OnLevelUp);
    }

    void OnDisable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.onLevelUp.RemoveListener(OnLevelUp);
    }

    void OnLevelUp(int level)
    {
        tokens += 1;
        Debug.Log($"Got token! Total: {tokens}");
    }

    void OnTokenSpend()
    {
        if(tokens == 0)
        {
            PlayCanvas.SetActive(true);
            UpgradeCanvas.SetActive(false);
            mouseUnlocker.LockMouse();
        }
        else
            return;
    }

    // ✅ FIXED: now uses cost
    bool TrySpend(int cost)
    {
        if (tokens < cost)
            return false;

        tokens -= cost;
        return true;
    }

    // ─────────────────────────────
    // UPGRADES
    // ─────────────────────────────

    public void UpgradeMaxHealth(int cost)
    {
        if (!TrySpend(cost)) return;

        maxHealthLevel++;
        health.SetMaxHealth(health.maxHealth += 10);
        OnTokenSpend();
    }

    public void UpgradeRegen(int cost)
    {
        if (!TrySpend(cost)) return;

        regenLevel++;

        CancelInvoke(nameof(RegenTick));
        regenAmount =+ 1;

        InvokeRepeating(nameof(RegenTick), 1f, 1f);
        OnTokenSpend();
    }

    float regenAmount;

    void RegenTick()
    {
        health.Heal(regenAmount);
    }

    public void UpgradeJumpForce(int cost)
    {
        if (!TrySpend(cost)) return;

        jumpForceLevel++;
        player.jumpForce += 10;
        OnTokenSpend();
    }

    public void UpgradeMaxJumps(int cost, int amount)
    {
        if (!TrySpend(cost)) return;

        maxJumpsLevel++;
        player.MaxJump += 1;
        OnTokenSpend();
    }

    public void UpgradeMoveSpeed(int cost)
    {
        if (!TrySpend(cost)) return;

        moveSpeedLevel++;
        player.moveSpeed += 10;
        OnTokenSpend();
    }

    public void UpgradeAcceleration(int cost)
    {
        if (!TrySpend(cost)) return;

        accelerationLevel++;
        player.acceleration += 15;
        OnTokenSpend();
    }

    public void UpgradeMagnetStrength(int cost)
    {
        if (!TrySpend(cost)) return;

        magnetStrengthLevel++;
        magnet.magneticStrength += 50;
        OnTokenSpend();
    }

    public void UpgradeMagnetRange(int cost)
    {
        if (!TrySpend(cost)) return;

        magnetRangeLevel++;
        magnet.range += 10;
        OnTokenSpend();
    }

    public void UnlockMagnet(int cost)
    {
        if (magnetUnlockLevel > 0) return;

        if (!TrySpend(cost)) return;

        magnetUnlockLevel = 1;
        magnet.currentEnabled = MagnetController.Enabled.ON;
        OnTokenSpend();
    }
}