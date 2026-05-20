// Owned by SeungYeon Jung
using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Food")]
    [SerializeField] private Food heldFood;
    [SerializeField] private Transform holdAnchor;

    public Food HeldFood => heldFood;
    public bool HasFood() => heldFood != null;

    private void Awake()
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

        List<FoodSO> holding = new List<FoodSO>();

        holding.Add(food.data); 
        var recipe = RecipeManager.Instance.Cook(holding, RecipeType.Side);
        if (recipe != null)
        {
            Destroy(food.gameObject);
            food = recipe.Result.CreateFood();
        }

        heldFood = food;
        AttachHeldFood(food);
        return true;
    }

    public Food RemoveFood()
    {
        var removed = heldFood;
        if (removed == null)
            return null;

        DetachHeldFood(removed);
        heldFood = null;
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
}
