using System.Collections.Generic;
using UnityEngine;

namespace POKE.PMDCollab.OpenWorld
{
    public enum PMDSfx
    {
        Hit,
        SuperEffective,
        NotVeryEffective,
        Melee,
        Projectile,
        Dash,
        Ultimate,
        Pickup,
        Coin,
        LevelUp,
        Recruit,
        Error,
        Gate,
        Faint,
        PlayerHurt,
        MenuClick,
        MenuOpen,
        QuestComplete,
        Heal,
        GameOver,
        Evolve,
        Footstep,
        BossRoar,
        ChestOpen,
        BallThrow,
        BallShake,
        BallFail
    }

    /// <summary>
    /// Bộ âm thanh tự tổng hợp (không cần file âm thanh): Hiệu ứng chiêu, đánh trúng, nhặt đồ, lên cấp, chuyển vùng...
    /// và Nhạc nền chiptune 8-bit riêng cho từng vùng đất, phòng trong nhà và trận đánh Trùm.
    /// </summary>
    public class PMDAudio : MonoBehaviour
    {
        private const int SampleRate = 22050;

        public static PMDAudio Instance { get; private set; }

        private AudioSource _musicSource;
        private AudioSource _sfxSource;
        private readonly Dictionary<PMDSfx, AudioClip> _sfxCache = new Dictionary<PMDSfx, AudioClip>();
        private readonly Dictionary<string, AudioClip> _musicCache = new Dictionary<string, AudioClip>();
        private readonly Dictionary<PMDSfx, float> _lastPlayTime = new Dictionary<PMDSfx, float>();
        private string _currentMusicKey;

        private void Awake()
        {
            Instance = this;
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            _musicSource.ignoreListenerPause = true;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.ignoreListenerPause = true;

            if (FindFirstObjectByType<AudioListener>() == null)
            {
                var cam = Camera.main;
                (cam != null ? cam.gameObject : gameObject).AddComponent<AudioListener>();
            }
        }

        private void Update()
        {
            var gs = PMDGameState.Instance;
            if (gs != null && _musicSource != null)
            {
                float target = gs.MusicVolume * (gs.IsPaused || gs.IsMainMenuOpen ? 0.55f : 1f) * 0.45f;
                _musicSource.volume = Mathf.MoveTowards(_musicSource.volume, target, Time.unscaledDeltaTime * 0.8f);
            }
        }

        public static void Play(PMDSfx sfx, float volumeScale = 1f)
        {
            if (Instance == null)
                return;
            Instance.PlayInternal(sfx, volumeScale);
        }

        private void PlayInternal(PMDSfx sfx, float volumeScale)
        {
            float now = Time.unscaledTime;
            if (_lastPlayTime.TryGetValue(sfx, out float last) && now - last < 0.035f)
                return; // avoid stacking the same sound in a single frame
            _lastPlayTime[sfx] = now;

            if (!_sfxCache.TryGetValue(sfx, out AudioClip clip) || clip == null)
            {
                clip = GenerateSfx(sfx);
                _sfxCache[sfx] = clip;
            }

            float vol = (PMDGameState.Instance != null ? PMDGameState.Instance.SfxVolume : 0.8f) * volumeScale;
            _sfxSource.PlayOneShot(clip, vol);
        }

        public static void PlayMusic(RegionBiomeStyle biome, bool isBoss)
        {
            if (Instance == null)
                return;
            Instance.PlayMusicInternal(biome, isBoss);
        }

        private void PlayMusicInternal(RegionBiomeStyle biome, bool isBoss)
        {
            string key = isBoss ? "Boss" : biome.ToString();
            if (key == _currentMusicKey && _musicSource.isPlaying)
                return;

            if (!_musicCache.TryGetValue(key, out AudioClip clip) || clip == null)
            {
                clip = GenerateMusic(biome, isBoss);
                _musicCache[key] = clip;
            }

            _currentMusicKey = key;
            _musicSource.clip = clip;
            _musicSource.volume = 0f;
            _musicSource.Play();
        }

