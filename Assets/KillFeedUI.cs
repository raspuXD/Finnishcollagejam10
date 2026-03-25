using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class KillFeedUI : MonoBehaviour, IClosableUI
{
    [Header("References")]
    public ScoreManager scoreManager;

    [Header("Score Display")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highscoreText;
    public TextMeshProUGUI levelText;
    public Image           xpBar;
    public TextMeshProUGUI tokenText;

    [Header("Kill Feed")]
    public Transform       killFeedContainer;
    public GameObject      killFeedEntryPrefab;
    public int             maxFeedEntries = 5;
    public float           entryLifetime  = 2.5f;
    public float           entryFadeTime  = 0.5f;

    [Header("Chain Display")]
    public GameObject      chainContainer;        // parent object to show/hide
    public TextMeshProUGUI chainCountText;         // "x4 CHAIN"
    public TextMeshProUGUI chainMultiplierText;    // "x2.5"
    public Image           chainTimerBar;          // fill image for timer
    public Color           chainBaseColor  = Color.white;
    public Color           chainMaxColor   = Color.red;
    public int             maxChainDisplay = 10;

    private Queue<GameObject> activeFeedEntries = new Queue<GameObject>();
    private float             chainTimeRemaining = 0f;
    private bool              chainVisible       = false;

    // ── IClosableUI ───────────────────────────────────────
    public void Hide() => gameObject.SetActive(false);
    public void Show() => gameObject.SetActive(true);

    // ── Lifecycle ─────────────────────────────────────────

    void OnEnable()
    {
        if (scoreManager == null) return;
        scoreManager.onScoreChanged.AddListener(UpdateScore);
        scoreManager.onChainUpdated.AddListener(UpdateChain);
        scoreManager.onXPChanged.AddListener(UpdateXP);
        scoreManager.onLevelUp.AddListener(UpdateLevel);
        scoreManager.onKill.AddListener(ShowKillFeedEntry);
        scoreManager.onTokensChanged.AddListener(UpdateTokens);
    }

    void OnDisable()
    {
        if (scoreManager == null) return;
        scoreManager.onScoreChanged.RemoveListener(UpdateScore);
        scoreManager.onChainUpdated.RemoveListener(UpdateChain);
        scoreManager.onXPChanged.RemoveListener(UpdateXP);
        scoreManager.onLevelUp.RemoveListener(UpdateLevel);
        scoreManager.onKill.RemoveListener(ShowKillFeedEntry);
        scoreManager.onTokensChanged.RemoveListener(UpdateTokens);
    }

    void Start()
    {
        if (scoreManager == null) return;

        UpdateScore(scoreManager.TotalScore);
        UpdateChain(scoreManager.ChainCount);
        UpdateXP(scoreManager.CurrentXP);
        UpdateLevel(scoreManager.CurrentLevel);
        UpdateTokens(scoreManager.Tokens);

        if (highscoreText != null)
            highscoreText.text = $"BEST: {scoreManager.HighScore:N0}";

        // Hide chain display initially
        if (chainContainer != null)
            chainContainer.SetActive(false);
    }

    void Update()
    {
        if (!chainVisible) return;

        chainTimeRemaining -= Time.deltaTime;

        if (chainTimerBar != null)
        {
            float fill = Mathf.Clamp01(chainTimeRemaining / scoreManager.chainWindow);
            chainTimerBar.fillAmount = fill;
            chainTimerBar.color      = Color.Lerp(Color.red, Color.green, fill);
        }

        if (chainTimeRemaining <= 0f)
            HideChain();
    }

    // ── Chain ─────────────────────────────────────────────

    void UpdateChain(int chain)
    {
        if (chain <= 0)
        {
            HideChain();
            return;
        }

        chainVisible       = true;
        chainTimeRemaining = scoreManager.chainWindow;

        if (chainContainer != null)
            chainContainer.SetActive(true);

        // Color ramp
        float t     = Mathf.Clamp01((float)chain / maxChainDisplay);
        Color color = Color.Lerp(chainBaseColor, chainMaxColor, t);

        if (chainCountText != null)
        {
            chainCountText.text  = $"x{chain} CHAIN";
            chainCountText.color = color;
        }

        // Multiplier = 1 + chain * chainMultiplier from ScoreManager
        if (chainMultiplierText != null)
        {
            float mult = 1f + chain * scoreManager.chainMultiplier;
            chainMultiplierText.text  = $"{mult:F1}x";
            chainMultiplierText.color = color;
        }

        if (chainTimerBar != null)
        {
            chainTimerBar.fillAmount = 1f;
            chainTimerBar.color      = color;
        }
    }

    void HideChain()
    {
        chainVisible = false;
        if (chainContainer != null)
            chainContainer.SetActive(false);
    }

    // ── Score / XP / Tokens ───────────────────────────────

    void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = score.ToString("N0");

        if (highscoreText != null)
            highscoreText.text = $"BEST: {scoreManager.HighScore:N0}";
    }

    void UpdateXP(int xp)
    {
        if (xpBar != null)
            xpBar.fillAmount = (float)xp / scoreManager.xpPerLevel;
    }

    void UpdateLevel(int level)
    {
        if (levelText != null)
            levelText.text = $"LVL {level}";
    }

    void UpdateTokens(int tokens)
    {
        if (tokenText != null)
            tokenText.text = $"Tokens: {tokens}";
    }

    // ── Kill Feed ─────────────────────────────────────────

    void ShowKillFeedEntry(string displayName, int points, int chain)
    {
        if (killFeedContainer == null || killFeedEntryPrefab == null) return;

        while (activeFeedEntries.Count >= maxFeedEntries)
        {
            GameObject old = activeFeedEntries.Dequeue();
            if (old != null) Destroy(old);
        }

        GameObject entry = Instantiate(killFeedEntryPrefab, killFeedContainer);
        entry.transform.SetAsLastSibling();

        TextMeshProUGUI t = entry.GetComponentInChildren<TextMeshProUGUI>();
        if (t != null)
        {
            string chainSuffix = chain > 1 ? $" <color=#FFD700>x{chain}</color>" : "";
            t.text = $"{displayName}  <color=#00FF88>+{points}</color>{chainSuffix}";
        }

        activeFeedEntries.Enqueue(entry);
        StartCoroutine(FadeOutEntry(entry, entryLifetime, entryFadeTime));
    }

    IEnumerator FadeOutEntry(GameObject entry, float lifetime, float fadeTime)
    {
        yield return new WaitForSeconds(lifetime);

        CanvasGroup cg = entry.GetComponent<CanvasGroup>();
        if (cg == null) cg = entry.AddComponent<CanvasGroup>();

        float t = 0f;
        while (t < fadeTime)
        {
            if (entry == null) yield break;
            cg.alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
            t += Time.deltaTime;
            yield return null;
        }

        if (entry != null) Destroy(entry);
    }
}