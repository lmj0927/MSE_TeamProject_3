using UnityEngine;
//using sy;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private global::Food heldFood;
    [SerializeField] private Transform holdAnchor;
    [SerializeField] private float pickupRadius = 1.25f;
    [SerializeField] private Vector3 pickupProbeOffset = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private LayerMask pickupLayers = ~0;

    public global::Food HeldFood => heldFood;
    public bool HasFood() => heldFood != null;

    private void Awake()
    {
        if (holdAnchor == null)
            holdAnchor = transform;
    }

    public bool AddFood(global::Food food)
    {
        if (food == null || heldFood != null)
            return false;

        var otherOwner = food.GetComponentInParent<PlayerController>();
        if (otherOwner != null && otherOwner != this)
            return false;

        heldFood = food;
        AttachHeldFood(food);
        return true;
    }

    public bool AddFood()
    {
        if (heldFood != null)
            return false;

        var found = FindNearestPickupableFood();
        return found != null && AddFood(found);
    }

    public global::Food RemoveFood()
    {
        var removed = heldFood;
        if (removed == null)
            return null;

        DetachHeldFood(removed);
        heldFood = null;
        return removed;
    }

    private void AttachHeldFood(global::Food food)
    {
        food.transform.SetParent(holdAnchor, true);
        food.transform.localPosition = Vector3.zero;
        food.transform.localRotation = Quaternion.identity;

        foreach (var rb in food.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        foreach (var col in food.GetComponentsInChildren<Collider>())
            col.enabled = false;
    }

    private void DetachHeldFood(global::Food food)
    {
        food.transform.SetParent(null, true);

        foreach (var rb in food.GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = false;

        foreach (var col in food.GetComponentsInChildren<Collider>())
            col.enabled = true;
    }

    private global::Food FindNearestPickupableFood()
    {
        var center = transform.position + pickupProbeOffset;
        var cols = Physics.OverlapSphere(center, pickupRadius, pickupLayers, QueryTriggerInteraction.Collide);

        global::Food best = null;
        float bestSqr = float.MaxValue;

        foreach (var col in cols)
        {
            if (col == null)
                continue;

            var food = col.GetComponentInParent<global::Food>();
            if (food == null)
                continue;

            var owner = food.GetComponentInParent<PlayerController>();
            if (owner != null && owner != this)
                continue;

            float sqr = (food.transform.position - center).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = food;
            }
        }

        return best;
    }
}
