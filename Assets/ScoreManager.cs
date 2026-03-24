using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class KillTypeScore
{
    public string causeTag;
    public int basePoints;
    public string displayName;  // shown in kill feed e.g. "Wall Slam!"
}

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Kill Scores")]
    public List<KillTypeScore> killScores = new List<KillTypeScore>()
    {
        new KillTypeScore { causeTag = "Spike",   basePoints = 300, displayName = "Spiked!"      },
        new KillTypeScore { causeTag = "Wall",    basePoints = 150, displayName = "Wall Slam!"   },
        new KillTypeScore { causeTag = "Floor",   basePoints = 100, displayName = "Floor Slam!"  },
        new KillTypeScore { causeTag = "Prop",    basePoints = 200, displayName = "Prop Kill!"   },
        new KillTypeScore { causeTag = "Enemy",   basePoints = 50,  displayName = "Basic Kill"   },
        new KillTypeScore { causeTag = "Unknown", basePoints = 50,  displayName = "Kill"         },
    };

    [Header("Chain Settings")]
    public float chainWindow     = 3f;    // seconds to continue a chain
    public float chainMultiplier = 0.5f;  // extra multiplier added per chain kill
    public int   maxChain        = 10;

    [Header("XP & Leveling")]
    public int   baseXPPerKill  = 50;
    public float xpScaling      = 0.1f;  // extra XP per chain kill
    public int   xpPerLevel     = 500;
    public int   maxLevel       = 20;

    [Header("Events")]
    public UnityEvent<int>    onScoreChanged;    // total score
    public UnityEvent<int>    onChainUpdated;    // current chain count
    public UnityEvent<int>    onXPChanged;       // current XP in level
    public UnityEvent<int>    onLevelUp;         // new level
    public UnityEvent<string, int, int> onKill;  // displayName, points, chain

    // ── State ──────────────────────────────────────────────
    public int  TotalScore   { get; private set; }
    public int  ChainCount   { get; private set; }
    public int  CurrentLevel { get; private set; } = 1;
    public int  CurrentXP    { get; private set; }
    public int  HighScore    { get; private set; }

    private float chainTimer;
    private bool  chainActive;

    const string HIGHSCORE_KEY = "HighScore";
    const string LEVEL_KEY     = "PlayerLevel";
    const string XP_KEY        = "PlayerXP";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        HighScore    = PlayerPrefs.GetInt(HIGHSCORE_KEY, 0);
        CurrentLevel = PlayerPrefs.GetInt(LEVEL_KEY, 1);
        CurrentXP    = PlayerPrefs.GetInt(XP_KEY, 0);
    }

    void Update()
    {
        if (!chainActive) return;

        chainTimer -= Time.deltaTime;

        if (chainTimer <= 0f)
            BreakChain();
    }

    // ── Public API ─────────────────────────────────────────

    public void RegisterKill(string causeTag)
    {
        KillTypeScore killType = GetKillType(causeTag);

        // Chain multiplier
        float multiplier = 1f + (ChainCount * chainMultiplier);
        int   points     = Mathf.RoundToInt(killType.basePoints * multiplier);

        // Score
        TotalScore += points;
        onScoreChanged?.Invoke(TotalScore);

        // Chain
        ChainCount  = Mathf.Min(ChainCount + 1, maxChain);
        chainTimer  = chainWindow;
        chainActive = true;
        onChainUpdated?.Invoke(ChainCount);

        // Kill feed event
        onKill?.Invoke(killType.displayName, points, ChainCount);

        // XP
        int xpGained = Mathf.RoundToInt(baseXPPerKill * (1f + ChainCount * xpScaling));
        AddXP(xpGained);

        // Highscore
        if (TotalScore > HighScore)
        {
            HighScore = TotalScore;
            PlayerPrefs.SetInt(HIGHSCORE_KEY, HighScore);
        }

        Debug.Log($"[Score] {killType.displayName} +{points} | Chain x{ChainCount} | Total {TotalScore}");
    }

    public void SaveProgress()
    {
        PlayerPrefs.SetInt(LEVEL_KEY, CurrentLevel);
        PlayerPrefs.SetInt(XP_KEY,    CurrentXP);
        PlayerPrefs.Save();
    }

    // ── Internal ───────────────────────────────────────────

    void AddXP(int amount)
    {
        if (CurrentLevel >= maxLevel) return;

        CurrentXP += amount;
        onXPChanged?.Invoke(CurrentXP);

        while (CurrentXP >= xpPerLevel && CurrentLevel < maxLevel)
        {
            CurrentXP    -= xpPerLevel;
            CurrentLevel ++;
            onLevelUp?.Invoke(CurrentLevel);
            Debug.Log($"[Score] Level up! Now level {CurrentLevel}");
        }
    }

    void BreakChain()
    {
        ChainCount  = 0;
        chainActive = false;
        onChainUpdated?.Invoke(0);
    }

    KillTypeScore GetKillType(string causeTag)
    {
        foreach (var k in killScores)
            if (k.causeTag == causeTag) return k;

        // fallback
        return new KillTypeScore
        {
            causeTag    = causeTag,
            basePoints  = 50,
            displayName = "Kill"
        };
    }
}