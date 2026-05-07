using UnityEngine;

/// <summary>
/// E 키로 주변 <see cref="IInteractable"/> (MinJun 카운터 등)과 상호작용합니다.
/// </summary>
public class PlayerInteractInput : MonoBehaviour
{
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private PlayerController counterPlayer;
    [SerializeField] private float interactRadius = 2.5f;
    [SerializeField] private float probeHeight = 0.85f;
    [SerializeField] [Range(-1f, 1f)] private float minForwardDot = 0.15f;
    [SerializeField] private LayerMask interactLayers = ~0;

    private void Awake()
    {
        if (counterPlayer == null)
            counterPlayer = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(interactKey))
            TryInteract();
    }

    public void Interact() => TryInteract();

    public void TryInteract()
    {
        if (counterPlayer == null)
            counterPlayer = GetComponent<PlayerController>();
        if (counterPlayer == null)
            return;

        var center = transform.position + Vector3.up * probeHeight;
        var cols = Physics.OverlapSphere(center, interactRadius, interactLayers, QueryTriggerInteraction.Collide);

        IInteractable best = null;
        float bestSqr = float.MaxValue;

        foreach (var col in cols)
        {
            if (col == null)
                continue;

            var interactable = col.GetComponentInParent<IInteractable>();
            if (interactable == null)
                continue;
            if (interactable is MonoBehaviour mb && !mb.isActiveAndEnabled)
                continue;

            var targetPoint = col.bounds.center;
            var toTarget = targetPoint - center;
            float sqr = toTarget.sqrMagnitude;
            if (sqr < 0.0001f)
            {
                best = interactable;
                bestSqr = 0f;
                break;
            }

            float dot = Vector3.Dot(transform.forward, toTarget / Mathf.Sqrt(sqr));
            if (dot < minForwardDot)
                continue;

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = interactable;
            }
        }

        if (best != null)
            best.Interact(counterPlayer);
    }
}
