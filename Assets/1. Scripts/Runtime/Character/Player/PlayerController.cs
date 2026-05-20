// Owned by SeungYeon Jung
using Fusion;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("Food")]
    [Networked] public NetworkObject HeldFoodObject { get; set; }
    [SerializeField] private Food heldFood;
    [SerializeField] private Transform holdAnchor;

    public Food HeldFood => heldFood;
    public bool HasFood() => heldFood != null;

    public override void Spawned()
    {
        if (holdAnchor == null)
            holdAnchor = transform;
    }

    /// <summary>
    /// Add a food (networked) object to the player.
    /// </summary>
    /// <param name="foodNO">A food object created by `FoodSpawner`.</param>
    /// <returns>Success or fail.</returns>
    public bool AddFood(NetworkObject foodNO)
    {
        if (foodNO == null || heldFood != null)
            return false;

        var otherOwner = foodNO.GetComponentInParent<PlayerController>();
        if (otherOwner != null && otherOwner != this)
            return false;

        HeldFoodObject = foodNO;
        HeldFoodObject.RequestStateAuthority();
        if(!HeldFoodObject.HasStateAuthority)
        {
            Debug.LogError("[PlayerController AddFood] Failed to get authority!");
        }
        heldFood = foodNO.GetComponent<Food>();
        // AttachHeldFood(heldFood);
        return true;
    }

    /// <summary>
    /// Remove food player is holding.
    /// </summary>
    /// <returns>Removed food without rigidbody.</returns>
    public NetworkObject RemoveFood()
    {
        Food removed = heldFood;
        if (removed == null)
            return null;

        // DetachHeldFood(removed);
        HeldFoodObject = null;
        heldFood = null;
        return removed.Object;
    }

    /// <summary>
    /// Remove food player is holding after restoring rigidbody.
    /// </summary>
    /// <returns>Removed food with rigidbody.</returns>
    public NetworkObject RemoveFoodAndRestoreRigidbody()
    {
        NetworkObject foodNO = RemoveFood();
        if(foodNO == null) return null;

        // restore the rigidbody
        foreach (var rb in foodNO.GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = false;
        foreach (var col in foodNO.GetComponentsInChildren<Collider>())
            col.enabled = true;

        return foodNO;
    }

    private void AttachHeldFood(Food food)
    {
        food.transform.SetParent(holdAnchor, true);
        food.transform.localPosition = Vector3.zero;
        food.transform.localRotation = Quaternion.identity;

        foreach (var rb in food.GetComponentsInChildren<Rigidbody>())
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            
        }

        foreach (var col in food.GetComponentsInChildren<Collider>())
            col.enabled = false;
    }

    private void DetachHeldFood(Food food)
    {
        food.transform.SetParent(null, true);

        foreach (var rb in food.GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = false;

        foreach (var col in food.GetComponentsInChildren<Collider>())
            col.enabled = true;
    }

    public void FreezeMovement(bool apply)
    {
        GetComponent<PlayerMovement>().SetInteracting(apply);
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if(HeldFoodObject == null) return;

        // follow the hold anchor
        HeldFoodObject.transform.position = holdAnchor.position;
        HeldFoodObject.transform.rotation = holdAnchor.rotation;

        // disable the collisions
        foreach (var rb in HeldFoodObject.GetComponentsInChildren<Rigidbody>())
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        foreach (var col in HeldFoodObject.GetComponentsInChildren<Collider>())
            col.enabled = false;
    }
}
