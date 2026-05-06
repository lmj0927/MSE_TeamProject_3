using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public class IngredientPopupUI : BasePopupUI
{
    [SerializeField] private List<FoodSO> ingredients;
    [SerializeField] private List<IngredientButton> ingredientButtons;

    public Action<Food> OnIngredientSelected;

    private GameObject lastSelected;

    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    private void Update()
    {
        if (EventSystem.current == null) return;

        if (EventSystem.current.currentSelectedGameObject != null)
        {
            lastSelected = EventSystem.current.currentSelectedGameObject;
        }
        else if (lastSelected != null)
        {
            EventSystem.current.SetSelectedGameObject(lastSelected);
        }
    }

    protected override void OnShow()
    {
        if (ingredientButtons.Count > 0)
        {
            lastSelected = ingredientButtons[0].gameObject;
            EventSystem.current.SetSelectedGameObject(lastSelected);
        }
        
    }

    public void Initialize()
    {
        for (int i = 0; i < ingredientButtons.Count; i++)
        {
            ingredientButtons[i].Initialize(ingredients[i], OnIngredientButtonClick);
        }
    }

    private void OnIngredientButtonClick(FoodSO food)
    {
        var instantiatedFood = food.CreateFood();
        OnIngredientSelected?.Invoke(instantiatedFood);
        Hide();
    }
}
