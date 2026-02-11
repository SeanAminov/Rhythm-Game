using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int Score { get; private set; }
    public int Combo { get; private set; }
    public int MaxCombo { get; private set; }
    public int PerfectCount { get; private set; }
    public int GoodCount { get; private set; }
    public int OkCount { get; private set; }
    public int MissCount { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ResetScore()
    {
        Score = 0;
        Combo = 0;
        MaxCombo = 0;
        PerfectCount = 0;
        GoodCount = 0;
        OkCount = 0;
        MissCount = 0;
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateScore(Score, Combo);
    }

    public void RegisterHit(string judgment)
    {
        int multiplier = 1 + Combo / 10;

        if (judgment == "Perfect")
        {
            Score += 300 * multiplier;
            Combo++;
            PerfectCount++;
        }
        else if (judgment == "Good")
        {
            Score += 100 * multiplier;
            Combo++;
            GoodCount++;
        }
        else if (judgment == "OK")
        {
            Score += 50 * multiplier;
            Combo++;
            OkCount++;
        }

        if (Combo > MaxCombo)
            MaxCombo = Combo;

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateScore(Score, Combo);
    }

    public void RegisterMiss()
    {
        Combo = 0;
        MissCount++;

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateScore(Score, Combo);
    }
}
