using UnityEngine;
using System.Collections.Generic;

public class RefrigeratorCounter : ACounter
{
    [SerializeField] private IngredientPopupUI ingredientPopupUI;
    private PlayerController interactPlayer;

    void Start()
    {
        ingredientPopupUI.OnIngredientSelected += OnIngredientSelected;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // TODO : player unfreeze
            ingredientPopupUI.Hide();
        }
    }

    public override void Interact(PlayerController player)
    {
        if (!player.HasFood())
        {
            interactPlayer = player;
            // TODO : player freeze
            ingredientPopupUI.Show();
        }
    }

    private void OnIngredientSelected(Food food)
    {
        if(food != null)
            interactPlayer.AddFood(food);
        // TODO : player unfreeze
    }
}
