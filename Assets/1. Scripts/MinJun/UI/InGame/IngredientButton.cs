using UnityEngine;
using UnityEngine.UI;
using System;

public class IngredientButton : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private Button button;

    public void Initialize(FoodSO foodSO, Action<FoodSO> onClick)
    {
        image.sprite = foodSO.Sprite;
        button.onClick.AddListener(() => onClick?.Invoke(foodSO));
    }
}
