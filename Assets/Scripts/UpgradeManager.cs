using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

[System.Serializable]
public class Upgrade
{
    public string id;
    public string displayName;
    public string description;
    public int    maxLevel;
    public int    currentLevel;
    public float  valuePerLevel;
    public float  baseValue;
    public int    tokenCost;

    public bool  IsMaxed      => currentLevel >= maxLevel;
    public float CurrentValue => baseValue + valuePerLevel * currentLevel;
    public float NextValue    => baseValue + valuePerLevel * (currentLevel + 1);
}

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    [Header("References")]
    public PlayerController playerController;
    public PlayerHealth     playerHealth;
    public MagnetController magnetController;
    public ScoreManager     scoreManager;

    [Header("Input")]
    public InputActionReference toggleUpgradeMenuAction;

    [Header("Upgrade Menu UI")]
    public GameObject upgradeMenuRoot;
    public Transform  upgradeButtonContainer;
    public GameObject upgradeButtonPrefab;
    public TextMeshProUGUI tokenCountText;

    [Header("Upgrades")]
    public List<Upgrade> upgrades = new List<Upgrade>()
    {
        new Upgrade { id = "max_health",      displayName = "Max Health",      description = "+25 max health per level",         maxLevel = 5, baseValue = 100f, valuePerLevel = 25f,  tokenCost = 1 },
        new Upgrade { id = "health_regen",    displayName = "Health Regen",    description = "+2 hp/sec per level",              maxLevel = 5, baseValue = 0f,   valuePerLevel = 2f,   tokenCost = 1 },
        new Upgrade { id = "move_speed",      displayName = "Move Speed",      description = "+2 move speed, +4 sprint speed",   maxLevel = 5, baseValue = 8f,   valuePerLevel = 2f,   tokenCost = 1 },
        new Upgrade { id = "acceleration",    displayName = "Acceleration",    description = "+5 acceleration per level",         maxLevel = 5, baseValue = 20f,  valuePerLevel = 5f,   tokenCost = 1 },
        new Upgrade { id = "jump_force",      displayName = "Jump Force",      description = "+2 jump force per level",           maxLevel = 5, baseValue = 5f,   valuePerLevel = 2f,   tokenCost = 1 },
        new Upgrade { id = "max_jumps",       displayName = "Extra Jump",      description = "+1 max jump per level",             maxLevel = 3, baseValue = 1f,   valuePerLevel = 1f,   tokenCost = 2 },
        new Upgrade { id = "magnet_strength", displayName = "Magnet Strength", description = "+100 magnet strength per level",    maxLevel = 5, baseValue = 500f, valuePerLevel = 100f, tokenCost = 1 },
        new Upgrade { id = "magnet_range",    displayName = "Magnet Range",    description = "+3 magnet range per level",         maxLevel = 5, baseValue = 20f,  valuePerLevel = 3f,   tokenCost = 1 },
        new Upgrade { id = "magnet_toggle",   displayName = "Magnet Toggle",   description = "Unlock magnet on/off toggle",       maxLevel = 1, baseValue = 0f,   valuePerLevel = 1f,   tokenCost = 3 },
    };

    private bool menuOpen = false;
    private List<GameObject> spawnedButtons = new List<GameObject>();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        if (toggleUpgradeMenuAction != null)
        {
            toggleUpgradeMenuAction.action.Enable();
            toggleUpgradeMenuAction.action.performed += OnToggleUpgradeMenu;
        }
    }

    void OnDisable()
    {
        if (toggleUpgradeMenuAction != null)
        {
            toggleUpgradeMenuAction.action.performed -= OnToggleUpgradeMenu;
            toggleUpgradeMenuAction.action.Disable();
        }
    }

    void Start()
    {
        //LoadUpgrades();
        ApplyAll();

        if (upgradeMenuRoot != null)
            upgradeMenuRoot.SetActive(false);
        else
            Debug.Log("UPGRADE MENU NULL");
    }

    // ── Input Callback ─────────────────────────────────────────────

    private void OnToggleUpgradeMenu(InputAction.CallbackContext context)
    {
        ToggleUpgradeMenu();
    }

    // ── Menu ───────────────────────────────────────────────────────

    public void ToggleUpgradeMenu()
    {
        menuOpen = !menuOpen;
        upgradeMenuRoot.SetActive(menuOpen);

        Time.timeScale = menuOpen ? 0f : 1f;

        Cursor.lockState = menuOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = menuOpen;

        if (menuOpen)
            RefreshUI();
    }

    void RefreshUI()
    {
        foreach (var b in spawnedButtons)
            if (b != null) Destroy(b);
        spawnedButtons.Clear();

        if (tokenCountText != null && scoreManager != null)
            tokenCountText.text = $"Tokens: {scoreManager.Tokens}";

        foreach (var upgrade in upgrades)
        {
            GameObject entry = Instantiate(upgradeButtonPrefab, upgradeButtonContainer);
            spawnedButtons.Add(entry);

            TextMeshProUGUI nameText  = entry.transform.Find("NameText") ?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI descText  = entry.transform.Find("DescText") ?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI levelText = entry.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI costText  = entry.transform.Find("CostText") ?.GetComponent<TextMeshProUGUI>();
            Button          button    = entry.GetComponentInChildren<Button>();

            if (nameText  != null) nameText.text  = upgrade.displayName;
            if (descText  != null) descText.text  = upgrade.description;
            if (levelText != null) levelText.text = upgrade.IsMaxed
                                                    ? "MAX"
                                                    : $"Level {upgrade.currentLevel} / {upgrade.maxLevel}";
            if (costText  != null) costText.text  = upgrade.IsMaxed
                                                    ? "-"
                                                    : $"{upgrade.tokenCost} token(s)";

            if (button != null)
            {
                bool canAfford = scoreManager != null && scoreManager.Tokens >= upgrade.tokenCost;
                button.interactable = !upgrade.IsMaxed && canAfford;

                string capturedId = upgrade.id;
                button.onClick.AddListener(() =>
                {
                    TryPurchase(capturedId);
                    RefreshUI();
                });
            }
        }
    }

    // ── Purchase ───────────────────────────────────────────────────

    public bool TryPurchase(string id)
    {
        Upgrade upgrade = GetUpgrade(id);
        if (upgrade == null)  return false;
        if (upgrade.IsMaxed)  return false;
        if (scoreManager == null) return false;

        if (scoreManager.Tokens < upgrade.tokenCost)
        {
            Debug.Log($"Not enough tokens. Need {upgrade.tokenCost}, have {scoreManager.Tokens}");
            return false;
        }

        scoreManager.SpendTokens(upgrade.tokenCost);
        upgrade.currentLevel++;

        ApplyUpgrade(upgrade);
        SaveUpgrades();

        Debug.Log($"Upgraded {upgrade.displayName} to level {upgrade.currentLevel}");
        return true;
    }

    public Upgrade GetUpgrade(string id)
    {
        foreach (var u in upgrades)
            if (u.id == id) return u;
        return null;
    }

    // ── Apply ──────────────────────────────────────────────────────

    void ApplyAll()
    {
        foreach (var u in upgrades)
            ApplyUpgrade(u);
    }

    void ApplyUpgrade(Upgrade u)
    {
        switch (u.id)
        {
            case "max_health":
                if (playerHealth != null)
                    playerHealth.SetMaxHealth(u.CurrentValue);
                break;

            case "health_regen":
                if (playerHealth != null)
                    playerHealth.regenRate = u.CurrentValue;
                break;

            case "move_speed":
                if (playerController != null)
                {
                    playerController.moveSpeed        = u.CurrentValue;
                    playerController.sprintSpeedBonus = 5f + u.currentLevel * 4f;
                }
                break;

            case "acceleration":
                if (playerController != null)
                    playerController.acceleration = u.CurrentValue;
                break;

            case "jump_force":
                if (playerController != null)
                    playerController.jumpForce = u.CurrentValue;
                break;

            case "max_jumps":
                if (playerController != null)
                    playerController.MaxJump = (int)u.CurrentValue;
                break;

            case "magnet_strength":
                if (magnetController != null)
                    magnetController.magneticStrength = u.CurrentValue;
                break;

            case "magnet_range":
                if (magnetController != null)
                    magnetController.range = u.CurrentValue;
                break;

            case "magnet_toggle":
                break;
        }
    }

    // ── Save / Load ────────────────────────────────────────────────

    void SaveUpgrades()
    {
        foreach (var u in upgrades)
            PlayerPrefs.SetInt("Upgrade_" + u.id, u.currentLevel);
        PlayerPrefs.Save();
    }

    void LoadUpgrades()
    {
        foreach (var u in upgrades)
            u.currentLevel = PlayerPrefs.GetInt("Upgrade_" + u.id, 0);
    }

    public void ResetUpgrades()
    {
        foreach (var u in upgrades)
        {
            u.currentLevel = 0;
            PlayerPrefs.DeleteKey("Upgrade_" + u.id);
        }
        PlayerPrefs.Save();
        ApplyAll();
    }
}