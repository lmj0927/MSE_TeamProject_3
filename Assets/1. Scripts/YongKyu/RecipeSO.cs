using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu]
public class RecipeSO : ScriptableObject
{
    [SerializeField] private FoodSO result;
    [SerializeField] private List<FoodSO> ingredients;

    /// <summary>
    /// value for cooking
    /// If the recipe is for firinig, it means the timer.
    /// If the recipe is for slicing, it means the number of slicing.
    /// If the recipe is for assembling, it has no meaning.
    /// </summary>
    [SerializeField] private int value;

    public FoodSO Result => result;
    public List<FoodSO> Ingredients => ingredients;
    public int Value => value;
}
