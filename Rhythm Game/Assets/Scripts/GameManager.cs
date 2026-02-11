using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public GameObject notePrefab;
    public Transform spawnPoint;
    public Transform hitZoneTransform;
    public AudioSource audioSource;

    [Header("Settings")]
    public float approachTime = 1.5f;

    [Header("Note Scaling")]
    public float noteStartScale = 2.5f;
    public float noteEndScale = 1.5f;

    // Audio offset in milliseconds — positive = notes arrive later (you were hitting early)
    // negative = notes arrive earlier (you were hitting late)
    float audioOffsetMs;
    public float AudioOffsetMs
    {
        get => audioOffsetMs;
        set
        {
            audioOffsetMs = Mathf.Clamp(value, -200f, 200f);
            PlayerPrefs.SetFloat("AudioOffsetMs", audioOffsetMs);
        }
    }
    // Offset in seconds for internal use
    public double AudioOffsetSec => audioOffsetMs / 1000.0;

    public SongData SongData => songData;
    public bool IsPlaying => isPlaying;
    public bool IsPaused => isPaused;
    public double StartDspTime => startDspTime;

    SongData songData;
    double startDspTime;
    int nextBeatIndex;
    bool isPlaying;
    bool isPaused;
    double pausedDspTime;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Load saved offset
        audioOffsetMs = PlayerPrefs.GetFloat("AudioOffsetMs", 0f);
    }

    void Start()
    {
        StartCoroutine(LoadAndPlay());
    }

    public void Restart()
    {
        foreach (var note in FindObjectsByType<NoteObject>(FindObjectsSortMode.None))
            Destroy(note.gameObject);

        isPlaying = false;
        isPaused = false;
        nextBeatIndex = 0;
        Time.timeScale = 1f;

        if (audioSource.isPlaying)
            audioSource.Stop();

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ResetScore();

        StartCoroutine(LoadAndPlay());
    }

    public void Pause()
    {
        if (!isPlaying || isPaused) return;
        isPaused = true;
        audioSource.Pause();
        pausedDspTime = AudioSettings.dspTime;
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        if (!isPaused) return;

        // Shift startDspTime forward by however long we were paused
        double pauseDuration = AudioSettings.dspTime - pausedDspTime;
        startDspTime += pauseDuration;

        // Also shift all active notes' hit times
        foreach (var note in FindObjectsByType<NoteObject>(FindObjectsSortMode.None))
        {
            if (note.IsActive)
                note.ShiftHitTime(pauseDuration);
        }

        isPaused = false;
        Time.timeScale = 1f;
        audioSource.UnPause();
    }

    IEnumerator LoadAndPlay()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowSongSelect(false);
            UIManager.Instance.ShowPauseMenu(false);
            UIManager.Instance.SetStatus("Loading...");
        }

        // Load JSON
        string jsonPath = Path.Combine(Application.streamingAssetsPath, "analysis.json");
        string jsonUrl = jsonPath;
        if (!jsonUrl.StartsWith("http") && !jsonUrl.StartsWith("jar"))
            jsonUrl = "file:///" + jsonUrl;

        using (UnityWebRequest jsonReq = UnityWebRequest.Get(jsonUrl))
        {
            yield return jsonReq.SendWebRequest();

            if (jsonReq.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load analysis.json: " + jsonReq.error);
                if (UIManager.Instance != null)
                    UIManager.Instance.SetStatus("Error loading analysis data");
                yield break;
            }

            songData = JsonUtility.FromJson<SongData>(jsonReq.downloadHandler.text);
            Debug.Log($"Loaded song: {songData.tempo_bpm:F1} BPM, {songData.beats_sec.Length} beats, duration {songData.duration:F1}s");
        }

        // Load Audio
        string audioPath = Path.Combine(Application.streamingAssetsPath, "song.mp3");
        string audioUrl = audioPath;
        if (!audioUrl.StartsWith("http") && !audioUrl.StartsWith("jar"))
            audioUrl = "file:///" + audioUrl;

        using (UnityWebRequest audioReq = UnityWebRequestMultimedia.GetAudioClip(audioUrl, AudioType.MPEG))
        {
            yield return audioReq.SendWebRequest();

            if (audioReq.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load song: " + audioReq.error);
                if (UIManager.Instance != null)
                    UIManager.Instance.SetStatus("Error loading audio");
                yield break;
            }

            audioSource.clip = DownloadHandlerAudioClip.GetContent(audioReq);
        }

        if (UIManager.Instance != null)
            UIManager.Instance.SetStatus("");

        // Schedule playback
        nextBeatIndex = 0;
        startDspTime = AudioSettings.dspTime + 2.0;
        audioSource.PlayScheduled(startDspTime);
        isPlaying = true;
        isPaused = false;

        Debug.Log($"Playback scheduled (offset: {audioOffsetMs:+0;-0;0}ms)");
    }

    void Update()
    {
        if (!isPlaying || songData == null || isPaused) return;

        double currentDsp = AudioSettings.dspTime;

        // Spawn notes — offset shifts when the note should be hit relative to audio
        while (nextBeatIndex < songData.beats_sec.Length)
        {
            float hitTimeSec = songData.beats_sec[nextBeatIndex];
            double hitTimeDsp = startDspTime + hitTimeSec + AudioOffsetSec;
            double spawnTimeDsp = hitTimeDsp - approachTime;

            if (currentDsp >= spawnTimeDsp)
            {
                SpawnNote(hitTimeDsp);
                nextBeatIndex++;
            }
            else
            {
                break;
            }
        }

        // Song end
        if (nextBeatIndex >= songData.beats_sec.Length && audioSource.clip != null)
        {
            double songEndDsp = startDspTime + audioSource.clip.length;
            if (currentDsp > songEndDsp + 1.0)
            {
                isPlaying = false;
                OnSongEnd();
            }
        }
    }

    void SpawnNote(double hitTimeDsp)
    {
        if (notePrefab == null || spawnPoint == null || hitZoneTransform == null) return;

        GameObject noteGO = Instantiate(notePrefab, spawnPoint.position, Quaternion.identity);
        NoteObject noteObj = noteGO.GetComponent<NoteObject>();
        noteObj.Initialize(hitTimeDsp, approachTime, spawnPoint.position, hitZoneTransform.position,
                          noteStartScale, noteEndScale);
    }

    void OnSongEnd()
    {
        Debug.Log("Song complete!");
        if (UIManager.Instance != null)
        {
            var sm = ScoreManager.Instance;
            string results = $"Song Complete!\nScore: {sm.Score}\nMax Combo: {sm.MaxCombo}x\n" +
                             $"Perfect: {sm.PerfectCount} | Good: {sm.GoodCount} | OK: {sm.OkCount} | Miss: {sm.MissCount}";
            UIManager.Instance.ShowFeedback(results, Color.white);
            UIManager.Instance.ShowSongSelect(true);
        }
    }
}
