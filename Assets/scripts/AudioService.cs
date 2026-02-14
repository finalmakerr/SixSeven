using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class AudioService : MonoBehaviour
{
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;

    public static AudioService Instance { get; private set; }

    private readonly HashSet<string> missingKeysLogged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private SceneAssetLoader sceneAssetLoader;
    private AudioAssetCatalog audioCatalog;
    private bool catalogWarningLogged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        EnsureAudioSources();
        sceneAssetLoader = FindObjectOfType<SceneAssetLoader>();

        if (sceneAssetLoader == null)
        {
            LogCatalogMissingWarning();
            return;
        }

        CacheCatalog();
        if (audioCatalog == null)
        {
            sceneAssetLoader.OnAssetsLoaded.AddListener(CacheCatalog);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (sceneAssetLoader != null)
        {
            sceneAssetLoader.OnAssetsLoaded.RemoveListener(CacheCatalog);
        }
    }

    public void PlaySfx(string key, float volume = 1f)
    {
        if (sfxSource == null)
        {
            return;
        }

        var clip = GetClip(key);
        if (clip == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    public void PlayMusic(string key, bool loop = true)
    {
        if (musicSource == null)
        {
            return;
        }

        var clip = GetClip(key);
        if (clip == null)
        {
            return;
        }

        if (musicSource.clip != clip)
        {
            musicSource.clip = clip;
        }

        musicSource.loop = loop;
        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        if (musicSource == null)
        {
            return;
        }

        musicSource.Stop();
    }

    public AudioClip GetClip(string key)
    {
        if (audioCatalog == null)
        {
            CacheCatalog();
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

        var normalizedKey = key.Trim();
        var clip = audioCatalog.Get(normalizedKey);
        if (clip != null)
        {
            return clip;
        }

        if (missingKeysLogged.Add(normalizedKey))
        {
            Debug.LogWarning($"AudioService: Missing audio key '{normalizedKey}'.", this);
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

    private void CacheCatalog()
    {
        if (sceneAssetLoader == null)
        {
            return;
        }

        audioCatalog = sceneAssetLoader.GetLoadedAsset<AudioAssetCatalog>();
    }

    private void LogCatalogMissingWarning()
    {
        if (catalogWarningLogged)
        {
            return;
        }

        catalogWarningLogged = true;
        Debug.LogWarning("AudioService: AudioAssetCatalog unavailable. Audio playback will use caller fallbacks.", this);
    }
}
