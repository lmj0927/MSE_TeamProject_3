// Owned by YongKyu Lee
using UnityEngine;

[CreateAssetMenu]
public class FoodSO : ScriptableObject
{
    public enum FoodType
    {
        Raw,
        Baked,
        Sliced,
        Fried,
        Main,
        Side,
        Beverage
    }

    [Header("Food Info")]
    [SerializeField] private string foodName;
    [SerializeField] private GameObject prefab;
    [SerializeField] private Sprite sprite;
    [SerializeField] private FoodType type;
    [SerializeField] private int point = 0;


    public string FoodName => foodName;
    public GameObject Prefab => prefab;
    public Sprite Sprite => sprite;
    public FoodType Type => type;
    public int Point => point;


    // public Food CreateFood()
    // {
    //     Debug.LogWarning("[FoodSO CreateFood] Deprecated method.");
    //     return null;
    //     // var food = Instantiate(prefab);
    //     // food.AddComponent<Food>();
    //     // food.GetComponent<Food>().SetData(this);
    //     // return food.GetComponent<Food>();
    // }

}
