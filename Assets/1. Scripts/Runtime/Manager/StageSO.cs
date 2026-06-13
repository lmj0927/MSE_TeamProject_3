using System.Linq;
using UnityEngine;

// ScriptableObject holding configuration data for each stage
[CreateAssetMenu(fileName = "StageSO", menuName = "Scriptable Objects/StageSO")]
public class StageSO : ScriptableObject
{
    // Basic stage settings
    public string stageName;
    public string sceneName;
    public float stageTimeLimit;
    public float spawnRate;

    //  Score thresholds for star ratings
    [Header("Score Goals")]
    public int oneStarScore;
    public int twoStarScore;
    public int threeStarScore;

    // Available menus and ingredients
    [Header("Available Content")]
    public FoodSO[] availableIngredients;
    public RecipeSO[] availableAssemble;
    public RecipeSO[] availableSide;
    public RecipeSO[] availableBeverage;

#if UNITY_EDITOR
    // Auto-sort ingredients and remove nulls when modified in the Editor
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