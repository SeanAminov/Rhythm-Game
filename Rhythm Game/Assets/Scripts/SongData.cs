using System;

[Serializable]
public class SongData
{
    public int sr;
    public float duration;
    public float tempo_bpm;
    public float[] beats_sec;
    public float[] downbeats_sec;
    public float[] onsets_sec;
    public float[] strong_onsets_sec;
}
