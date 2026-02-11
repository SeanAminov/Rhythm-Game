using UnityEngine;

public class NoteObject : MonoBehaviour
{
    public double HitTimeDsp { get; set; }
    public bool IsActive { get; private set; } = true;

    double approachTime;
    Vector3 spawnPos;
    Vector3 targetPos;
    bool missRegistered;
    SpriteRenderer spriteRenderer;

    // Visual: note shrinks as it approaches the hit zone
    float startScale;
    float targetScale;

    public void Initialize(double hitTimeDsp, double approachTime, Vector3 spawnPos, Vector3 targetPos,
                           float startScale, float targetScale)
    {
        HitTimeDsp = hitTimeDsp;
        this.approachTime = approachTime;
        this.spawnPos = spawnPos;
        this.targetPos = targetPos;
        this.startScale = startScale;
        this.targetScale = targetScale;
        transform.position = spawnPos;
        transform.localScale = Vector3.one * startScale;

        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (!IsActive) return;

        double currentDsp = AudioSettings.dspTime;
        double spawnTimeDsp = HitTimeDsp - approachTime;
        double elapsed = currentDsp - spawnTimeDsp;
        float t = (float)(elapsed / approachTime);

        if (t < 0f) t = 0f;

        // Move toward hit zone
        transform.position = Vector3.Lerp(spawnPos, targetPos, t);

        // Shrink as it approaches — at t=1.0 it should be exactly the size of the hit zone ring
        float scale = Mathf.Lerp(startScale, targetScale, t);
        transform.localScale = Vector3.one * scale;

        // Fade in slightly at the start
        if (spriteRenderer != null && t < 0.15f)
        {
            Color c = spriteRenderer.color;
            c.a = Mathf.Lerp(0.3f, 1f, t / 0.15f);
            spriteRenderer.color = c;
        }

        // Auto-miss if note passes well beyond hit zone
        if (!missRegistered && currentDsp > HitTimeDsp + 0.2)
        {
            missRegistered = true;
            IsActive = false;

            if (ScoreManager.Instance != null)
                ScoreManager.Instance.RegisterMiss();
            if (UIManager.Instance != null)
                UIManager.Instance.ShowFeedback("Miss", new Color(1f, 0.27f, 0.27f));

            Destroy(gameObject, 0.2f);
        }
    }

    public void ShiftHitTime(double delta)
    {
        HitTimeDsp += delta;
    }

    public void MarkHit(Color flashColor)
    {
        IsActive = false;
        if (spriteRenderer != null)
            spriteRenderer.color = flashColor;
        // Quick pop effect — scale up slightly then destroy
        transform.localScale *= 1.2f;
        Destroy(gameObject, 0.08f);
    }
}
