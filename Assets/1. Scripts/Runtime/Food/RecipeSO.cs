// Owned by YongKyu Lee
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu]
public class RecipeSO : ScriptableObject
{
    [SerializeField] private FoodSO result;
    /// <summary>
    /// If the recipe is for beverage, result must be only elements of ingredients
    /// </summary>
    [SerializeField] private List<FoodSO> ingredients;
    [SerializeField] private List<RecipeSO> complements;

    /// <summary>
    /// value for cooking
    /// If the recipe is for fir or oil, it means the timer.
    /// If the recipe is for slicing, it means the number of slicing.
    /// If the recipe is for assembling, it has no meaning.
    /// If the recipe is for beverage, it meas the range of interact timing
    /// </summary>
    [SerializeField] private int value;

    public FoodSO Result => result; 
    public List<FoodSO> Ingredients => ingredients;             
    public List<RecipeSO> Complements => complements;
    public int Value => value;
}
