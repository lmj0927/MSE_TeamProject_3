// Owned by JunYoung Park
using UnityEngine;

/// <summary>
/// Interacts with nearby <see cref="IInteractable"/> objects (e.g., counters) using the E key.
/// Displays an outline on the looked-at target using Linework Lite.
/// </summary>
public class PlayerInteractInput : MonoBehaviour
{
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private PlayerController counterPlayer;
    [SerializeField] private float interactRadius = 2.5f;
    [SerializeField] private float probeHeight = 0.85f;
    [SerializeField][Range(-1f, 1f)] private float minForwardDot = 0.15f;
    [SerializeField] private LayerMask interactLayers = ~0;

    [Header("Outline Settings")]
    [SerializeField] private int outlineLayerIndex = 1;

    private IInteractable currentTarget;

    private void Awake()
    {
        if (counterPlayer == null)
            counterPlayer = GetComponent<PlayerController>();
    }

    private void Update()
    {
        UpdateTargetAndOutline();

        // Trigger interaction when the interact key is pressed
        if (Input.GetKeyDown(interactKey))
        {
            if (currentTarget != null)
            {
                currentTarget.Interact(counterPlayer);
            }
        }
    }

    // Public method to force interaction (e.g., for UI buttons)
    public void Interact()
    {
        if (currentTarget != null)
            currentTarget.Interact(counterPlayer);
    }

    // Find the closest valid interactable object within viewing angle
    private void UpdateTargetAndOutline()
    {
        if (counterPlayer == null) return;

        var center = transform.position + Vector3.up * probeHeight;
        var cols = Physics.OverlapSphere(center, interactRadius, interactLayers, QueryTriggerInteraction.Collide);

        IInteractable best = null;
        float bestSqr = float.MaxValue;

        foreach (var col in cols)
        {
            if (col == null) continue;

            var interactable = col.GetComponentInParent<IInteractable>();
            if (interactable == null) continue;

            if (interactable is MonoBehaviour mb && !mb.isActiveAndEnabled) continue;

            var targetPoint = col.bounds.center;
            var toTarget = targetPoint - center;
            float sqr = toTarget.sqrMagnitude;

            if (sqr < 0.0001f)
            {
                best = interactable;
                bestSqr = 0f;
                break;
            }

            Vector3 flatForward = new Vector3(transform.forward.x, 0, transform.forward.z);
            Vector3 flatToTarget = new Vector3(toTarget.x, 0, toTarget.z).normalized;

            // Filter out objects outside the forward view cone
            float dot = Vector3.Dot(flatForward, flatToTarget);
            if (dot < minForwardDot) continue;

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = interactable;
            }
        }

        // Update outline rendering state if the target has changed
        if (currentTarget != best)
        {
            if (currentTarget != null)
            {
                SetOutline(currentTarget, false);
            }

            currentTarget = best;

            if (currentTarget != null)
            {
                SetOutline(currentTarget, true);
            }
        }
    }

    private void SetOutline(IInteractable interactable, bool show)
    {
        if (interactable is MonoBehaviour mb)
        {
            Transform outlineT = mb.transform;

            if (mb is ACounter ac && ac.OutlineRoot != null)
            {
                outlineT = ac.OutlineRoot;
            }
            Renderer[] renderers = outlineT.GetComponentsInChildren<Renderer>();


            uint layerBit = 1u << outlineLayerIndex;

            foreach (var r in renderers)
            {
                // Exclude held food items from being outlined
                if (r.GetComponentInParent<Food>() != null) continue;

                if (show)
                {
                    r.renderingLayerMask |= layerBit;
                }
                else
                {
                    r.renderingLayerMask &= ~layerBit;
                }
            }
        }
    }
}