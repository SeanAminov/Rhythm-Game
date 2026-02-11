using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Gameplay HUD")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI offsetDisplayText;
    public TextMeshProUGUI songInfoText;

    [Header("Pause Menu")]
    public GameObject pausePanel;
    public TextMeshProUGUI offsetValueText;

    [Header("Song Select UI")]
    public GameObject songSelectPanel;
    public TMP_InputField songPathInput;
    public TextMeshProUGUI statusText;

    Coroutine feedbackCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (feedbackText != null)
            feedbackText.text = "";
        UpdateOffsetDisplay();
        UpdateSongInfo();

        // Wire pause menu buttons at runtime (editor-created runtime listeners don't persist in scene)
        if (pausePanel != null)
        {
            WireButtonByName(pausePanel.transform, "ResumeButton", OnResumeClicked);
            WireButtonByName(pausePanel.transform, "RestartButton", OnRestartClicked);
            WireButtonByName(pausePanel.transform, "OffsetMinus5", OnOffsetMinusClicked);
            WireButtonByName(pausePanel.transform, "OffsetMinus1", OnOffsetMinusSmallClicked);
            WireButtonByName(pausePanel.transform, "OffsetPlus1", OnOffsetPlusSmallClicked);
            WireButtonByName(pausePanel.transform, "OffsetPlus5", OnOffsetPlusClicked);
        }
    }

    void WireButtonByName(Transform parent, string name, UnityEngine.Events.UnityAction action)
    {
        Transform t = parent.Find(name);
        if (t != null)
        {
            var btn = t.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
                btn.onClick.AddListener(action);
        }
    }

    void Update()
    {
        // ESC to toggle pause
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

            if (GameManager.Instance.IsPaused)
            {
                GameManager.Instance.Resume();
                ShowPauseMenu(false);
            }
            else
            {
                GameManager.Instance.Pause();
                ShowPauseMenu(true);
            }
        }

        // While paused, allow offset adjustment with arrow keys
        if (GameManager.Instance != null && GameManager.Instance.IsPaused && Keyboard.current != null)
        {
            // Left/Right: ±1ms, Up/Down: ±5ms
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
                AdjustOffset(-1f);
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
                AdjustOffset(1f);
            if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                AdjustOffset(-5f);
            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                AdjustOffset(5f);
        }
    }

    public void AdjustOffset(float deltaMs)
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.AudioOffsetMs += deltaMs;
        UpdateOffsetDisplay();
    }

    void UpdateOffsetDisplay()
    {
        if (GameManager.Instance == null) return;

        float ms = GameManager.Instance.AudioOffsetMs;
        string sign = ms >= 0 ? "+" : "";
        string display = $"Offset: {sign}{ms:0}ms";

        if (offsetDisplayText != null)
            offsetDisplayText.text = display;
        if (offsetValueText != null)
            offsetValueText.text = $"{sign}{ms:0}ms";
    }

    void UpdateSongInfo()
    {
        if (songInfoText == null || GameManager.Instance == null) return;
        var sd = GameManager.Instance.SongData;
        if (sd != null)
            songInfoText.text = $"{sd.tempo_bpm:F0} BPM";
        else
            songInfoText.text = "";
    }

    public void UpdateScore(int score, int combo)
    {
        if (scoreText != null)
            scoreText.text = score.ToString();
        if (comboText != null)
            comboText.text = combo > 1 ? combo + "x COMBO" : "";
    }

    public void ShowFeedback(string judgment, Color color)
    {
        if (feedbackText == null) return;

        if (feedbackCoroutine != null)
            StopCoroutine(feedbackCoroutine);

        feedbackText.text = judgment;
        feedbackText.color = color;

        bool isSongEnd = judgment.Contains("Song Complete");
        feedbackCoroutine = StartCoroutine(FadeFeedback(isSongEnd ? 10f : 0.5f));

        // Update song info when it becomes available
        UpdateSongInfo();
    }

    IEnumerator FadeFeedback(float duration)
    {
        yield return new WaitForSeconds(duration);
        feedbackText.text = "";
        feedbackCoroutine = null;
    }

    public void ShowPauseMenu(bool show)
    {
        if (pausePanel != null)
            pausePanel.SetActive(show);
        if (show)
            UpdateOffsetDisplay();
    }

    public void ShowSongSelect(bool show)
    {
        if (songSelectPanel != null)
            songSelectPanel.SetActive(show);
    }

    public void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    // Called from pause menu buttons
    public void OnResumeClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Resume();
            ShowPauseMenu(false);
        }
    }

    public void OnRestartClicked()
    {
        if (GameManager.Instance != null)
        {
            ShowPauseMenu(false);
            Time.timeScale = 1f;
            GameManager.Instance.Restart();
        }
    }

    public void OnOffsetMinusClicked() => AdjustOffset(-5f);
    public void OnOffsetPlusClicked() => AdjustOffset(5f);
    public void OnOffsetMinusSmallClicked() => AdjustOffset(-1f);
    public void OnOffsetPlusSmallClicked() => AdjustOffset(1f);
}
