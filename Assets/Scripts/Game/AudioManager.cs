using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[System.Serializable]
public class AudioData
{
    public string name;
    public AudioClip clip;
}

/// <summary>
/// Robust AudioManager
/// - Singleton (safe re-init across scenes)
/// - BGM: play/stop with fade, crossfade between tracks
/// - SFX: one-shot (2D), 3D at position (pooled), optional pitch variance
/// - Uses dictionaries safely (no duplicate-key crashes)
/// - Mixer-backed volume controls (linear 0..1 -> dB), persist via PlayerPrefs
/// </summary>
[DefaultExecutionOrder(-50)]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("Primary BGM audio source")]
    public AudioSource bgmA;
    [Tooltip("Secondary BGM source for crossfades")]
    public AudioSource bgmB;
    [Tooltip("SFX AudioSource for 2D one-shots")]
    public AudioSource sfx2D;

    [Header("Audio Lists")]
    public List<AudioData> backgroundMusicList = new List<AudioData>();
    public List<AudioData> sfxList = new List<AudioData>();

    [Header("Audio Mixer (optional)")]
    public AudioMixer mixer;                 // assign a mixer (recommended)
    [Tooltip("Exposed parameter name for master volume (dB)")]
    public string mixerParamMaster = "MasterVol";
    [Tooltip("Exposed parameter name for BGM volume (dB)")]
    public string mixerParamBGM = "BGMVol";
    [Tooltip("Exposed parameter name for SFX volume (dB)")]
    public string mixerParamSFX = "SFXVol";

    [Header("SFX 3D Pool")]
    [Tooltip("Prefab with an AudioSource set to 3D for positional SFX")]
    public AudioSource sfx3DPrefab;
    [Range(1, 32)] public int sfx3DPoolSize = 8;

    [Header("Defaults")]
    [Range(0f, 1f)] public float defaultBGMFade = 0.5f;
    [Range(0f, 0.5f)] public float sfxPitchVariance = 0.0f; // e.g., 0.05 for slight variety

    // Internal state
    private readonly Dictionary<string, AudioClip> bgmDict = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, AudioClip> sfxDict = new Dictionary<string, AudioClip>();
    private readonly Queue<AudioSource> sfx3DPool = new Queue<AudioSource>();

    private bool usingA = true; // which BGM source is the "current" one

    // PlayerPrefs keys
    const string PP_MASTER = "Audio_Master";
    const string PP_BGM = "Audio_BGM";
    const string PP_SFX = "Audio_SFX";

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildDictionaries();
            BuildSFX3DPool();
            LoadSavedVolumes(); // optional
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Guard against missing sources
        if (!bgmA) bgmA = gameObject.AddComponent<AudioSource>();
        if (!bgmB) bgmB = gameObject.AddComponent<AudioSource>();
        if (!sfx2D) sfx2D = gameObject.AddComponent<AudioSource>();

        // Make sure BGM sources loop by default
        bgmA.loop = true;
        bgmB.loop = true;
    }

    #region Setup
    private void BuildDictionaries()
    {
        bgmDict.Clear();
        sfxDict.Clear();

        foreach (var d in backgroundMusicList)
        {
            if (d.clip == null || string.IsNullOrEmpty(d.name)) continue;
            if (!bgmDict.ContainsKey(d.name))
                bgmDict.Add(d.name, d.clip);
            else
                Debug.LogWarning($"[AudioManager] Duplicate BGM key '{d.name}' skipped.");
        }

        foreach (var d in sfxList)
        {
            if (d.clip == null || string.IsNullOrEmpty(d.name)) continue;
            if (!sfxDict.ContainsKey(d.name))
                sfxDict.Add(d.name, d.clip);
            else
                Debug.LogWarning($"[AudioManager] Duplicate SFX key '{d.name}' skipped.");
        }
    }

    private void BuildSFX3DPool()
    {
        if (!sfx3DPrefab) return;
        for (int i = 0; i < sfx3DPoolSize; i++)
        {
            var src = Instantiate(sfx3DPrefab, transform);
            src.playOnAwake = false;
            src.loop = false;
            src.gameObject.SetActive(false);
            sfx3DPool.Enqueue(src);
        }
    }
    #endregion

    #region BGM
    public void PlayBackgroundMusic(string bgmName, float fadeSeconds = -1f)
    {
        if (!TryGetBGM(bgmName, out var clip))
        {
            Debug.LogWarning($"[AudioManager] BGM '{bgmName}' not found.");
            return;
        }

        if (fadeSeconds < 0f) fadeSeconds = defaultBGMFade;

        // Instant swap if same clip already playing
        var current = CurrentBGMSrc();
        if (current.clip == clip && current.isPlaying)
            return;

        StopAllCoroutines();
        StartCoroutine(CrossfadeTo(clip, fadeSeconds));
    }

    public void StopBackgroundMusic(float fadeSeconds = -1f)
    {
        if (fadeSeconds < 0f) 
            fadeSeconds = defaultBGMFade;
        StopAllCoroutines();
        StartCoroutine(FadeOutBoth(fadeSeconds));
    }

    public bool IsBackgroundMusicPlaying(string bgmName)
    {
        var a = bgmA && bgmA.isPlaying && bgmA.clip && bgmA.clip.name == bgmName;
        var b = bgmB && bgmB.isPlaying && bgmB.clip && bgmB.clip.name == bgmName;
        return a || b;
    }

    private IEnumerator CrossfadeTo(AudioClip nextClip, float duration)
    {
        var from = CurrentBGMSrc();
        var to = OtherBGMSrc();

        to.clip = nextClip;
        to.time = 0f;
        to.volume = 0f;
        to.Play();

        float t = 0f;
        float fromStart = from ? from.volume : 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = (duration <= 0f) ? 1f : Mathf.Clamp01(t / duration);
            if (from) from.volume = Mathf.Lerp(fromStart, 0f, p);
            to.volume = Mathf.Lerp(0f, 1f, p);
            yield return null;
        }

        if (from)
        {
            from.Stop();
            from.volume = 1f; // reset
        }

        to.volume = 1f;
        usingA = (to == bgmA);
    }

    private IEnumerator FadeOutBoth(float duration)
    {
        float a0 = (bgmA && bgmA.isPlaying) ? bgmA.volume : 0f;
        float b0 = (bgmB && bgmB.isPlaying) ? bgmB.volume : 0f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = (duration <= 0f) ? 1f : Mathf.Clamp01(t / duration);

            if (bgmA) bgmA.volume = Mathf.Lerp(a0, 0f, p);
            if (bgmB) bgmB.volume = Mathf.Lerp(b0, 0f, p);
            yield return null;
        }

        if (bgmA) { bgmA.Stop(); bgmA.volume = 1f; }
        if (bgmB) { bgmB.Stop(); bgmB.volume = 1f; }
    }

    private AudioSource CurrentBGMSrc() => usingA ? bgmA : bgmB;
    private AudioSource OtherBGMSrc() => usingA ? bgmB : bgmA;

    private bool TryGetBGM(string name, out AudioClip clip) => bgmDict.TryGetValue(name, out clip);
    #endregion

    #region SFX
    public void PlaySFX(string sfxName, float volume = 1f)
    {
        if (!sfxDict.TryGetValue(sfxName, out var clip))
        {
            Debug.LogWarning($"[AudioManager] SFX '{sfxName}' not found.");
            return;
        }

        if (!sfx2D)
        {
            Debug.LogWarning("[AudioManager] Missing 2D SFX AudioSource.");
            return;
        }

        float pitch = 1f;
        if (sfxPitchVariance > 0f)
        {
            pitch = 1f + Random.Range(-sfxPitchVariance, sfxPitchVariance);
        }

        sfx2D.pitch = pitch;
        sfx2D.PlayOneShot(clip, Mathf.Clamp01(volume));
        sfx2D.pitch = 1f; // reset
    }

    public void PlaySFXAt(string sfxName, Vector3 worldPos, float volume = 1f)
    {
        if (!sfxDict.TryGetValue(sfxName, out var clip))
        {
            Debug.LogWarning($"[AudioManager] SFX '{sfxName}' not found.");
            return;
        }
        if (!sfx3DPrefab)
        {
            // Fallback: Unity helper (no pooling)
            AudioSource.PlayClipAtPoint(clip, worldPos, Mathf.Clamp01(volume));
            return;
        }

        var src = GetFree3DSrc();
        src.transform.position = worldPos;
        src.gameObject.SetActive(true);

        float pitch = 1f;
        if (sfxPitchVariance > 0f)
            pitch = 1f + Random.Range(-sfxPitchVariance, sfxPitchVariance);

        src.pitch = pitch;
        src.clip = clip;
        src.volume = Mathf.Clamp01(volume);
        src.Play();
        StartCoroutine(ReturnToPoolWhenDone(src));
    }

    private AudioSource GetFree3DSrc()
    {
        // Try to reuse pooled
        foreach (var s in sfx3DPool)
        {
            if (!s.isPlaying)
                return s;
        }
        // None free: expand pool by one
        var extra = Instantiate(sfx3DPrefab, transform);
        extra.playOnAwake = false;
        extra.loop = false;
        extra.gameObject.SetActive(false);
        sfx3DPool.Enqueue(extra);
        return extra;
    }

    private IEnumerator ReturnToPoolWhenDone(AudioSource src)
    {
        // wait until the clip finishes
        while (src.isPlaying) yield return null;
        src.clip = null;
        src.gameObject.SetActive(false);
    }
    #endregion

    #region Volumes (Mixer-backed, persisted)
    // linear [0..1] -> dB conversion
    private static float LinearToDb(float v) => (v <= 0.0001f) ? -80f : Mathf.Log10(v) * 20f;

    public void SetMasterVolume(float linear01)
    {
        if (mixer && !string.IsNullOrEmpty(mixerParamMaster))
            mixer.SetFloat(mixerParamMaster, LinearToDb(Mathf.Clamp01(linear01)));
        PlayerPrefs.SetFloat(PP_MASTER, Mathf.Clamp01(linear01));
    }
    public void SetBGMVolume(float linear01)
    {
        if (mixer && !string.IsNullOrEmpty(mixerParamBGM))
            mixer.SetFloat(mixerParamBGM, LinearToDb(Mathf.Clamp01(linear01)));
        PlayerPrefs.SetFloat(PP_BGM, Mathf.Clamp01(linear01));
    }
    public void SetSFXVolume(float linear01)
    {
        if (mixer && !string.IsNullOrEmpty(mixerParamSFX))
            mixer.SetFloat(mixerParamSFX, LinearToDb(Mathf.Clamp01(linear01)));
        PlayerPrefs.SetFloat(PP_SFX, Mathf.Clamp01(linear01));
    }

    private void LoadSavedVolumes()
    {
        if (!mixer) return;

        float master = PlayerPrefs.GetFloat(PP_MASTER, 1f);
        float bgm = PlayerPrefs.GetFloat(PP_BGM, 1f);
        float sfx = PlayerPrefs.GetFloat(PP_SFX, 1f);

        mixer.SetFloat(mixerParamMaster, LinearToDb(master));
        mixer.SetFloat(mixerParamBGM, LinearToDb(bgm));
        mixer.SetFloat(mixerParamSFX, LinearToDb(sfx));
    }
    #endregion
}