        // =========================================================================================
        // SFX SYNTHESIS
        // =========================================================================================
        private static AudioClip GenerateSfx(PMDSfx sfx)
        {
            switch (sfx)
            {
                case PMDSfx.Hit: return Tone("Hit", 0.10f, 320f, 120f, Wave.Square, 0.35f, 0.5f);
                case PMDSfx.SuperEffective: return Seq("Super", new[] { 520f, 780f, 1040f }, 0.06f, Wave.Square, 0.4f);
                case PMDSfx.NotVeryEffective: return Tone("Weak", 0.14f, 220f, 140f, Wave.Triangle, 0.1f, 0.5f);
                case PMDSfx.Melee: return Tone("Melee", 0.09f, 900f, 300f, Wave.Noise, 0.9f, 0.35f);
                case PMDSfx.Projectile: return Tone("Proj", 0.18f, 600f, 1200f, Wave.Square, 0.05f, 0.3f);
                case PMDSfx.Dash: return Tone("Dash", 0.22f, 200f, 900f, Wave.Noise, 0.6f, 0.35f);
                case PMDSfx.Ultimate: return Tone("Ult", 0.55f, 120f, 50f, Wave.Noise, 0.85f, 0.55f);
                case PMDSfx.Pickup: return Seq("Pickup", new[] { 880f, 1320f }, 0.06f, Wave.Square, 0.3f);
                case PMDSfx.Coin: return Seq("Coin", new[] { 988f, 1319f }, 0.07f, Wave.Square, 0.3f);
                case PMDSfx.LevelUp: return Seq("LevelUp", new[] { 523f, 659f, 784f, 1047f, 784f, 1047f }, 0.09f, Wave.Square, 0.35f);
                case PMDSfx.Recruit: return Seq("Recruit", new[] { 659f, 784f, 988f, 1319f }, 0.08f, Wave.Triangle, 0.5f);
                case PMDSfx.Error: return Seq("Error", new[] { 220f, 180f }, 0.09f, Wave.Square, 0.3f);
                case PMDSfx.Gate: return Tone("Gate", 0.6f, 300f, 1400f, Wave.Triangle, 0.1f, 0.4f);
                case PMDSfx.Faint: return Tone("Faint", 0.45f, 600f, 90f, Wave.Square, 0.1f, 0.35f);
                case PMDSfx.PlayerHurt: return Tone("Hurt", 0.16f, 260f, 90f, Wave.Square, 0.5f, 0.45f);
                case PMDSfx.MenuClick: return Tone("Click", 0.04f, 1200f, 1200f, Wave.Square, 0f, 0.2f);
                case PMDSfx.MenuOpen: return Seq("Open", new[] { 660f, 990f }, 0.05f, Wave.Triangle, 0.35f);
                case PMDSfx.QuestComplete: return Seq("Quest", new[] { 784f, 988f, 1175f, 1568f }, 0.11f, Wave.Square, 0.35f);
                case PMDSfx.Heal: return Seq("Heal", new[] { 523f, 659f, 784f, 1047f }, 0.07f, Wave.Triangle, 0.45f);
                case PMDSfx.GameOver: return Seq("GameOver", new[] { 392f, 370f, 349f, 262f }, 0.28f, Wave.Triangle, 0.5f);
                case PMDSfx.Evolve: return Seq("Evolve", new[] { 523f, 587f, 659f, 784f, 880f, 1047f, 1319f, 1568f }, 0.1f, Wave.Triangle, 0.5f);
                case PMDSfx.Footstep: return Tone("Step", 0.035f, 160f, 90f, Wave.Noise, 1f, 0.12f);
                case PMDSfx.BossRoar: return Tone("Roar", 0.9f, 140f, 60f, Wave.Square, 0.6f, 0.45f);
                case PMDSfx.ChestOpen: return Seq("Chest", new[] { 392f, 523f, 659f, 784f, 1047f }, 0.07f, Wave.Square, 0.35f);
                case PMDSfx.BallThrow: return Tone("BallThrow", 0.16f, 420f, 1180f, Wave.Triangle, 0.08f, 0.36f);
                case PMDSfx.BallShake: return Seq("BallShake", new[] { 340f, 480f }, 0.055f, Wave.Triangle, 0.34f);
                case PMDSfx.BallFail: return Seq("BallFail", new[] { 580f, 320f, 190f }, 0.07f, Wave.Square, 0.32f);
                default: return Tone("Default", 0.08f, 440f, 440f, Wave.Square, 0f, 0.3f);
            }
        }

        private enum Wave
        {
            Square,
            Triangle,
            Sine,
            Noise
        }

