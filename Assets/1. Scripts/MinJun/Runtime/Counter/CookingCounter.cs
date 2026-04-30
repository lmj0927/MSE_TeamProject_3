using UnityEngine;
using minjun;

public class CookingCounter : ACounter
{
    public override void Interact(Player player)
    {
        // Cooking Counter는 음식 여러개 추가 가능
        if (player.HasFood())
        {
            AddFood(player.RemoveFood());
        }
        else if (!player.HasFood())
        {
            //TODO : Check Recipe and Cook Food
        }
    }
}
