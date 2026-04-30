using UnityEngine;
using UnityEngine.UI;

public class StaminaBarWorld : MonoBehaviour
{
    [SerializeField] private Stamina stamina;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private Vector2 size = new Vector2(120f, 14f);
    [SerializeField] private float worldScale = 0.02f;
    [SerializeField] private bool billboardToCamera = true;
    [SerializeField] private bool hideWhenFull = false;

    [Header("Colors")]
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private Color fillColor = new Color(0.2f, 0.9f, 0.3f, 0.95f);

    [Header("Runtime UI (auto-created if empty)")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private Image fillImage;

    private Camera _cam;

    private void Awake()
    {
        if (stamina == null)
            stamina = GetComponentInParent<Stamina>();

        _cam = Camera.main;

        if (worldCanvas == null || fillImage == null)
            CreateUiIfNeeded();
    }

    private void LateUpdate()
    {
        if (stamina == null || worldCanvas == null || fillImage == null)
            return;

        worldCanvas.transform.position = transform.position + worldOffset;

        if (billboardToCamera)
        {
            if (_cam == null)
                _cam = Camera.main;

            if (_cam != null)
                worldCanvas.transform.rotation = Quaternion.LookRotation(_cam.transform.position - worldCanvas.transform.position, Vector3.up);
        }

        float t = stamina.Normalized;
        fillImage.fillAmount = t;

        if (hideWhenFull)
            worldCanvas.gameObject.SetActive(t < 0.999f);
    }

    private void CreateUiIfNeeded()
    {
        var canvasGo = new GameObject("StaminaBarCanvas");
        canvasGo.transform.SetParent(transform, worldPositionStays: false);
        canvasGo.transform.localPosition = worldOffset;

        worldCanvas = canvasGo.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.sortingOrder = 50;

        var canvasRect = worldCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = size;
        canvasRect.pivot = new Vector2(0.5f, 0.5f);
        canvasRect.localScale = Vector3.one * Mathf.Max(0.0001f, worldScale);

        var uiSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        if (uiSprite == null)
        {
            var tex = Texture2D.whiteTexture;
            uiSprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
        }

        // Background
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(canvasGo.transform, worldPositionStays: false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        var bgImage = bgGo.AddComponent<Image>();
        bgImage.sprite = uiSprite;
        bgImage.color = backgroundColor;

        // Fill
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(bgGo.transform, worldPositionStays: false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        fillImage = fillGo.AddComponent<Image>();
        fillImage.sprite = uiSprite;
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = stamina != null ? stamina.Normalized : 1f;
    }
}

