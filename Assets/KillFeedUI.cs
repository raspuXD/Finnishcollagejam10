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
    public TextMeshProUGUI deathCauseText;
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
            StartFadeOutChain(); // was HideChain()
    }

    // Player Death
    string FormatDeathCause(string causeTag)
    {
        switch (causeTag)
        {
            case "Spikes":   return "Killed by Spikes";
            case "Pit":      return "Fell into a Pit";
            case "Enemy":    return "Killed by an Enemy";
            case "Prop":     return "Crushed by a Prop";
            case "Wall":     return "Wall Slammed";
            case "Floor":    return "Floor Slammed";
            default:         return $"Killed by {causeTag}";
        }
    }
    // Call this from PlayerHealth.onDeath event
    public void GetPlayerDeath(string causeTag)
    {
        if (deathCauseText != null)
            deathCauseText.text = FormatDeathCause(causeTag);
    }

    

    // ── Chain ─────────────────────────────────────────────

    // ── Chain ─────────────────────────────────────────────

void UpdateChain(int chain)
{
    if (chain <= 0)
    {
        StartFadeOutChain();
        return;
    }

    // Cancel any ongoing fade if a new kill comes in
    if (chainFadeCoroutine != null)
    {
        StopCoroutine(chainFadeCoroutine);
        chainFadeCoroutine = null;
    }

    chainVisible       = true;
    chainTimeRemaining = scoreManager.chainWindow;

    if (chainContainer != null)
    {
        chainContainer.SetActive(true);
        // Make sure alpha is fully visible
        CanvasGroup cg = chainContainer.GetComponent<CanvasGroup>();
        if (cg == null) cg = chainContainer.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
    }

    float t     = Mathf.Clamp01((float)chain / maxChainDisplay);
    Color color = Color.Lerp(chainBaseColor, chainMaxColor, t);

    if (chainCountText != null)
    {
        chainCountText.text  = $"x{chain} CHAIN";
        chainCountText.color = color;
    }

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

private Coroutine chainFadeCoroutine = null;

void StartFadeOutChain()
{
    if (!chainVisible) return;
    chainVisible       = false;
    chainTimeRemaining = 0f;

    if (chainContainer != null)
    {
        if (chainFadeCoroutine != null) StopCoroutine(chainFadeCoroutine);
        chainFadeCoroutine = StartCoroutine(FadeOutChain(entryFadeTime));
    }
}

IEnumerator FadeOutChain(float fadeTime)
{
    CanvasGroup cg = chainContainer.GetComponent<CanvasGroup>();
    if (cg == null) cg = chainContainer.AddComponent<CanvasGroup>();

    float t = 0f;
    while (t < fadeTime)
    {
        cg.alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
        t += Time.deltaTime;
        yield return null;
    }

    cg.alpha = 0f;
    chainContainer.SetActive(false);
    chainFadeCoroutine = null;
}

void HideChain()
{
    chainVisible       = false;
    chainTimeRemaining = 0f;
    if (chainContainer != null)
    {
        CanvasGroup cg = chainContainer.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f; // reset for next time
        chainContainer.SetActive(false);
    }
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