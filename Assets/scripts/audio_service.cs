using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class audio_service : MonoBehaviour
{
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private SceneAssetLoader sceneAssetLoader;
    [SerializeField] private AudioAssetCatalog audioCatalog;

    public static audio_service Instance { get; private set; }

    private readonly HashSet<string> missingKeysLogged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private bool catalogWarningLogged;
    private bool subscribedToAssetsLoaded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
        EnsureSceneAssetLoader();
        CacheCatalog();
        SubscribeIfWaitingOnAssets();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        UnsubscribeFromAssetsLoaded();
    }

    public void play_sfx(string key, float volume = 1f)
    {
        PlaySfx(key, volume);
    }

    public void play_music(string key, bool loop = true)
    {
        PlayMusic(key, loop);
    }

    public void stop_music()
    {
        StopMusic();
    }

    public AudioClip get_clip(string key)
    {
        var clip = ResolveClip(key);
        return clip;
    }

    private void PlaySfx(string key, float volume = 1f)
    {
        if (sfxSource == null)
        {
            return;
        }

        var clip = ResolveClip(key);
        if (clip == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private void PlayMusic(string key, bool loop = true)
    {
        if (musicSource == null)
        {
            return;
        }

        var clip = ResolveClip(key);
        if (clip == null)
        {
            return;
        }

        var clipChanged = musicSource.clip != clip;
        if (clipChanged)
        {
            musicSource.clip = clip;
        }

        musicSource.loop = loop;
        if (clipChanged || !musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    private void StopMusic()
    {
        if (musicSource == null)
        {
            return;
        }

        musicSource.Stop();
    }

    private AudioClip ResolveClip(string key)
    {
        if (audioCatalog == null)
        {
            EnsureSceneAssetLoader();
            CacheCatalog();
            SubscribeIfWaitingOnAssets();
        }

        if (audioCatalog == null)
        {
            LogCatalogMissingWarning();
            return null;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var normalizedKey = key.Trim().ToLowerInvariant();
        var clip = audioCatalog.Get(normalizedKey);
        if (clip != null)
        {
            return clip;
        }

        if (missingKeysLogged.Add(normalizedKey))
        {
            Debug.LogWarning($"audio_service: missing audio key '{normalizedKey}'.", this);
        }

        return null;
    }

    private void EnsureAudioSources()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
        }
    }

    private void HandleAssetsLoaded()
    {
        CacheCatalog();
        UnsubscribeFromAssetsLoaded();
    }

    private void EnsureSceneAssetLoader()
    {
        if (sceneAssetLoader != null)
        {
            return;
        }

        sceneAssetLoader = FindObjectOfType<SceneAssetLoader>();
    }

    private void CacheCatalog()
    {
        if (sceneAssetLoader == null)
        {
            return;
        }

        audioCatalog = sceneAssetLoader.GetLoadedAsset<AudioAssetCatalog>();
    }

    private void SubscribeIfWaitingOnAssets()
    {
        if (sceneAssetLoader == null || sceneAssetLoader.IsLoaded || subscribedToAssetsLoaded)
        {
            return;
        }

        sceneAssetLoader.OnAssetsLoaded.AddListener(HandleAssetsLoaded);
        subscribedToAssetsLoaded = true;
    }

    private void UnsubscribeFromAssetsLoaded()
    {
        if (sceneAssetLoader == null || !subscribedToAssetsLoaded)
        {
            return;
        }

        sceneAssetLoader.OnAssetsLoaded.RemoveListener(HandleAssetsLoaded);
        subscribedToAssetsLoaded = false;
    }

    private void LogCatalogMissingWarning()
    {
        if (catalogWarningLogged)
        {
            return;
        }

        catalogWarningLogged = true;
        Debug.LogWarning("audio_service: audioassetcatalog unavailable.", this);
    }
}
