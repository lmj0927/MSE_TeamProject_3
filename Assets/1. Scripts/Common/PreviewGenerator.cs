// Owned by JunYoung Park
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.IO;

public class PreviewGenerator
{
    private const int PreviewLayer = 31; 

    public static Sprite TakeLiveSnapshot(GameObject liveObject, int width = 256, int height = 256, bool saveToDisk = false)
    {
        if (liveObject == null) return null;

        Dictionary<GameObject, int> layerBackup = new Dictionary<GameObject, int>();
        SetLayerRecursivelyWithBackup(liveObject, PreviewLayer, layerBackup);

        Bounds bounds = CalculateBounds(liveObject);
        Vector3 center = liveObject.transform.position + Vector3.up * 1.0f;

        GameObject cameraObject = new GameObject("Snapshot Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;

        Color bgColor = new Color(0f, 0f, 0f, 0f);
        camera.backgroundColor = bgColor;

        camera.orthographic = true;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 50f; 
        camera.cullingMask = 1 << PreviewLayer;
        camera.aspect = (float)width / height;

        Vector3 cameraDir = (liveObject.transform.forward + Vector3.up * 0.3f).normalized;
        float cameraDistance = 5f; 

        cameraObject.transform.position = center + cameraDir * cameraDistance;
        cameraObject.transform.LookAt(center);

        camera.orthographicSize = 1.2f;

        AmbientMode previousAmbientMode = RenderSettings.ambientMode;
        Color previousAmbientLight = RenderSettings.ambientLight;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.6f, 1f);

        GameObject keyLightObject = new GameObject("Preview Soft Key Light");
        Light keyLight = keyLightObject.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.intensity = 0.8f;
        keyLight.color = Color.white;
        keyLight.cullingMask = 1 << PreviewLayer;
        keyLightObject.transform.rotation = Quaternion.Euler(30f, cameraObject.transform.eulerAngles.y - 30f, 0f);

        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        renderTexture.antiAliasing = 8; 

        camera.targetTexture = renderTexture;
        RenderTexture previousRT = RenderTexture.active;
        RenderTexture.active = renderTexture;

        GL.Clear(true, true, bgColor);
        camera.Render();

        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        foreach (var kvp in layerBackup)
        {
            if (kvp.Key != null) kvp.Key.layer = kvp.Value;
        }

        RenderTexture.active = previousRT;
        camera.targetTexture = null;
        RenderTexture.ReleaseTemporary(renderTexture);

        RenderSettings.ambientMode = previousAmbientMode;
        RenderSettings.ambientLight = previousAmbientLight;

        Object.Destroy(keyLightObject);
        Object.Destroy(cameraObject);

        if (saveToDisk)
        {
            byte[] bytes = result.EncodeToPNG();
            string folderPath = Path.Combine(Application.dataPath, "TestPreviews");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            string timeStamp = System.DateTime.Now.ToString("HHmmss");
            string filePath = Path.Combine(folderPath, $"{liveObject.name}_{timeStamp}.png");

            File.WriteAllBytes(filePath, bytes);
            Debug.Log($"<color=cyan>[Captured]</color> Saved as Png: {filePath}");
        }

        Rect rect = new Rect(0, 0, width, height);
        Vector2 pivot = new Vector2(0.5f, 0.5f);
        Sprite sprite = Sprite.Create(result, rect, pivot, 100f, 0, SpriteMeshType.FullRect);
        sprite.name = liveObject.name + "_LiveSprite";

        return sprite;
    }

    private static void SetLayerRecursivelyWithBackup(GameObject obj, int newLayer, Dictionary<GameObject, int> backup)
    {
        if (obj == null) return;
        backup[obj] = obj.layer;
        obj.layer = newLayer;   

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursivelyWithBackup(child.gameObject, newLayer, backup);
        }
    }

    private static Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.one);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
}