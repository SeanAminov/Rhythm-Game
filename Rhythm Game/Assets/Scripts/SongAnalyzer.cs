using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Collections;
using Debug = UnityEngine.Debug;

public class SongAnalyzer : MonoBehaviour
{
    public static SongAnalyzer Instance { get; private set; }

    [Header("Python Paths (auto-detected for this machine)")]
    public string pythonExe = @"D:\musicmap\venv\Scripts\python.exe";
    public string analyzerScript = @"D:\musicmap\analyze_song.py";

    bool isAnalyzing;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AnalyzeAndPlay(string audioFilePath)
    {
        if (isAnalyzing)
        {
            Debug.LogWarning("Analysis already in progress.");
            return;
        }

        StartCoroutine(AnalyzeCoroutine(audioFilePath));
    }

    IEnumerator AnalyzeCoroutine(string audioFilePath)
    {
        isAnalyzing = true;

        if (UIManager.Instance != null)
            UIManager.Instance.SetStatus("Analyzing song...");

        string outputJson = Path.Combine(Application.streamingAssetsPath, "analysis.json");

        // Ensure StreamingAssets folder exists
        Directory.CreateDirectory(Application.streamingAssetsPath);

        // Run Python analyzer
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = $"\"{analyzerScript}\" \"{audioFilePath}\" \"{outputJson}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        Debug.Log($"Running: {psi.FileName} {psi.Arguments}");

        Process process = new Process { StartInfo = psi };
        bool started = false;

        try
        {
            started = process.Start();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to start analyzer: {ex.Message}");
            if (UIManager.Instance != null)
                UIManager.Instance.SetStatus("Error: Could not start Python analyzer");
            isAnalyzing = false;
            yield break;
        }

        if (!started)
        {
            Debug.LogError("Failed to start analyzer process.");
            isAnalyzing = false;
            yield break;
        }

        // Wait for process to finish (check every frame)
        while (!process.HasExited)
        {
            yield return null;
        }

        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();

        if (!string.IsNullOrEmpty(stdout))
            Debug.Log("Analyzer: " + stdout);
        if (!string.IsNullOrEmpty(stderr))
            Debug.LogWarning("Analyzer stderr: " + stderr);

        if (process.ExitCode != 0)
        {
            Debug.LogError($"Analyzer failed with exit code {process.ExitCode}");
            if (UIManager.Instance != null)
                UIManager.Instance.SetStatus("Analysis failed — check console");
            isAnalyzing = false;
            yield break;
        }

        // Copy audio file to StreamingAssets as song.mp3
        string destAudio = Path.Combine(Application.streamingAssetsPath, "song.mp3");
        try
        {
            File.Copy(audioFilePath, destAudio, overwrite: true);
            Debug.Log($"Copied audio to {destAudio}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to copy audio file: {ex.Message}");
            if (UIManager.Instance != null)
                UIManager.Instance.SetStatus("Error copying audio file");
            isAnalyzing = false;
            yield break;
        }

        if (UIManager.Instance != null)
            UIManager.Instance.SetStatus("Analysis complete! Starting game...");

        isAnalyzing = false;

        // Restart the game with new data
        yield return new WaitForSeconds(0.5f);
        if (GameManager.Instance != null)
            GameManager.Instance.Restart();
    }
}