        private static float WaveSample(Wave w, float phase, System.Random noise)
        {
            float p = phase - Mathf.Floor(phase);
            switch (w)
            {
                case Wave.Square: return p < 0.5f ? 1f : -1f;
                case Wave.Triangle: return 1f - 4f * Mathf.Abs(p - 0.5f);
                case Wave.Sine: return Mathf.Sin(p * Mathf.PI * 2f);
                default: return (float)(noise.NextDouble() * 2.0 - 1.0);
            }
        }

        private static AudioClip Tone(string name, float duration, float f0, float f1, Wave wave, float noiseMix, float volume)
        {
            int n = Mathf.CeilToInt(duration * SampleRate);
            float[] data = new float[n];
            var rng = new System.Random(name.GetHashCode());
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float f = Mathf.Lerp(f0, f1, t);
                phase += f / SampleRate;
                float s = WaveSample(wave, phase, rng);
                if (noiseMix > 0f && wave != Wave.Noise)
                    s = Mathf.Lerp(s, (float)(rng.NextDouble() * 2.0 - 1.0), noiseMix);
                float env = Mathf.Min(1f, i / (SampleRate * 0.004f)) * (1f - t) * (1f - t);
                data[i] = s * env * volume;
            }
            var clip = AudioClip.Create("SFX_" + name, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Seq(string name, float[] notes, float noteDur, Wave wave, float volume)
        {
            int perNote = Mathf.CeilToInt(noteDur * SampleRate);
            int n = perNote * notes.Length + SampleRate / 10;
            float[] data = new float[n];
            var rng = new System.Random(name.GetHashCode());
            for (int k = 0; k < notes.Length; k++)
            {
                float phase = 0f;
                int start = k * perNote;
                int len = perNote + SampleRate / 10;
                for (int i = 0; i < len && start + i < n; i++)
                {
                    phase += notes[k] / SampleRate;
                    float t = i / (float)len;
                    float env = Mathf.Min(1f, i / (SampleRate * 0.003f)) * Mathf.Pow(1f - t, 2.2f);
                    data[start + i] += WaveSample(wave, phase, rng) * env * volume * 0.8f;
                }
            }
            var clip = AudioClip.Create("SFX_" + name, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // =========================================================================================
        // MUSIC SYNTHESIS (8-bit chiptune: melody + bass + arpeggio + hi-hat, 8 bars looping)
        // =========================================================================================
        private static readonly int[] MajorScale = { 0, 2, 4, 5, 7, 9, 11 };
        private static readonly int[] MinorScale = { 0, 2, 3, 5, 7, 8, 10 };

        private static AudioClip GenerateMusic(RegionBiomeStyle biome, bool isBoss)
        {
            float bpm;
            int root;           // MIDI note
            bool minor;
            Wave lead;
            int seed;

            if (isBoss)
            {
                bpm = 150f; root = 45; minor = true; lead = Wave.Square; seed = 666;
            }
            else
            {
                switch (biome)
                {
                    case RegionBiomeStyle.DeepWoodland: bpm = 96f; root = 50; minor = true; lead = Wave.Triangle; seed = 21; break;
                    case RegionBiomeStyle.RedRockCanyon: bpm = 128f; root = 52; minor = true; lead = Wave.Square; seed = 33; break;
                    case RegionBiomeStyle.TropicalArchipelago: bpm = 118f; root = 55; minor = false; lead = Wave.Triangle; seed = 44; break;
                    case RegionBiomeStyle.SakuraSanctuary: bpm = 88f; root = 57; minor = false; lead = Wave.Sine; seed = 55; break;
                    case RegionBiomeStyle.GlacialHighlands: bpm = 92f; root = 53; minor = true; lead = Wave.Sine; seed = 66; break;
                    case RegionBiomeStyle.AutumnGuildHighlands: bpm = 112f; root = 50; minor = false; lead = Wave.Square; seed = 77; break;
                    case RegionBiomeStyle.StarlightApex: bpm = 104f; root = 48; minor = true; lead = Wave.Triangle; seed = 88; break;
                    case RegionBiomeStyle.IndoorRoom: bpm = 84f; root = 55; minor = false; lead = Wave.Triangle; seed = 99; break;
                    default: bpm = 108f; root = 53; minor = false; lead = Wave.Square; seed = 11; break;
                }
            }

            int[] scale = minor ? MinorScale : MajorScale;
            int[] progression = minor ? new[] { 0, 5, 3, 4 } : new[] { 0, 5, 3, 4 }; // I-vi-IV-V / i-VI-iv-v (scale degrees)
            const int bars = 8;
            const int stepsPerBar = 8; // eighth notes
            float stepDur = 60f / bpm / 2f;
            int stepSamples = Mathf.RoundToInt(stepDur * SampleRate);
            int total = stepSamples * bars * stepsPerBar;
            float[] data = new float[total];
            var rng = new System.Random(seed);
            var noise = new System.Random(seed + 1);

            int melodyDegree = 7; // start an octave above root
            for (int bar = 0; bar < bars; bar++)
            {
                int chordDeg = progression[bar % progression.Length];
                for (int step = 0; step < stepsPerBar; step++)
                {
                    int startSample = (bar * stepsPerBar + step) * stepSamples;

                    // Melody: random walk biased toward chord tones; some rests
                    bool rest = rng.NextDouble() < (isBoss ? 0.08 : 0.2);
                    if (step % 2 == 0 || rng.NextDouble() < 0.5)
                    {
                        int move = rng.Next(-2, 3);
                        melodyDegree = Mathf.Clamp(melodyDegree + move, 4, 13);
                        if (step == 0)
                            melodyDegree = chordDeg + 7 + (rng.NextDouble() < 0.5 ? 0 : 2);
                    }
                    if (!rest)
                    {
                        float freq = MidiToFreq(root + 12 + DegreeToSemitone(scale, melodyDegree));
                        AddNote(data, startSample, stepSamples, freq, lead, 0.16f, noise, 0.85f);
                    }

                    // Bass: root on beats, fifth on off-beats
                    int bassDeg = (step % 4 == 2) ? chordDeg + 4 : chordDeg;
                    float bassFreq = MidiToFreq(root - 12 + DegreeToSemitone(scale, bassDeg));
                    AddNote(data, startSample, stepSamples, bassFreq, Wave.Triangle, 0.22f, noise, 0.9f);

                    // Arpeggio (soft)
                    int arpDeg = chordDeg + (step % 3) * 2;
                    float arpFreq = MidiToFreq(root + DegreeToSemitone(scale, arpDeg));
                    AddNote(data, startSample, stepSamples / 2, arpFreq, Wave.Square, 0.045f, noise, 0.6f);

                    // Hi-hat / kick
                    if (step % 2 == 1 || isBoss)
                        AddNote(data, startSample, stepSamples / 4, 0f, Wave.Noise, isBoss ? 0.05f : 0.035f, noise, 0.3f);
                    if (step % 4 == 0)
                        AddKick(data, startSample, stepSamples, isBoss ? 0.32f : 0.22f);
                }
            }

            var clip = AudioClip.Create("Music_" + (isBoss ? "Boss" : biome.ToString()), total, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static int DegreeToSemitone(int[] scale, int degree)
        {
            int octave = Mathf.FloorToInt(degree / 7f);
            int idx = ((degree % 7) + 7) % 7;
            return octave * 12 + scale[idx];
        }

        private static float MidiToFreq(int midi)
        {
            return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
        }

        private static void AddNote(float[] data, int start, int length, float freq, Wave wave, float vol, System.Random noise, float sustain)
        {
            float phase = 0f;
            int len = Mathf.Min(length, data.Length - start);
            for (int i = 0; i < len; i++)
            {
                phase += freq / SampleRate;
                float t = i / (float)length;
                float env = Mathf.Min(1f, i / (SampleRate * 0.004f)) * (t < sustain ? 1f - t * 0.4f : Mathf.Max(0f, (1f - t) / (1f - sustain)) * 0.6f);
                data[start + i] += WaveSample(wave, phase, noise) * env * vol;
            }
        }

        private static void AddKick(float[] data, int start, int length, float vol)
        {
            float phase = 0f;
            int len = Mathf.Min(length, data.Length - start);
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)length;
                float f = Mathf.Lerp(140f, 40f, Mathf.Min(1f, t * 3f));
                phase += f / SampleRate;
                data[start + i] += Mathf.Sin(phase * Mathf.PI * 2f) * Mathf.Pow(1f - t, 4f) * vol;
            }
        }
    }
}

