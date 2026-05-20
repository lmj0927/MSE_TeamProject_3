// Owned by SeungYeon Jung
using Fusion;
using Mono.Cecil.Cil;
using Unity.VisualScripting;
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

    public bool AddFood(Food food)
    {
        if (food == null || heldFood != null)
            return false;

        var otherOwner = food.GetComponentInParent<PlayerController>();
        if (otherOwner != null && otherOwner != this)
            return false;

        HeldFoodObject = food.Object;
        heldFood = food;
        AttachHeldFood(food);
        return true;
    }

    public Food RemoveFood()
    {
        Food removed = heldFood;
        if (removed == null)
            return null;

        DetachHeldFood(removed);
        HeldFoodObject = null;
        // heldFood = null;
        return removed;
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

    public override void Render()
    {
        if(HeldFoodObject)
        {
            heldFood.transform.SetParent(holdAnchor, true);
            heldFood.transform.localPosition = Vector3.zero;
            heldFood.transform.localRotation = Quaternion.identity;

            foreach (var rb in heldFood.GetComponentsInChildren<Rigidbody>())
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                
            }

            foreach (var col in heldFood.GetComponentsInChildren<Collider>())
                col.enabled = false;
        }
        else if(heldFood)
        {
            heldFood.transform.SetParent(null, true);

            foreach (var rb in heldFood.GetComponentsInChildren<Rigidbody>())
                rb.isKinematic = false;

            foreach (var col in heldFood.GetComponentsInChildren<Collider>())
                col.enabled = true;

            heldFood = null;    
        }
    }
}
