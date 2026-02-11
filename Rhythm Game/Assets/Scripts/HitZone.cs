using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class HitZone : MonoBehaviour
{
    [Header("Timing Windows (seconds)")]
    public float perfectWindow = 0.08f;
    public float goodWindow = 0.15f;
    public float okWindow = 0.20f;

    [Header("Colors")]
    Color perfectColor = new Color(0f, 1f, 0.53f);     // bright green
    Color goodColor = new Color(1f, 0.87f, 0f);         // yellow
    Color okColor = new Color(0.4f, 0.7f, 1f);          // light blue
    Color missColor = new Color(1f, 0.27f, 0.27f);      // red

    SpriteRenderer spriteRenderer;
    Color defaultColor;
    Coroutine flashCoroutine;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            defaultColor = spriteRenderer.color;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            JudgeHit();
        }
    }

    void JudgeHit()
    {
        NoteObject[] notes = FindObjectsByType<NoteObject>(FindObjectsSortMode.None);

        NoteObject nearest = null;
        float nearestDelta = float.MaxValue;
        float nearestRawDelta = 0f; // signed: negative = early, positive = late

        foreach (var note in notes)
        {
            if (!note.IsActive) continue;

            float rawDelta = (float)(AudioSettings.dspTime - note.HitTimeDsp);
            float absDelta = Mathf.Abs(rawDelta);
            if (absDelta < nearestDelta)
            {
                nearestDelta = absDelta;
                nearestRawDelta = rawDelta;
                nearest = note;
            }
        }

        if (nearest == null || nearestDelta > okWindow)
        {
            // No note close enough — empty press, no penalty
            return;
        }

        // Early/late suffix for feedback
        string timing = nearestRawDelta < -0.03f ? " Early" : nearestRawDelta > 0.03f ? " Late" : "";

        if (nearestDelta <= perfectWindow)
        {
            nearest.MarkHit(perfectColor);
            ScoreManager.Instance.RegisterHit("Perfect");
            UIManager.Instance.ShowFeedback("Perfect!", perfectColor);
            Flash(perfectColor);
        }
        else if (nearestDelta <= goodWindow)
        {
            nearest.MarkHit(goodColor);
            ScoreManager.Instance.RegisterHit("Good");
            UIManager.Instance.ShowFeedback("Good" + timing, goodColor);
            Flash(goodColor);
        }
        else // within okWindow
        {
            nearest.MarkHit(okColor);
            ScoreManager.Instance.RegisterHit("OK");
            UIManager.Instance.ShowFeedback("OK" + timing, okColor);
            Flash(okColor);
        }
    }

    void Flash(Color color)
    {
        if (spriteRenderer == null) return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine(color));
    }

    IEnumerator FlashRoutine(Color color)
    {
        spriteRenderer.color = new Color(color.r, color.g, color.b, 0.8f);
        yield return new WaitForSeconds(0.12f);
        spriteRenderer.color = defaultColor;
        flashCoroutine = null;
    }
}
