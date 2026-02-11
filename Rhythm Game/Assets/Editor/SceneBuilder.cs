using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using TMPro;

public class SceneBuilder
{
    [MenuItem("Rhythm Game/Build Scene")]
    public static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // === CAMERA ===
        Camera cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.backgroundColor = new Color(0.06f, 0.06f, 0.12f);

        // === CREATE SPRITES ===
        string spriteDir = "Assets/Sprites";
        if (!AssetDatabase.IsValidFolder(spriteDir))
            AssetDatabase.CreateFolder("Assets", "Sprites");

        Texture2D filledTex = CreateCircleTexture(128, false);
        SaveTextureAsset(filledTex, "Assets/Sprites/CircleFilled.asset");
        CreateAndSaveSprite(filledTex, "Assets/Sprites/CircleFilled_Sprite.asset", 128);

        Texture2D ringTex = CreateCircleTexture(128, true);
        SaveTextureAsset(ringTex, "Assets/Sprites/CircleRing.asset");
        CreateAndSaveSprite(ringTex, "Assets/Sprites/CircleRing_Sprite.asset", 128);

        AssetDatabase.SaveAssets();

        Sprite filledSpriteAsset = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/CircleFilled_Sprite.asset");
        Sprite ringSpriteAsset = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/CircleRing_Sprite.asset");

        // === SPAWN POINT ===
        GameObject spawnPoint = new GameObject("SpawnPoint");
        spawnPoint.transform.position = new Vector3(0f, 6f, 0f);

        // === HIT ZONE ===
        GameObject hitZone = new GameObject("HitZone");
        hitZone.transform.position = new Vector3(0f, -3.5f, 0f);
        hitZone.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
        SpriteRenderer hitZoneSR = hitZone.AddComponent<SpriteRenderer>();
        hitZoneSR.sprite = ringSpriteAsset;
        hitZoneSR.color = new Color(0.4f, 0.6f, 1f, 0.6f);
        hitZoneSR.sortingOrder = 2;
        hitZone.AddComponent<HitZone>();

