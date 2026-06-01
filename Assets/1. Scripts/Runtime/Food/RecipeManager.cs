// Owned by YongKyu Lee
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using Unity.VisualScripting;
using System.Linq;

public class RecipeManager : Singleton<RecipeManager>
{

    /* RecipeManager */
    [SerializeField]
    List<RecipeSO> sideMenuRecipes;
    [SerializeField]
    List<RecipeSO> beverageRecipes;
    [SerializeField]
    List<RecipeSO> assembleRecipes;
    [SerializeField]
    List<RecipeSO> grillRecipes;
    [SerializeField]
    List<RecipeSO> sliceRecipes;
    [SerializeField]
    List<RecipeSO> oilRecipes;
    [SerializeField]
    List<FoodSO> ingredients;
    public List<FoodSO> Ingredients => ingredients;

    [SerializeField]
    FoodSO trashFood;

    private List<FoodSO> copyIng = new List<FoodSO>();


    private bool hasData = false;
    public bool HasData => hasData;

    public void SetData(List<FoodSO> ing, List<RecipeSO> assemble, List<RecipeSO> side, List<RecipeSO> beverage)
    {
        ingredients = ing;
        assembleRecipes = assemble;
        sideMenuRecipes = side;
        beverageRecipes = beverage;
        hasData = true;
        Debug.Log("[RecipeManager SetData] data filled");
    }
    /// <summary>
    /// Returns a randomly selected assemble/beverage/sidemenu recipe from the assembleRecipes list.
    /// If the list is null or empty, returns null.
    /// </summary>
    /// <returns>
    /// A randomly selected RecipeSO, or null if no recipe is available.
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

    public RecipeSO GiveRandomBeverageRecipe()
    {
        if (beverageRecipes == null || beverageRecipes.Count == 0)
        {
            Debug.LogError("beverageRecipe is not valid. Null or Empty.");
            return null;
        }
        return beverageRecipes[Random.Range(0, beverageRecipes.Count)];
    }

    public RecipeSO GiveRandomSideRecipe()
    {
        if (sideMenuRecipes == null || sideMenuRecipes.Count == 0)
        {
            Debug.LogError("sideMenuRecipe is not valid. Null or Empty.");
            return null;
        }
        return sideMenuRecipes[Random.Range(0, sideMenuRecipes.Count)];
    }

    // Index-based API for network sync (master picks idx, others look up locally)
    public int RandomAssembleIdx() => (assembleRecipes == null || assembleRecipes.Count == 0) ? -1 : Random.Range(0, assembleRecipes.Count);
    public int RandomBeverageIdx() => (beverageRecipes == null || beverageRecipes.Count == 0) ? -1 : Random.Range(0, beverageRecipes.Count);
    public int RandomSideIdx() => (sideMenuRecipes == null || sideMenuRecipes.Count == 0) ? -1 : Random.Range(0, sideMenuRecipes.Count);

    public RecipeSO GetAssemble(int idx) => (assembleRecipes == null || idx < 0 || idx >= assembleRecipes.Count) ? null : assembleRecipes[idx];
    public RecipeSO GetBeverage(int idx) => (beverageRecipes == null || idx < 0 || idx >= beverageRecipes.Count) ? null : beverageRecipes[idx];
    public RecipeSO GetSide(int idx) => (sideMenuRecipes == null || idx < 0 || idx >= sideMenuRecipes.Count) ? null : sideMenuRecipes[idx];

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
            case RecipeType.Side:
                recipes = sideMenuRecipes;
                break;
            case RecipeType.Beverage:
                recipes = beverageRecipes;
                break;
            case RecipeType.Assemble:
                recipes = assembleRecipes;
                break;
            case RecipeType.Grill:
                recipes = grillRecipes;
                break;
            case RecipeType.Slice:
                recipes = sliceRecipes;
                break;
            case RecipeType.Oil:
                recipes = oilRecipes;
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
