using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    public bool useRandomPitch;
    public Vector2 pitchRange = new Vector2(0.9f, 1.1f);
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public Sound[] musicSounds;
    public Sound[] sfxSounds;
    public AudioClip[] uiButtonSounds;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Mixer")]
    public AudioMixer audioMixer;

    [Header("Startup")]
    public string musicOnStart = "Theme";

    string currentMusicName;
    float currentMusicTime;

    const string SFX_VOLUME_KEY = "SFXVolume";
    const string MUSIC_VOLUME_KEY = "MusicVolume";

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupSources();
        LoadVolumes();
    }

    void Start()
    {
        if (!string.IsNullOrEmpty(musicOnStart))
        {
            PlayMusic(musicOnStart);
        }
    }

    void Update()
    {
        if (musicSource.isPlaying)
        {
            currentMusicTime = musicSource.time;
        }
    }

    void SetupSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void LoadVolumes()
    {
        if (audioMixer == null)
        {
            return;
        }

        float sfx = Mathf.Clamp(PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 0.5f), 0.0001f, 1f);
        float music = Mathf.Clamp(PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.5f), 0.0001f, 1f);

        audioMixer.SetFloat("SFXVolume", Mathf.Log10(sfx) * 30f);
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(music) * 30f);
    }

    public void PlayMusic(string name)
    {
        if (currentMusicName == name && musicSource.isPlaying)
        {
            return;
        }

        Sound s = Array.Find(musicSounds, x => x.name == name);
        if (s == null || s.clip == null)
        {
            return;
        }

        currentMusicName = name;
        musicSource.clip = s.clip;
        musicSource.time = currentMusicTime;
        musicSource.pitch = 1f;
        musicSource.Play();
    }

    public void ChangeMusic(string newMusic, float fadeOutTime, float fadeInTime)
    {
        StartCoroutine(FadeMusic(newMusic, fadeOutTime, fadeInTime));
    }

    IEnumerator FadeMusic(string newMusic, float fadeOut, float fadeIn)
    {
        if (musicSource.isPlaying)
        {
            float startVolume = musicSource.volume;

            while (musicSource.volume > 0f)
            {
                musicSource.volume -= startVolume * Time.deltaTime / fadeOut;
                yield return null;
            }

            musicSource.Stop();
            musicSource.volume = startVolume;
        }

        Sound s = Array.Find(musicSounds, x => x.name == newMusic);
        if (s == null || s.clip == null)
        {
            yield break;
        }

        musicSource.clip = s.clip;
        musicSource.volume = 0f;
        musicSource.Play();

        while (musicSource.volume < 1f)
        {
            musicSource.volume += Time.deltaTime / fadeIn;
            yield return null;
        }

        musicSource.volume = 1f;
        currentMusicName = newMusic;
    }

    public void PlaySFX(string name)
    {
        Sound s = Array.Find(sfxSounds, x => x.name == name);
        if (s == null || s.clip == null)
        {
            return;
        }

        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = s.clip;
        source.volume = sfxSource.volume;
        source.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
        source.pitch = s.useRandomPitch
            ? UnityEngine.Random.Range(s.pitchRange.x, s.pitchRange.y)
            : 1f;

        source.Play();
        Destroy(source, s.clip.length / source.pitch);
    }

    private Dictionary<string, float> sfxCooldowns = new Dictionary<string, float>();

    public bool IsSFXOnCooldown(string name, float cooldown)
    {
        if (sfxCooldowns.TryGetValue(name, out float lastTime))
            return Time.time < lastTime + cooldown;
        return false;
    }

    public void RegisterSFXPlayed(string name)
    {
        sfxCooldowns[name] = Time.time;
    }

    public void PlaySFX3D(string name, GameObject target, float minRange, float maxRange)
    {
        Sound s = Array.Find(sfxSounds, x => x.name == name);
        if (s == null || s.clip == null)
        {
            return;
        }

        Vector3 position = target != null ? target.transform.position : Vector3.zero;

        GameObject tempGO = new GameObject("3D_SFX");
        tempGO.transform.position = position;

        AudioSource source = tempGO.AddComponent<AudioSource>();

        source.clip = s.clip;
        source.volume = sfxSource.volume;
        source.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
        source.spatialBlend = 1f;
        source.minDistance = minRange;
        source.maxDistance = maxRange;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.pitch = s.useRandomPitch
            ? UnityEngine.Random.Range(s.pitchRange.x, s.pitchRange.y)
            : 1f;

        source.Play();

        Destroy(tempGO, s.clip.length / source.pitch);
    }

    public void PlayUIButton()
    {
        if (uiButtonSounds == null || uiButtonSounds.Length == 0)
        {
            return;
        }

        AudioClip clip = uiButtonSounds[UnityEngine.Random.Range(0, uiButtonSounds.Length)];

        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = sfxSource.volume;
        source.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
        source.pitch = UnityEngine.Random.Range(0.9f, 1.1f);

        source.Play();
        Destroy(source, clip.length / source.pitch);
    }
}