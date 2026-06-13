// Owned by MinJun Lee
using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Single ingredient button in the popup list.
/// </summary>
public class IngredientButton : MonoBehaviour
{
    [SerializeField] private Image image; // ingredient icon
    [SerializeField] private Button button; // click button

    public void Initialize(FoodSO foodSO, Action<FoodSO> onClick)
    {
        image.sprite = foodSO.Sprite;
        // pass selected FoodSO back to popup
        button.onClick.AddListener(() => onClick?.Invoke(foodSO));
    }
}
