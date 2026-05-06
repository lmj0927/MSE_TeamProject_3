using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using Unity.VisualScripting;

public class RecipeManager : Singleton<RecipeManager>
{

    /* RecipeManager */

    [SerializeField]
    List<RecipeSO> assembleRecipes;
    [SerializeField]
    List<RecipeSO> fireRecipes;
    [SerializeField]
    List<RecipeSO> sliceRecipes;

    [SerializeField]
    FoodSO trashFood;

    private List<FoodSO> copyIng = new List<FoodSO>();

    /// <summary>
    /// Returns a randomly selected assemble recipe from the assembleRecipes list.
    /// If the list is null or empty, returns null.
    /// </summary>
    /// <returns>
    /// A randomly selected RecipeSO, or null if no assemble recipe is available.
    /// </returns>
    public RecipeSO GiveRandomAssembleRecipe()
    {
        if(assembleRecipes == null || assembleRecipes.Count == 0)
        {
            Debug.LogError("assembleRecipe is not valid. Null or Empty.");
            return null;
        }
        return assembleRecipes[Random.Range(0, assembleRecipes.Count)];
    }


    /// <summary>
    /// Attempts to cook a food item using the given ingredients and recipe type.
    /// 
    /// The method first selects the appropriate recipe list based on the given RecipeType.
    /// It then checks each recipe in that list to see whether all required ingredients
    /// are included in the provided ingredient list.
    /// 
    /// If a matching recipe is found, the recipe's result food is returned.
    /// If the recipe type is invalid or no matching recipe is found, trashFood is returned.
    /// </summary>
    /// <param name="ingredients">
    /// The list of ingredients provided by the player.
    /// </param>
    /// <param name="type">
    /// The type of recipe to search, such as Assemble, Fire, or Slice.
    /// </param>
    /// <returns>
    /// The resulting FoodSO if a matching recipe is found; otherwise, trashFood. 
    /// If type is invalid, null will be returned.
    /// </returns>
    public RecipeSO Cook(List<FoodSO> ingredients, RecipeType type)
    {
        List<RecipeSO> recipes;
        switch(type)
        {
            case RecipeType.Assemble:
                recipes = assembleRecipes;
                break;
            case RecipeType.Fire:
                recipes = fireRecipes;
                break;
            case RecipeType.Slice:
                recipes = sliceRecipes;
                break;
            default:
                recipes = null;
                break;
        }
        if(recipes == null) {
            Debug.LogError("Invalid Recipe Type");
            return null;
        }

        foreach(RecipeSO recipe in recipes)
        {
            bool recFind = false;

            if (recipe.Ingredients.Count == ingredients.Count)
            {
                copyIng.Clear();
                copyIng.AddRange(ingredients);

                recFind = true;

                foreach (FoodSO recipeIng in recipe.Ingredients)
                {
                    bool ingFind = false;
                    for (int i = 0; i < copyIng.Count; i++)
                    {
                        ingFind = recipeIng.FoodName.Equals(copyIng[i].FoodName);
                        if (ingFind)
                        {
                            copyIng.RemoveAt(i);
                            break;
                        }
                    }
                    if (!ingFind)
                    {
                        recFind = false;
                        break;
                    }
                }
            }

            if (!recFind && recipe.Complements != null)
            {
                foreach (RecipeSO complement in recipe.Complements)
                {
                    if (complement.Ingredients.Count != ingredients.Count) continue;

                    copyIng.Clear();
                    copyIng.AddRange(ingredients);

                    recFind = true;

                    foreach (FoodSO compleIng in complement.Ingredients)
                    {
                        bool ingFind = false;
                        for (int i = 0; i < copyIng.Count; i++)
                        {
                            ingFind = compleIng.FoodName.Equals(copyIng[i].FoodName);
                            if (ingFind)
                            {
                                copyIng.RemoveAt(i);
                                break;
                            }
                        }
                        if (!ingFind)
                        {
                            recFind = false;
                            break;
                        }
                    }

                    if (recFind) break;
                }
            }   
            if(recFind) return recipe;

        }
        
        return null;
    }

    public FoodSO GetTrashFood()
    {
        return trashFood;
    }


    /* Debug */

    [ContextMenu("Debug/LogRandomRecipe")]
    public void LogRandomAssembleRecipe()
    {
        RecipeSO r = GiveRandomAssembleRecipe();
        string log = "How to make " + r.Result.FoodName + "? ";
        foreach(FoodSO f in r.Ingredients) log += f.FoodName + ", ";
        Debug.Log(log);
    }

    [SerializeField]
    List<FoodSO> debugRecipes;
    [ContextMenu("Debug/LogCook")]
    public void LogCookAssemble()
    {
        string log = "Ingredient: ";
        foreach(FoodSO f in debugRecipes) log += f.FoodName + ", ";
        Debug.Log(log);
        RecipeSO res = Cook(debugRecipes, RecipeType.Assemble);
        Debug.Log("Cook result: " + res.Result.FoodName);
    }
}