        // === NOTE PREFAB ===
        string prefabDir = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabDir))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        GameObject notePrefabGO = new GameObject("NotePrefab");
        notePrefabGO.transform.localScale = Vector3.one;
        SpriteRenderer noteSR = notePrefabGO.AddComponent<SpriteRenderer>();
        noteSR.sprite = filledSpriteAsset;
        noteSR.color = new Color(0.3f, 0.85f, 1f, 1f);
        noteSR.sortingOrder = 1;
        notePrefabGO.AddComponent<NoteObject>();

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(notePrefabGO, "Assets/Prefabs/NotePrefab.prefab");
        Object.DestroyImmediate(notePrefabGO);

        // === GAME MANAGER ===
        GameObject gmGO = new GameObject("GameManager");
        GameManager gm = gmGO.AddComponent<GameManager>();
        AudioSource audioSrc = gmGO.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        gmGO.AddComponent<ScoreManager>();
        gmGO.AddComponent<SongAnalyzer>();

        gm.notePrefab = savedPrefab;
        gm.spawnPoint = spawnPoint.transform;
        gm.hitZoneTransform = hitZone.transform;
        gm.audioSource = audioSrc;
        gm.approachTime = 1.5f;
        gm.noteStartScale = 2.5f;
        gm.noteEndScale = 1.5f;

        // === CANVAS ===
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // =====================
        // HUD - TOP BAR
        // =====================

        // Score label + value (top-right, padded inward)
        TextMeshProUGUI scoreLabel = CreateTMPText(canvasGO.transform, "ScoreLabel", "SCORE",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-160, -20),
            new Vector2(200, 30), 18, TextAlignmentOptions.Right);
        scoreLabel.color = new Color(0.5f, 0.5f, 0.65f);

        TextMeshProUGUI scoreText = CreateTMPText(canvasGO.transform, "ScoreText", "0",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-160, -52),
            new Vector2(200, 50), 42, TextAlignmentOptions.Right);

        // Combo (top-center)
        TextMeshProUGUI comboText = CreateTMPText(canvasGO.transform, "ComboText", "",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40),
            new Vector2(400, 50), 36, TextAlignmentOptions.Center);
        comboText.color = new Color(1f, 0.87f, 0f);

        // Song info (top-left)
        TextMeshProUGUI songInfoText = CreateTMPText(canvasGO.transform, "SongInfoText", "",
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -25),
            new Vector2(300, 30), 18, TextAlignmentOptions.Left);
        songInfoText.color = new Color(0.5f, 0.5f, 0.65f);

        // Offset display (top-left, below song info)
        TextMeshProUGUI offsetDisplayText = CreateTMPText(canvasGO.transform, "OffsetDisplayText", "Offset: +0ms",
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -50),
            new Vector2(300, 25), 16, TextAlignmentOptions.Left);
        offsetDisplayText.color = new Color(0.45f, 0.45f, 0.6f);

        // Feedback text (near hit zone)
        TextMeshProUGUI feedbackText = CreateTMPText(canvasGO.transform, "FeedbackText", "",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -220),
            new Vector2(600, 200), 56, TextAlignmentOptions.Center);
        feedbackText.fontStyle = FontStyles.Bold;

        // =====================
        // PAUSE MENU
        // =====================
        GameObject pausePanel = CreatePanel(canvasGO.transform, "PausePanel",
            new Vector2(500, 380), Vector2.zero, new Color(0.08f, 0.08f, 0.16f, 0.95f));

        CreateTMPText(pausePanel.transform, "PauseTitle", "PAUSED",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20),
            new Vector2(400, 50), 40, TextAlignmentOptions.Center);

        // Offset controls
        CreateTMPText(pausePanel.transform, "OffsetLabel", "Audio Offset",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -80),
            new Vector2(400, 30), 20, TextAlignmentOptions.Center);

        // Offset value display
        TextMeshProUGUI offsetValueText = CreateTMPText(pausePanel.transform, "OffsetValue", "+0ms",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -110),
            new Vector2(150, 40), 32, TextAlignmentOptions.Center);
        offsetValueText.color = new Color(0.3f, 0.85f, 1f);

        // Offset buttons row: -5  -1  [value]  +1  +5
        CreateButton(pausePanel.transform, "OffsetMinus5", "-5ms",
            new Vector2(0.5f, 1), new Vector2(-160, -115), new Vector2(70, 36),
            new Color(0.25f, 0.25f, 0.4f), 16);
        CreateButton(pausePanel.transform, "OffsetMinus1", "-1ms",
            new Vector2(0.5f, 1), new Vector2(-85, -115), new Vector2(60, 36),
            new Color(0.25f, 0.25f, 0.4f), 16);
        CreateButton(pausePanel.transform, "OffsetPlus1", "+1ms",
            new Vector2(0.5f, 1), new Vector2(85, -115), new Vector2(60, 36),
            new Color(0.25f, 0.25f, 0.4f), 16);
        CreateButton(pausePanel.transform, "OffsetPlus5", "+5ms",
            new Vector2(0.5f, 1), new Vector2(160, -115), new Vector2(70, 36),
            new Color(0.25f, 0.25f, 0.4f), 16);

        // Offset hint
        TextMeshProUGUI offsetHint = CreateTMPText(pausePanel.transform, "OffsetHint",
            "Notes early? Increase offset. Notes late? Decrease.\nArrow keys: Left/Right ±1ms, Up/Down ±5ms",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -160),
            new Vector2(450, 50), 14, TextAlignmentOptions.Center);
        offsetHint.color = new Color(0.45f, 0.45f, 0.6f);

        // Resume button
        CreateButton(pausePanel.transform, "ResumeButton", "Resume",
            new Vector2(0.5f, 0), new Vector2(0, 100), new Vector2(200, 45),
            new Color(0.15f, 0.55f, 0.3f), 22);

        // Restart button
        CreateButton(pausePanel.transform, "RestartButton", "Restart",
            new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(200, 45),
            new Color(0.55f, 0.25f, 0.15f), 22);

        pausePanel.SetActive(false);

        // =====================
        // SONG SELECT PANEL
        // =====================
        GameObject songSelectPanel = CreatePanel(canvasGO.transform, "SongSelectPanel",
            new Vector2(700, 200), new Vector2(0, 50), new Color(0.12f, 0.12f, 0.22f, 0.95f));

        CreateTMPText(songSelectPanel.transform, "TitleLabel",
            "Paste a song file path below, then press Analyze & Play",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -15),
            new Vector2(650, 40), 20, TextAlignmentOptions.Center).color = new Color(0.7f, 0.7f, 0.85f);

        // Input field
        GameObject inputGO = new GameObject("SongPathInput");
        inputGO.transform.SetParent(songSelectPanel.transform, false);
        RectTransform inputRect = inputGO.AddComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.5f, 0.5f);
        inputRect.anchorMax = new Vector2(0.5f, 0.5f);
        inputRect.sizeDelta = new Vector2(600, 40);
        inputRect.anchoredPosition = new Vector2(0, 10);
        inputGO.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.28f, 1f);
        TMP_InputField inputField = inputGO.AddComponent<TMP_InputField>();

        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(inputGO.transform, false);
        RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(10, 0);
        textAreaRect.offsetMax = new Vector2(-10, 0);
        textArea.AddComponent<RectMask2D>();

        TextMeshProUGUI placeholder = CreateTMPText(textArea.transform, "Placeholder",
            @"e.g. C:\Music\song.mp3",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 18, TextAlignmentOptions.Left);
        StretchRect(placeholder.GetComponent<RectTransform>());
        placeholder.color = new Color(0.45f, 0.45f, 0.55f);

        TextMeshProUGUI inputText = CreateTMPText(textArea.transform, "Text", "",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 18, TextAlignmentOptions.Left);
        StretchRect(inputText.GetComponent<RectTransform>());

        inputField.textViewport = textAreaRect;
        inputField.textComponent = inputText;
        inputField.placeholder = placeholder;

        // Analyze button
        CreateButton(songSelectPanel.transform, "AnalyzeButton", "Analyze & Play",
            new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(220, 42),
            new Color(0.15f, 0.55f, 0.3f), 20);
        Button analyzeBtn = songSelectPanel.transform.Find("AnalyzeButton").GetComponent<Button>();

        // Status text
        TextMeshProUGUI statusText = CreateTMPText(canvasGO.transform, "StatusText", "",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -80),
            new Vector2(600, 40), 20, TextAlignmentOptions.Center);
        statusText.color = new Color(0.6f, 0.6f, 0.75f);

        // Song select controller
        SongSelectController ssc = songSelectPanel.AddComponent<SongSelectController>();
        ssc.songPathInput = inputField;
        ssc.analyzeButton = analyzeBtn;

        songSelectPanel.SetActive(false);

        // =====================
        // WIRE UP UI MANAGER
        // =====================
        UIManager uiMgr = canvasGO.AddComponent<UIManager>();
        uiMgr.scoreText = scoreText;
        uiMgr.comboText = comboText;
        uiMgr.feedbackText = feedbackText;
        uiMgr.offsetDisplayText = offsetDisplayText;
        uiMgr.songInfoText = songInfoText;
        uiMgr.pausePanel = pausePanel;
        uiMgr.offsetValueText = offsetValueText;
        uiMgr.songSelectPanel = songSelectPanel;
        uiMgr.songPathInput = inputField;
        uiMgr.statusText = statusText;

        // Pause menu buttons are wired at runtime by UIManager.Start()

        // =====================
        // CONTROLS HINT (bottom)
        // =====================
        TextMeshProUGUI controlsHint = CreateTMPText(canvasGO.transform, "ControlsHint",
            "SPACE = Hit  |  ESC = Pause",
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20),
            new Vector2(500, 30), 16, TextAlignmentOptions.Center);
        controlsHint.color = new Color(0.35f, 0.35f, 0.5f);

        // === EVENT SYSTEM ===
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // === SAVE ===
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/RhythmGame.unity");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Rhythm Game scene built! Run: Rhythm Game > Build Scene, then press Play.");
    }

    // =====================
    // HELPER METHODS
    // =====================

    static GameObject CreatePanel(Transform parent, string name, Vector2 size, Vector2 pos, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        panel.AddComponent<Image>().color = color;
        return panel;
    }

    static void CreateButton(Transform parent, string name, string label,
        Vector2 anchor, Vector2 pos, Vector2 size, Color bgColor, int fontSize)
    {
        GameObject btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);
        RectTransform rect = btnGO.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        btnGO.AddComponent<Image>().color = bgColor;
        btnGO.AddComponent<Button>();

        TextMeshProUGUI txt = CreateTMPText(btnGO.transform, "Text", label,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, fontSize, TextAlignmentOptions.Center);
        StretchRect(txt.GetComponent<RectTransform>());
    }

    static void StretchRect(RectTransform rect)
    {
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static TextMeshProUGUI CreateTMPText(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos,
        Vector2 sizeDelta, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;

        return tmp;
    }

    static void SaveTextureAsset(Texture2D tex, string path)
    {
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(tex, path);
    }

    static Sprite CreateAndSaveSprite(Texture2D tex, string path, int resolution)
    {
        AssetDatabase.DeleteAsset(path);
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, resolution, resolution),
                                       new Vector2(0.5f, 0.5f), resolution);
        sprite.name = System.IO.Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(sprite, path);
        return sprite;
    }

    static Texture2D CreateCircleTexture(int resolution, bool ringOnly)
    {
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        float center = resolution / 2f;
        float outerRadius = center - 1f;
        float ringWidth = resolution * 0.06f;
        float innerRadius = outerRadius - ringWidth;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));

                if (ringOnly)
                {
                    if (dist >= innerRadius && dist <= outerRadius)
                    {
                        float alpha = 1f;
                        if (dist > outerRadius - 1f)
                            alpha = Mathf.Clamp01(outerRadius - dist + 1f);
                        else if (dist < innerRadius + 1f)
                            alpha = Mathf.Clamp01(dist - innerRadius + 1f);
                        tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
                else
                {
                    if (dist <= outerRadius)
                    {
                        float alpha = 1f;
                        if (dist > outerRadius - 1f)
                            alpha = Mathf.Clamp01(outerRadius - dist + 1f);
                        tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }
}
