using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SceneAssetLoader : MonoBehaviour
{
    [SerializeField] private SceneAssetGroup sceneAssetGroup;
    [SerializeField] private UnityEvent onAssetsLoaded;

    private readonly List<AsyncOperationHandle<object>> loadHandles = new();
    private CancellationTokenSource destroyCancellationTokenSource;

    public bool IsLoaded { get; private set; }
    public float LoadProgress { get; private set; }
    public UnityEvent OnAssetsLoaded => onAssetsLoaded;

    private async void Start()
    {
        destroyCancellationTokenSource = new CancellationTokenSource();
        await LoadAssetsAsync(destroyCancellationTokenSource.Token);
    }

    private void Update()
    {
        if (loadHandles.Count == 0)
            return;

        float cumulativeProgress = 0f;
        for (int i = 0; i < loadHandles.Count; i++)
        {
            cumulativeProgress += loadHandles[i].PercentComplete;
        }

        LoadProgress = Mathf.Clamp01(cumulativeProgress / loadHandles.Count);
    }

    private async Task LoadAssetsAsync(CancellationToken cancellationToken)
    {
        IsLoaded = false;
        LoadProgress = 0f;

        if (sceneAssetGroup == null || sceneAssetGroup.AssetReferences == null)
        {
            IsLoaded = true;
            LoadProgress = 1f;
            onAssetsLoaded?.Invoke();
            return;
        }

        var handlesToAwait = new List<AsyncOperationHandle<object>>();

        foreach (AssetReference assetReference in sceneAssetGroup.AssetReferences)
        {
            if (assetReference == null)
                continue;

            AsyncOperationHandle<object> handle = assetReference.LoadAssetAsync<object>();
            loadHandles.Add(handle);
            handlesToAwait.Add(handle);
        }

        if (handlesToAwait.Count == 0)
        {
            IsLoaded = true;
            LoadProgress = 1f;
            onAssetsLoaded?.Invoke();
            return;
        }

        try
        {
            while (!AllHandlesCompleted(handlesToAwait))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            cancellationToken.ThrowIfCancellationRequested();
            IsLoaded = true;
            LoadProgress = 1f;
            onAssetsLoaded?.Invoke();
        }
        catch (OperationCanceledException)
        {
            // Destroy-time cancellation is expected.
        }
    }

    private static bool AllHandlesCompleted(IReadOnlyList<AsyncOperationHandle<object>> handles)
    {
        for (int i = 0; i < handles.Count; i++)
        {
            if (!handles[i].IsDone)
                return false;
        }

        return true;
    }

    private void OnDestroy()
    {
        destroyCancellationTokenSource?.Cancel();
        destroyCancellationTokenSource?.Dispose();
        destroyCancellationTokenSource = null;

        for (int i = 0; i < loadHandles.Count; i++)
        {
            AsyncOperationHandle<object> handle = loadHandles[i];
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        loadHandles.Clear();
    }
}
