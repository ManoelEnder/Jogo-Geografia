using UnityEngine;

namespace NucleoVivo
{
    public enum Sfx { Tap, Buy, Install, Event, Error, Rebirth, Ui }

    /// <summary>Sons curtos sintetizados por código (sem arquivos de áudio).</summary>
    [RequireComponent(typeof(AudioSource))]
    public class SfxPlayer : MonoBehaviour
    {
        [Range(0, 1)] public float volume = 0.6f;

        AudioSource source;
        AudioClip[] taps;
        AudioClip buy, install, evt, error, rebirth, ui;
        float lastTapTime;
        const int Rate = 44100;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            taps = new[]
            {
                Tone("tap0", 0.09f, 600f, 840f, 0.35f, 2),
                Tone("tap1", 0.09f, 640f, 900f, 0.35f, 2),
                Tone("tap2", 0.09f, 680f, 950f, 0.35f, 2),
            };
            buy = Chord("buy", 0.22f, new[] { 523.25f, 783.99f }, 0.08f, 0.35f);
            install = Chord("install", 0.5f, new[] { 392f, 523.25f, 659.25f, 783.99f }, 0.07f, 0.35f);
            evt = Chord("event", 0.6f, new[] { 880f, 1318.5f }, 0.12f, 0.25f);
            error = Tone("error", 0.14f, 220f, 180f, 0.3f, 1);
            rebirth = Sweep("rebirth", 1.6f);
            ui = Tone("ui", 0.05f, 900f, 950f, 0.2f, 1);
        }

        public void Play(Sfx s)
        {
            var gm = GameManager.Instance;
            if (gm != null && !gm.State.soundOn) return;
            switch (s)
            {
                case Sfx.Tap:
                    // Em toques muito rápidos, reduz o volume para não saturar.
                    float since = Time.unscaledTime - lastTapTime;
                    lastTapTime = Time.unscaledTime;
                    source.PlayOneShot(taps[Random.Range(0, taps.Length)], volume * (since < 0.08f ? 0.45f : 0.8f));
                    break;
                case Sfx.Buy: source.PlayOneShot(buy, volume); break;
                case Sfx.Install: source.PlayOneShot(install, volume); break;
                case Sfx.Event: source.PlayOneShot(evt, volume * 0.8f); break;
                case Sfx.Error: source.PlayOneShot(error, volume * 0.6f); break;
                case Sfx.Rebirth: source.PlayOneShot(rebirth, volume); break;
                case Sfx.Ui: source.PlayOneShot(ui, volume * 0.5f); break;
            }
        }

        static AudioClip Tone(string name, float dur, float f0, float f1, float amp, int harmonics)
        {
            int n = (int)(Rate * dur);
            var data = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float f = Mathf.Lerp(f0, f1, t);
                phase += 2 * Mathf.PI * f / Rate;
                float env = Mathf.Min(1f, i / (Rate * 0.004f)) * Mathf.Exp(-t * 5f);
                float s = Mathf.Sin((float)phase);
                if (harmonics > 1) s += 0.25f * Mathf.Sin((float)phase * 2f);
                data[i] = s * env * amp;
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Chord(string name, float dur, float[] freqs, float stagger, float amp)
        {
            int n = (int)(Rate * (dur + stagger * freqs.Length));
            var data = new float[n];
            for (int k = 0; k < freqs.Length; k++)
            {
                int start = (int)(Rate * stagger * k);
                int len = (int)(Rate * dur);
                for (int i = 0; i < len && start + i < n; i++)
                {
                    float t = (float)i / len;
                    float env = Mathf.Min(1f, i / (Rate * 0.005f)) * Mathf.Exp(-t * 4f);
                    float ph = 2 * Mathf.PI * freqs[k] * i / Rate;
                    data[start + i] += (Mathf.Sin(ph) + 0.2f * Mathf.Sin(ph * 2f)) * env * amp / Mathf.Sqrt(freqs.Length);
                }
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Sweep(string name, float dur)
        {
            int n = (int)(Rate * dur);
            var data = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                // desce (cidade apaga) e sobe (reator reacende)
                float f = t < 0.45f ? Mathf.Lerp(440f, 110f, t / 0.45f) : Mathf.Lerp(110f, 660f, (t - 0.45f) / 0.55f);
                phase += 2 * Mathf.PI * f / Rate;
                float env = Mathf.Sin(t * Mathf.PI) * 0.8f;
                data[i] = (Mathf.Sin((float)phase) + 0.3f * Mathf.Sin((float)phase * 1.5f)) * env * 0.3f;
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
