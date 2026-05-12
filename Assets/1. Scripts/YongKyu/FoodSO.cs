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
        Side
    }

    [Header("Food Info")]
    [SerializeField] private string foodName;
    [SerializeField] private GameObject prefab;
    [SerializeField] private Sprite sprite;
    [SerializeField] private FoodType type;

    public string FoodName => foodName;
    public GameObject Prefab => prefab;
    public Sprite Sprite => sprite;
    public FoodType Type => type;


    public Food CreateFood()
    {
        var food = Instantiate(prefab);
        food.AddComponent<Food>();
        food.GetComponent<Food>().SetData(this);
        return food.GetComponent<Food>();
    }

}
