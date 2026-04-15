using minjun;

public class BaseCounter : Counter
{
    public override void Interact(Player player)
    {
        // 플레이어가 음식을 들고 있고 카운터에 음식이 없을 때
        if (player.HasFood() && !HasFood())
        {
            AddFood(player.RemoveFood());
        }
        // 플레이어가 음식을 들고 있지 않고 카운터에 음식이 있을 때
        else if(!player.HasFood() && HasFood())
        {
            player.AddFood(RemoveFood());   
        }
    }
}
