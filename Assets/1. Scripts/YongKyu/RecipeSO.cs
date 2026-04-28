using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu]
public class RecipeSO : ScriptableObject
{
    public FoodSO result;
    public List<FoodSO> ingredients;
}
