using UnityEngine;

[CreateAssetMenu]
public class FoodSO : ScriptableObject
{

    [Header("Food Info")]
    [SerializeField] private string foodName;
    [SerializeField] private GameObject prefab;

    public string FoodName => foodName;
    public GameObject Prefab => prefab;

}
