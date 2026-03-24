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
    public TextMeshProUGUI chainText;
    public TextMeshProUGUI levelText;
    public Image           xpBar;

    [Header("Kill Feed")]
    public Transform       killFeedContainer;
    public GameObject      killFeedEntryPrefab; // TextMeshProUGUI + CanvasGroup
    public int             maxFeedEntries = 5;
    public float           entryLifetime  = 2.5f;
    public float           entryFadeTime  = 0.5f;

    [Header("Chain Display")]
    public TextMeshProUGUI chainPopupText;
    public float           chainPopupDuration = 1f;

    [Header("Chain Colors")]
    public Color baseChainColor  = Color.white;
    public Color maxChainColor   = Color.red;
    public int   maxChainDisplay = 10;

    private Queue<GameObject> activeFeedEntries = new Queue<GameObject>();
    private Coroutine chainPopupRoutine;
    public void Hide() => gameObject.SetActive(false);
    public void Show() => gameObject.SetActive(true);    void OnEnable()
    {
        if (scoreManager == null) return;

        scoreManager.onScoreChanged.AddListener(UpdateScore);
        scoreManager.onChainUpdated.AddListener(UpdateChain);
        scoreManager.onXPChanged.AddListener(UpdateXP);
        scoreManager.onLevelUp.AddListener(UpdateLevel);
        scoreManager.onKill.AddListener(ShowKillFeedEntry);
    }

    void OnDisable()
    {
        if (scoreManager == null) return;

        scoreManager.onScoreChanged.RemoveListener(UpdateScore);
        scoreManager.onChainUpdated.RemoveListener(UpdateChain);
        scoreManager.onXPChanged.RemoveListener(UpdateXP);
        scoreManager.onLevelUp.RemoveListener(UpdateLevel);
        scoreManager.onKill.RemoveListener(ShowKillFeedEntry);
    }

    void Start()
    {
        if (scoreManager == null) return;

        UpdateScore(scoreManager.TotalScore);
        UpdateChain(scoreManager.ChainCount);
        UpdateXP(scoreManager.CurrentXP);
        UpdateLevel(scoreManager.CurrentLevel);

        if (highscoreText != null)
            highscoreText.text = $"BEST: {scoreManager.HighScore}";
    }

    // ── Listeners ──────────────────────────────────────────

    void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = score.ToString("N0");

        if (highscoreText != null)
            highscoreText.text = $"BEST HIGHSCORE: {scoreManager.HighScore:N0}";
    }

    void UpdateChain(int chain)
    {
        if (chainText == null) return;

        if (chain <= 1)
        {
            chainText.gameObject.SetActive(false);
            return;
        }

        chainText.gameObject.SetActive(true);

        float t = Mathf.Clamp01((float)chain / maxChainDisplay);
        chainText.color = Color.Lerp(baseChainColor, maxChainColor, t);
        chainText.text  = $"x{chain} CHAIN";
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

    void ShowKillFeedEntry(string displayName, int points, int chain)
    {
        if (killFeedContainer == null || killFeedEntryPrefab == null) return;

        // Trim oldest if over limit
        while (activeFeedEntries.Count >= maxFeedEntries)
        {
            GameObject old = activeFeedEntries.Dequeue();
            if (old != null) Destroy(old);
        }

        GameObject entry  = Instantiate(killFeedEntryPrefab, killFeedContainer);
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