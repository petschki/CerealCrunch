using System.Collections.Generic;
using UnityEngine;

/// Central audio: background music loop plus one-shot SFX with per-call
/// pitch (used for rising cascade pops). Clips live in Resources/Audio.
/// Creates itself on first use and survives scene changes.
public class AudioManager : MonoBehaviour
{
    const float MusicVolume = 0.3f;
    const int SfxSourceCount = 4;

    static AudioManager instance;

    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    AudioSource musicSource;
    AudioSource[] sfxSources;
    int nextSfxSource;

    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("AudioManager");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<AudioManager>();
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        foreach (var clip in Resources.LoadAll<AudioClip>("Audio"))
            clips[clip.name] = clip;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = MusicVolume;
        musicSource.playOnAwake = false;
        if (clips.TryGetValue("music_loop", out var music))
        {
            musicSource.clip = music;
            musicSource.Play();
        }

        sfxSources = new AudioSource[SfxSourceCount];
        for (int i = 0; i < SfxSourceCount; i++)
        {
            sfxSources[i] = gameObject.AddComponent<AudioSource>();
            sfxSources[i].playOnAwake = false;
        }
    }

    /// Fire-and-forget SFX; rotating sources so overlapping sounds
    /// (and different pitches) don't cut each other off.
    public static void Play(string name, float pitch = 1f, float volume = 1f)
    {
        var self = Instance;
        if (!self.clips.TryGetValue(name, out var clip)) return;

        var source = self.sfxSources[self.nextSfxSource];
        self.nextSfxSource = (self.nextSfxSource + 1) % SfxSourceCount;
        source.pitch = pitch;
        source.PlayOneShot(clip, volume);
    }
}
