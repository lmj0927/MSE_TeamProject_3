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
}
