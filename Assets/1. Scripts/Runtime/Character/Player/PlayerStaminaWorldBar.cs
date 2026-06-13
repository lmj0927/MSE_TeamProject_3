// Owned by JunYoung Park
using UnityEngine;

// World-space UI controller for player stamina bar
[DefaultExecutionOrder(40)]
public class PlayerStaminaWorldBar : MonoBehaviour
{
    [SerializeField] private Stamina stamina;
    [SerializeField] private ProgressBar progressBar;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 2.55f, 0f);
    [SerializeField] private Transform followRoot;
    [SerializeField] private bool useLookAtCamera = true;

    private Canvas _canvas;
    private RectTransform _canvasRect;
    private Camera _cam;
    private bool _detached;

    private void Awake()
    {
        if (followRoot == null)
            followRoot = transform;

        BindRefs();
        SetupCanvas();
    }

    private void Start()
    {
        if (!Application.isPlaying)
            return;

        if (followRoot == null)
            followRoot = transform;

        BindRefs();
        if (_canvasRect == null)
            return;

        _cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        SetupCanvas();

        // Prevent unwanted scaling/rotation inherited from player
        _canvasRect.SetParent(null, false);
        _detached = true;

        ApplyLookAtCameras(!useLookAtCamera);
        SyncWorldTransform();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
            return;

        BindRefs();
        if (stamina == null || progressBar == null)
            return;

        if (!_detached || _canvasRect == null)
            return;

        if (_canvas != null && _canvas.worldCamera == null)
            SetupCanvas();

        if (_cam == null)
            _cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();

        // Sync position after all player movements are done
        SyncWorldTransform();
        progressBar.SetProgress(stamina.Normalized);
    }

    private void OnDestroy()
    {
        if (_canvasRect != null)
            Destroy(_canvasRect.gameObject);
    }

    // Updating UI position and facing camera
    private void SyncWorldTransform()
    {
        if (followRoot == null || _canvasRect == null)
            return;

        _canvasRect.position = followRoot.TransformPoint(localOffset);

        if (useLookAtCamera && _cam != null)
            _canvasRect.rotation = _cam.transform.rotation;
    }

    private void BindRefs()
    {
        if (stamina == null)
            stamina = GetComponent<Stamina>();
        if (progressBar == null)
            progressBar = GetComponentInChildren<ProgressBar>(true);

        if (progressBar == null)
            return;

        if (_canvas == null)
            _canvas = progressBar.GetComponentInParent<Canvas>();

        if (_canvas != null)
            _canvasRect = _canvas.GetComponent<RectTransform>();
    }

    private void SetupCanvas()
    {
        if (_canvas == null)
            return;

        _cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        _canvas.worldCamera = _cam;
        _canvas.sortingOrder = 100;
    }

    private void ApplyLookAtCameras(bool enable)
    {
        if (progressBar == null)
            return;

        foreach (var lookAt in progressBar.GetComponentsInChildren<LookAtCamera>(true))
            lookAt.enabled = enable;
    }



#if UNITY_EDITOR
    // Editor preview for UI offset positioning
    private void OnValidate()
    {
        if (Application.isPlaying)
            return;
        if (followRoot == null)
            followRoot = transform;
        BindRefs();
        if (_canvasRect != null && !_detached && _canvasRect.transform.IsChildOf(transform))
            _canvasRect.localPosition = localOffset;
    }
#endif
}