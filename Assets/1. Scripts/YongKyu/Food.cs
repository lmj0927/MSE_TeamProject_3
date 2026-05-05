using UnityEngine;

public class Food : MonoBehaviour
{

    public FoodSO data;

    public FoodSO Data => data;
    public void SetData(FoodSO data)
    {
        this.data = data;
    }
}
