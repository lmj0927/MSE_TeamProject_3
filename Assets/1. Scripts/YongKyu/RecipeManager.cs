using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

public class RecipeManager : MonoBehaviour
{


    /* Singleton */
    private static RecipeManager _instance;
    public  static RecipeManager instance
    {
        get
        {
            if(_instance == null) _instance = new RecipeManager();
            return _instance;
        }
    }


    /* RecipeManager */

    [SerializeField]
    List<RecipeSO> assembleRecipes;
    [SerializeField]
    List<RecipeSO> fireRecipes;
    [SerializeField]
    List<RecipeSO> sliceRecipes;

    [SerializeField]
    FoodSO trashFood;

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
    public FoodSO Cook(List<FoodSO> ingredients, RecipeType type)
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
            bool recFind = true;
            foreach(FoodSO recipeIng in recipe.ingredients)
            {
                bool ingFind = false;
                foreach(FoodSO queryIng in ingredients)
                {
                    ingFind = recipeIng.foodName.Equals(queryIng.foodName);
                    if(ingFind) break;
                }
                if(!ingFind) {
                    recFind = false;
                    break;
                }
            }
            if(recFind) return recipe.result;
        }
        
        return trashFood;
    }




    /* Debug */

    [ContextMenu("Debug/LogRandomRecipe")]
    public void LogRandomAssembleRecipe()
    {
        RecipeSO r = GiveRandomAssembleRecipe();
        string log = "How to make " + r.result.foodName + "? ";
        foreach(FoodSO f in r.ingredients) log += f.foodName + ", ";
        Debug.Log(log);
    }

    [SerializeField]
    List<FoodSO> debugRecipes;
    [ContextMenu("Debug/LogCook")]
    public void LogCookAssemble()
    {
        string log = "Ingredient: ";
        foreach(FoodSO f in debugRecipes) log += f.foodName + ", ";
        Debug.Log(log);
        FoodSO res = Cook(debugRecipes, RecipeType.Assemble);
        Debug.Log("Cook result: " + res.foodName);
    }
}
