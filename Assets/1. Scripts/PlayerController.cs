using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Food")]
    [SerializeField] private Food heldFood;

    public bool HasFood() => heldFood != null;

    // 추후 Interact에서 근처 Food를 찾아 넘겨줄 수 있게 오버로드 제공
    public bool AddFood(Food food)
    {
        if (food == null || heldFood != null)
            return false;

        heldFood = food;
        return true;
    }

    
    public void AddFood()
    {
        // Interact 구현 시: 근처 Food 탐색 후 AddFood(food) 호출 예정
    }

    public Food RemoveFood()
    {
        var removed = heldFood;
        heldFood = null;
        return removed;
    }
}
