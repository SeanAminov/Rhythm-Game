# Rhythm Game

A Unity rhythm game where the **song drives the gameplay**: you upload a music file, the system extracts beats and onsets, and the game spawns objects in sync with the music. The player hits them on time.

**Big idea:** A song acts as a **procedural seed** that generates the rhythm and hit pattern. This repo is the Unity game; the song analysis pipeline lives in the parent **musicmap** folder.

---

## Project overview

- **Player** uploads a music file (MP3/WAV).
- **Analysis** (Python, outside this repo) extracts:
  - **Tempo (BPM)**
  - **Beat timestamps** (quarter-note grid)
  - **Onset timestamps** (percussive/hit-like events)
- **Export:** analysis data is saved as **JSON**.
- **Unity** loads the JSON and the same audio clip, plays the music, and **spawns objects** so they arrive at a hit zone exactly on the beat/onset.
- **Sync** uses **`AudioSettings.dspTime`** so spawning and movement are frame-independent and stay in sync with the audio.

Current focus: **beat/onset extraction and spawning** for a rhythm shooter prototype. Later: expand the “song as seed” idea (e.g. levels, difficulty, patterns derived from the track).

---

## How song analysis works

The analysis pipeline is **not in this repo**. It lives in:

- **Folder:** `D:\musicmap` (parent of this repo, or the “musicmap” workspace).
- **Script:** `analyze_song.py`

### What it does

1. **Loads audio** with **librosa** (mono). For MP3 it uses **ffmpeg** to convert to a temporary WAV first (avoids Windows/audioread issues).
2. **Extracts:**
   - **Tempo (BPM)** via `librosa.beat.beat_track`
   - **Beat times (seconds)** from the beat grid
   - **Onset times (seconds)** from `librosa.onset.onset_detect` (with minimum spacing ~0.08 s to avoid clutter)
3. **Writes JSON** with this structure:

```json
{
  "sr": 44100,
  "tempo_bpm": 114.84,
  "beats_sec": [0.46, 0.95, 1.47, ...],
  "onsets_sec": [0.27, 0.61, 0.92, ...]
}
```

### How to run analysis

From `D:\musicmap` (with Python venv activated and ffmpeg on PATH):

```bash
python analyze_song.py "path\to\song.mp3" analysis.json
```

- **Input:** Any path to an MP3 or WAV (paths are normalized; MP3 is converted to temp WAV under the script folder).
- **Output:** `analysis.json` (or the path you pass as the second argument).

Copy the generated **analysis.json** and the **same audio file** into the Unity project (e.g. `Assets/StreamingAssets/`) so the game can load them.

### Tech stack (analysis)

- **Python 3.12**
- **librosa** (BSD), **soundfile**, **numpy**
- **ffmpeg** (on PATH) for MP3 → WAV when needed

---

## How the Unity game should work

### Sync model

- **Do not** use `Time.time` or frame count for when notes appear or when the player hits. Use **audio time** so sync is independent of frame rate.
- **Use `AudioSettings.dspTime`** as the reference clock. When you start playback, schedule the clip with `AudioSource.PlayScheduled(startDspTime)` and store that `startDspTime`. Then “current time in the song” is:

  `songTime = AudioSettings.dspTime - startDspTime`

- **Spawn time** for each note:

  `spawnTimeDsp = hitTimeDsp - approachTime`

  where `hitTimeDsp = startDspTime + hitTimeSec` (hitTimeSec from JSON). So the note is spawned `approachTime` seconds before it should be hit, and moves toward the hit zone so it **arrives** at `hitTimeDsp`.

- **Movement:** Update each note’s position so that at `AudioSettings.dspTime == hitTimeDsp` it is in the hit zone. Interpolate between spawn position and hit-zone position using DSP time (not deltaTime).

### Data flow in Unity

1. **Load** the analysis JSON (e.g. from StreamingAssets or as a TextAsset). Parse into a structure that matches the JSON: `sr`, `tempo_bpm`, `beats_sec[]`, `onsets_sec[]`.
2. **Load** the same song as an **AudioClip** (the one that was analyzed).
3. **On start:** Schedule playback with `PlayScheduled(startDspTime)` and keep `startDspTime`.
4. **Each frame:** For each beat or onset time in the JSON, compute `hitDsp = startDspTime + timeSec`. If `hitDsp - approachTime <= currentDspTime` and that note hasn’t been spawned yet, **spawn** a note and give it `hitDsp` and `approachTime` so it can move correctly.
5. **Note movement:** Each note updates its position so it goes from spawn point to hit zone and **reaches the hit zone at `hitDsp`** (using `AudioSettings.dspTime`).
6. **Hit detection:** When the player presses the hit key or clicks, determine which note is in the hit zone and check if current DSP time is within a small window around that note’s `hitDsp` (e.g. ±0.08 s). Score hit/miss accordingly.

### Design choices

- **Beats vs onsets:**  
  - **Beats** = quarter-note grid (fewer, regular).  
  - **Onsets** = more events, good for “hit every punch” feel.  
  The game can offer a toggle or use one by default (e.g. onsets for a shooter feel).
- **Approach time:** Tune so notes are on screen long enough to read but not so long that the screen is crowded (e.g. 1–2 seconds).

---

## Repo layout

- **`Rhythm Game/`** – Unity 2D project (open this folder in Unity Hub).
- **`README.md`** – This file (project idea, analysis, and Unity design).
- **`.gitignore`** – Unity (Library, Temp, etc.) and OS cruft.

The **analyzer** and **analysis.json** are in **`D:\musicmap`**, not in this repo. Copy `analysis.json` and the song into the Unity project when you add new tracks.

---

## Quick reference: end-to-end flow

1. **Analyze (in musicmap):**  
   `python analyze_song.py "path\to\song.mp3" analysis.json`
2. **Copy into Unity:**  
   Put `analysis.json` and the song (e.g. MP3/WAV) in `Assets/StreamingAssets/` (or similar).
3. **Unity:**  
   Load JSON + clip → `PlayScheduled(startDspTime)` → spawn notes at `hitDsp - approachTime` → move notes so they reach the hit zone at `hitDsp` → judge hits with a small window around `hitDsp` using `AudioSettings.dspTime`.

This README is the single place that describes the **idea**, **how analysis works**, and **how the Unity game should be built** so any new coding session or interface has full context.
