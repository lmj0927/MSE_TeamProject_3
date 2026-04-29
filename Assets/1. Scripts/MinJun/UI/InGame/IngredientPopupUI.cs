using System.Collections.Generic;
using UnityEngine;
using minjun;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public class IngredientPopupUI : BasePopupUI
{
    [SerializeField] private List<Food> ingredients;
    [SerializeField] private List<IngredientButton> ingredientButtons;

    public Action<Food> OnIngredientSelected;

    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    protected override void OnShow()
    {
        EventSystem.current.SetSelectedGameObject(ingredientButtons[0].gameObject);
    }

    public void Initialize()
    {
        for (int i = 0; i < ingredientButtons.Count; i++)
        {
            ingredientButtons[i].Initialize(ingredients[i], OnIngredientButtonClick);
        }
    }

    private void OnIngredientButtonClick(Food food)
    {
        var instantiatedFood = Instantiate(food);
        OnIngredientSelected?.Invoke(instantiatedFood);
        Hide();
    }
}
