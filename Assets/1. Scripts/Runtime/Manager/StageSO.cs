using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "StageSO", menuName = "Scriptable Objects/StageSO")]
public class StageSO : ScriptableObject
{
    public string stageName;
    public string sceneName;     
    public float stageTimeLimit;
    public float spawnRate;

    [Header("Score Goals")]
    public int oneStarScore;
    public int twoStarScore;
    public int threeStarScore;

    [Header("Available Content")]
    public FoodSO[] availableIngredients;
    public RecipeSO[] availableAssemble;
    public RecipeSO[] availableSide;
    public RecipeSO[] availableBeverage;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (availableIngredients != null && availableIngredients.Length > 0)
        {
            availableIngredients = availableIngredients
                .Where(item => item != null)    
                .OrderBy(item => item.FoodName)   
                .ToArray();
        }
    }
#endif
}
