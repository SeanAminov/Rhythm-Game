using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SongSelectController : MonoBehaviour
{
    public TMP_InputField songPathInput;
    public Button analyzeButton;

    void Start()
    {
        if (analyzeButton != null)
            analyzeButton.onClick.AddListener(OnAnalyzeClicked);
    }

    void OnAnalyzeClicked()
    {
        if (songPathInput == null) return;

        string path = songPathInput.text.Trim().Trim('"');
        if (string.IsNullOrEmpty(path))
        {
            if (UIManager.Instance != null)
                UIManager.Instance.SetStatus("Please enter a file path first.");
            return;
        }

        if (!System.IO.File.Exists(path))
        {
            if (UIManager.Instance != null)
                UIManager.Instance.SetStatus("File not found: " + path);
            return;
        }

        if (SongAnalyzer.Instance != null)
            SongAnalyzer.Instance.AnalyzeAndPlay(path);
    }
}
