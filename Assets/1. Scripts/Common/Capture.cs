#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;

public class PrefabPreviewCapture
{
    private const int PreviewLayer = 31;

    [MenuItem("Tools/Capture Selected Prefab Preview Transparent")]
    public static void CaptureSelectedPrefab()
    {
        GameObject selectedPrefab = Selection.activeGameObject;

        if (selectedPrefab == null)
        {
            Debug.LogWarning("Select a prefab in the project window.");
            return;
        }

        int width = 512;
        int height = 512;

        string folderPath = "Assets/PrefabPreviews";

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "PrefabPreviews");
        }

        string assetPath = $"{folderPath}/{selectedPrefab.name}_Preview.png";
        string fullPath = Path.Combine(Application.dataPath, $"PrefabPreviews/{selectedPrefab.name}_Preview.png");

        Texture2D texture = RenderPrefabToTexture(selectedPrefab, width, height);

        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(fullPath, bytes);

        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(assetPath);

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.SaveAndReimport();
        }

        AssetDatabase.Refresh();

        Debug.Log($"Sprite preview saved: {assetPath}");
    }

    private static Texture2D RenderPrefabToTexture(GameObject prefab, int width, int height)
    {
        GameObject instance = Object.Instantiate(prefab);

        instance.name = prefab.name + "_PreviewInstance";
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        SetActiveRecursively(instance, true);
        SetLayerRecursively(instance, PreviewLayer);

        Bounds bounds = CalculateBounds(instance);

        float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

        if (maxSize <= 0.0001f)
        {
            maxSize = 1f;
        }

        Vector3 center = bounds.center;

        GameObject cameraObject = new GameObject("Preview Camera");
        Camera camera = cameraObject.AddComponent<Camera>();

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.orthographic = true;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = maxSize * 20f;
        camera.cullingMask = 1 << PreviewLayer;
        camera.allowMSAA = false;
        camera.aspect = (float)width / height;

        // Bounds 크기에 비례해서 살짝 위에서 내려다보는 3/4 view.
        Vector3 viewDirection = new Vector3(0.6f, -0.4f, -1f).normalized;
        float cameraDistance = maxSize * 3.0f;

        cameraObject.transform.position = center - viewDirection * cameraDistance;
        cameraObject.transform.rotation = Quaternion.LookRotation(viewDirection, Vector3.up);

        // 프리팹 전체가 화면 안에 들어오도록 orthographic size 자동 계산.
        float padding = 1.25f;
        camera.orthographicSize = GetOrthographicSizeForBounds(bounds, camera, padding);


        // 기존 씬 조명/환경 설정 백업.
        AmbientMode previousAmbientMode = RenderSettings.ambientMode;
        Color previousAmbientLight = RenderSettings.ambientLight;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.62f, 0.62f, 1f);

        // 은은한 키 라이트.
        GameObject keyLightObject = new GameObject("Preview Soft Key Light");
        Light keyLight = keyLightObject.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.intensity = 0.55f;
        keyLight.color = Color.white;
        keyLight.cullingMask = 1 << PreviewLayer;
        keyLightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

        // 약한 필 라이트.
        GameObject fillLightObject = new GameObject("Preview Soft Fill Light");
        Light fillLight = fillLightObject.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.intensity = 0.1f;
        fillLight.color = Color.white;
        fillLight.cullingMask = 1 << PreviewLayer;
        fillLightObject.transform.rotation = Quaternion.Euler(25f, 140f, 0f);

        RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 1,
            useMipMap = false,
            autoGenerateMips = false
        };

        renderTexture.Create();

        camera.targetTexture = renderTexture;

        RenderTexture previousRT = RenderTexture.active;
        RenderTexture.active = renderTexture;

        GL.Clear(true, true, new Color(0f, 0f, 0f, 0f));

        camera.Render();

        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = previousRT;
        camera.targetTexture = null;

        // RenderSettings 복구.
        RenderSettings.ambientMode = previousAmbientMode;
        RenderSettings.ambientLight = previousAmbientLight;

        Object.DestroyImmediate(renderTexture);
        Object.DestroyImmediate(fillLightObject);
        Object.DestroyImmediate(keyLightObject);
        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(instance);

        return result;
    }

    private static Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            Debug.LogWarning("No renderer in the selected prefab.");
            return new Bounds(obj.transform.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;

        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

        return bounds;
    }

    private static float GetOrthographicSizeForBounds(Bounds bounds, Camera camera, float padding)
    {
        Vector3[] corners = GetBoundsCorners(bounds);

        Matrix4x4 worldToCamera = camera.worldToCameraMatrix;

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;

        foreach (Vector3 corner in corners)
        {
            Vector3 cameraSpacePoint = worldToCamera.MultiplyPoint(corner);

            minX = Mathf.Min(minX, cameraSpacePoint.x);
            maxX = Mathf.Max(maxX, cameraSpacePoint.x);
            minY = Mathf.Min(minY, cameraSpacePoint.y);
            maxY = Mathf.Max(maxY, cameraSpacePoint.y);
        }

        float projectedWidth = maxX - minX;
        float projectedHeight = maxY - minY;

        float sizeByHeight = projectedHeight * 0.5f;
        float sizeByWidth = projectedWidth * 0.5f / camera.aspect;

        return Mathf.Max(sizeByHeight, sizeByWidth) * padding;
    }

    private static Vector3[] GetBoundsCorners(Bounds bounds)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        return new Vector3[]
        {
            center + new Vector3( extents.x,  extents.y,  extents.z),
            center + new Vector3( extents.x,  extents.y, -extents.z),
            center + new Vector3( extents.x, -extents.y,  extents.z),
            center + new Vector3( extents.x, -extents.y, -extents.z),
            center + new Vector3(-extents.x,  extents.y,  extents.z),
            center + new Vector3(-extents.x,  extents.y, -extents.z),
            center + new Vector3(-extents.x, -extents.y,  extents.z),
            center + new Vector3(-extents.x, -extents.y, -extents.z),
        };
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static void SetActiveRecursively(GameObject obj, bool active)
    {
        obj.SetActive(active);

        foreach (Transform child in obj.transform)
        {
            SetActiveRecursively(child.gameObject, active);
        }
    }
}
#endif