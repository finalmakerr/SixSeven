using UnityEngine;

public class CameraAutoFit : MonoBehaviour
{
    public Transform boardRoot;
    public float padding = 0.5f;

    void Start()
    {
        if (!boardRoot) return;

        Camera cam = GetComponent<Camera>();
        if (!cam || !cam.orthographic) return;

        Renderer[] renderers = boardRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);

        float screenRatio = (float)Screen.width / Screen.height;
        float targetRatio = bounds.size.x / bounds.size.y;

        if (screenRatio >= targetRatio)
            cam.orthographicSize = bounds.size.y / 2f + padding;
        else
            cam.orthographicSize = bounds.size.x / (2f * screenRatio) + padding;

        cam.transform.position = new Vector3(
            bounds.center.x,
            bounds.center.y,
            cam.transform.position.z
        );
    }
}