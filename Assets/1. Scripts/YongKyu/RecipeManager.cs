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
    List<RecipeSO> recipes;

    public RecipeSO GiveRandomRecipe()
    {
        return recipes[Random.Range(0, recipes.Count)];
    }

    public FoodSO Cook(List<FoodSO> ingredients)
    {
        
        foreach(RecipeSO recipe in recipes)
        {
            bool find = false;
            foreach(FoodSO recipeIng in recipe.ingredients)
            {
                foreach(FoodSO queryIng in ingredients)
                {
                    find = recipeIng.foodName.Equals(queryIng.foodName);
                }
                if(!find) break;
            }
            if(find) return recipe.result;
        }
        
        return null;
    }




    /* Debug */

    [MenuItem("RecipeManager/Log")]
    static void LogRandomRecipe()
    {
        
    }
}
