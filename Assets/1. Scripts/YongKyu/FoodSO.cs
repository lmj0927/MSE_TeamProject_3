using UnityEngine;

[CreateAssetMenu]
public class FoodSO : ScriptableObject
{

    [Header("Food Info")]
    [SerializeField] private string foodName;
    [SerializeField] private GameObject prefab;
    [SerializeField] private Sprite sprite;

    public string FoodName => foodName;
    public GameObject Prefab => prefab;
    public Sprite Sprite => sprite;

    public Food CreateFood()
    {
        var food = Instantiate(prefab);
        food.AddComponent<Food>();
        food.GetComponent<Food>().SetData(this);
        return food.GetComponent<Food>();
    }

}
