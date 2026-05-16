#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;

public class PrefabPreviewCaptureWindow : EditorWindow
{
    private const int PreviewLayer = 31;

    // 조절 가능한 옵션들
    private int resolution = 512;
    private float zoomPadding = 1.1f;
    private Vector3 cameraAngles = new Vector3(19f, 149f, 0f);
    private Vector2 cameraOffset = Vector2.zero;

    private float keyLightIntensity = 0.23f;
    private float fillLightIntensity = 0.1f;

    [MenuItem("Tools/Prefab Preview Capturer")]
    public static void ShowWindow()
    {
        // 에디터 윈도우 띄우기
        GetWindow<PrefabPreviewCaptureWindow>("Preview Capturer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Capture Settings", EditorStyles.boldLabel);

        resolution = EditorGUILayout.IntField("Resolution (Size)", resolution);

        GUILayout.Space(10);
        GUILayout.Label("Camera Settings", EditorStyles.boldLabel);
        // 줌 조절 (숫자가 작을수록 확대됨)
        zoomPadding = EditorGUILayout.Slider("Zoom Padding (크기 조절)", zoomPadding, 0.1f, 5f);
        // 상하좌우 미세 조정
        cameraOffset = EditorGUILayout.Vector2Field("Camera Offset (위치 조정)", cameraOffset);
        // 카메라 각도 조절
        cameraAngles = EditorGUILayout.Vector3Field("Camera Rotation (각도)", cameraAngles);

        GUILayout.Space(10);
        GUILayout.Label("Light Settings", EditorStyles.boldLabel);
        keyLightIntensity = EditorGUILayout.Slider("Key Light (주 조명)", keyLightIntensity, 0f, 2f);
        fillLightIntensity = EditorGUILayout.Slider("Fill Light (보조 조명)", fillLightIntensity, 0f, 2f);

        GUILayout.Space(20);

        if (GUILayout.Button("Capture Selected Prefab", GUILayout.Height(40)))
        {
            CaptureSelectedPrefab();
        }
    }

    private void CaptureSelectedPrefab()
    {
        GameObject selectedPrefab = Selection.activeGameObject;

        if (selectedPrefab == null)
        {
            Debug.LogWarning("Select a prefab in the project window.");
            return;
        }

        string folderPath = "Assets/PrefabPreviews";

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "PrefabPreviews");
        }

        string assetPath = $"{folderPath}/{selectedPrefab.name}_Preview.png";
        string fullPath = Path.Combine(Application.dataPath, $"PrefabPreviews/{selectedPrefab.name}_Preview.png");

        Texture2D texture = RenderPrefabToTexture(selectedPrefab, resolution, resolution);

        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(fullPath, bytes);

        DestroyImmediate(texture);

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

    private Texture2D RenderPrefabToTexture(GameObject prefab, int width, int height)
    {
        GameObject instance = Instantiate(prefab);

        instance.name = prefab.name + "_PreviewInstance";
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        SetActiveRecursively(instance, true);
        SetLayerRecursively(instance, PreviewLayer);

        Bounds bounds = CalculateBounds(instance);

        float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxSize <= 0.0001f) maxSize = 1f;

        Vector3 center = bounds.center;

        GameObject cameraObject = new GameObject("Preview Camera");
        Camera camera = cameraObject.AddComponent<Camera>();

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.orthographic = true;
        camera.nearClipPlane = -maxSize * 10f; // 오브젝트가 잘리지 않게 넉넉히
        camera.farClipPlane = maxSize * 20f;
        camera.cullingMask = 1 << PreviewLayer;
        camera.allowMSAA = false;
        camera.aspect = (float)width / height;

        // ⭐ UI에서 설정한 값으로 카메라 위치와 각도 세팅
        Quaternion camRotation = Quaternion.Euler(cameraAngles);
        Vector3 viewDirection = camRotation * Vector3.forward;
        float cameraDistance = maxSize * 3.0f;

        cameraObject.transform.rotation = camRotation;
        cameraObject.transform.position = center - viewDirection * cameraDistance;

        // 수동 오프셋(Offset) 적용 (너무 쏠려있을 때 중심을 맞추는 용도)
        cameraObject.transform.position += cameraObject.transform.right * cameraOffset.x;
        cameraObject.transform.position += cameraObject.transform.up * cameraOffset.y;

        // 수동 줌 패딩 적용
        camera.orthographicSize = GetOrthographicSizeForBounds(bounds, camera, zoomPadding);

        AmbientMode previousAmbientMode = RenderSettings.ambientMode;
        Color previousAmbientLight = RenderSettings.ambientLight;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.62f, 0.62f, 1f);

        GameObject keyLightObject = new GameObject("Preview Soft Key Light");
        Light keyLight = keyLightObject.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.intensity = keyLightIntensity; // UI 값 적용
        keyLight.color = Color.white;
        keyLight.cullingMask = 1 << PreviewLayer;
        keyLightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

        GameObject fillLightObject = new GameObject("Preview Soft Fill Light");
        Light fillLight = fillLightObject.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.intensity = fillLightIntensity; // UI 값 적용
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

        RenderSettings.ambientMode = previousAmbientMode;
        RenderSettings.ambientLight = previousAmbientLight;

        DestroyImmediate(renderTexture);
        DestroyImmediate(fillLightObject);
        DestroyImmediate(keyLightObject);
        DestroyImmediate(cameraObject);
        DestroyImmediate(instance);

        return result;
    }

    private Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            return new Bounds(obj.transform.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

        return bounds;
    }

    private float GetOrthographicSizeForBounds(Bounds bounds, Camera camera, float padding)
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

    private Vector3[] GetBoundsCorners(Bounds bounds)
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

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void SetActiveRecursively(GameObject obj, bool active)
    {
        obj.SetActive(active);
        foreach (Transform child in obj.transform)
        {
            SetActiveRecursively(child.gameObject, active);
        }
    }
}
#endif