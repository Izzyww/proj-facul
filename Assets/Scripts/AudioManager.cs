using System.Collections.Generic;
using UnityEngine;

// Toca a trilha (BGM) e os efeitos sonoros. O pacote de assets so traz musicas
// (.ogg), entao os SFX (pulo, moeda, dano, etc.) sao SINTETIZADOS por codigo
// em tempo de execucao - sem precisar de arquivos de som.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public enum Sfx
    {
        Jump,
        DoubleJump,
        WallJump,
        Coin,
        Hurt,
        Stomp,
        BossHit,
        Win,
        Click
    }

    private AudioSource m_BgmSource;
    private AudioSource m_SfxSource;
    private AudioClip m_CurrentBgm;
    private readonly Dictionary<Sfx, AudioClip> m_SfxClips = new Dictionary<Sfx, AudioClip>();

    private const int SampleRate = 44100;

    public static AudioManager GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject obj = new GameObject("AudioManager");
        return obj.AddComponent<AudioManager>();
    }

    // Atalho seguro: toca um SFX se o manager existir.
    public static void Play(Sfx type)
    {
        if (Instance != null)
        {
            Instance.PlaySfx(type);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        m_BgmSource = gameObject.AddComponent<AudioSource>();
        m_BgmSource.loop = true;
        m_BgmSource.playOnAwake = false;
        m_BgmSource.volume = 0.4f;

        m_SfxSource = gameObject.AddComponent<AudioSource>();
        m_SfxSource.playOnAwake = false;
        m_SfxSource.volume = 0.55f;

        BuildSfxClips();
    }

    // ── BGM ───────────────────────────────────────────────────────────
    public void PlayBgm(AudioClip clip)
    {
        if (clip == null || clip == m_CurrentBgm)
        {
            return;
        }

        m_CurrentBgm = clip;
        m_BgmSource.clip = clip;
        m_BgmSource.Play();
    }

    public void StopBgm()
    {
        m_BgmSource.Stop();
        m_CurrentBgm = null;
    }

    // ── SFX ───────────────────────────────────────────────────────────
    public void PlaySfx(Sfx type)
    {
        if (m_SfxClips.TryGetValue(type, out AudioClip clip) && clip != null)
        {
            m_SfxSource.PlayOneShot(clip);
        }
    }

    private void BuildSfxClips()
    {
        m_SfxClips[Sfx.Jump] = Sweep("sfx_jump", 420f, 880f, 0.16f, 0.5f, false);
        m_SfxClips[Sfx.DoubleJump] = Sweep("sfx_djump", 640f, 1180f, 0.16f, 0.5f, false);
        m_SfxClips[Sfx.WallJump] = Sweep("sfx_walljump", 300f, 620f, 0.18f, 0.5f, true);
        m_SfxClips[Sfx.Coin] = Beeps("sfx_coin", new[] { 988f, 1319f }, 0.07f, 0.45f, true);
        m_SfxClips[Sfx.Hurt] = Sweep("sfx_hurt", 440f, 120f, 0.28f, 0.5f, true);
        m_SfxClips[Sfx.Stomp] = Thud("sfx_stomp", 0.16f, 0.6f);
        m_SfxClips[Sfx.BossHit] = Beeps("sfx_bosshit", new[] { 220f, 165f }, 0.08f, 0.55f, true);
        m_SfxClips[Sfx.Win] = Beeps("sfx_win", new[] { 523f, 659f, 784f, 1047f }, 0.11f, 0.45f, false);
        m_SfxClips[Sfx.Click] = Beeps("sfx_click", new[] { 880f }, 0.05f, 0.4f, true);
    }

    // Onda com frequencia variando de f0 a f1 (glissando), com envelope decaindo.
    private AudioClip Sweep(string name, float f0, float f1, float duration, float volume, bool square)
    {
        int count = Mathf.CeilToInt(SampleRate * duration);
        float[] data = new float[count];
        double phase = 0d;

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            float frac = t / duration;
            float freq = Mathf.Lerp(f0, f1, frac);
            phase += 2d * Mathf.PI * freq / SampleRate;

            float s = square ? Mathf.Sign(Mathf.Sin((float)phase)) : Mathf.Sin((float)phase);
            data[i] = s * volume * Envelope(t, duration);
        }

        return ClipFrom(name, data);
    }

    // Sequencia de bipes (cada um com sua frequencia).
    private AudioClip Beeps(string name, float[] freqs, float each, float volume, bool square)
    {
        float duration = each * freqs.Length;
        int count = Mathf.CeilToInt(SampleRate * duration);
        float[] data = new float[count];
        double phase = 0d;

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            int idx = Mathf.Clamp((int)(t / each), 0, freqs.Length - 1);
            float localT = t - idx * each;
            phase += 2d * Mathf.PI * freqs[idx] / SampleRate;

            float s = square ? Mathf.Sign(Mathf.Sin((float)phase)) : Mathf.Sin((float)phase);
            data[i] = s * volume * Envelope(localT, each);
        }

        return ClipFrom(name, data);
    }

    // Baque grave + ruido curto (pisar no inimigo).
    private AudioClip Thud(string name, float duration, float volume)
    {
        int count = Mathf.CeilToInt(SampleRate * duration);
        float[] data = new float[count];
        double phase = 0d;

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            float freq = Mathf.Lerp(150f, 70f, t / duration);
            phase += 2d * Mathf.PI * freq / SampleRate;

            float tone = Mathf.Sin((float)phase);
            float noise = (Random.value * 2f - 1f) * 0.5f;
            data[i] = (tone + noise) * volume * Envelope(t, duration);
        }

        return ClipFrom(name, data);
    }

    private static float Envelope(float t, float duration)
    {
        const float attack = 0.005f;
        float a = t < attack ? t / attack : 1f;
        float decay = Mathf.Exp(-3.5f * (t / duration));
        return a * decay;
    }

    private static AudioClip ClipFrom(string name, float[] data)
    {
        AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
