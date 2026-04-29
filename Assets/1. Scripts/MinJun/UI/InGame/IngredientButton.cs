using UnityEngine;
using UnityEngine.UI;
using System;
using minjun;
public class IngredientButton : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private Button button;

    public void Initialize(Food food, Action<Food> onClick)
    {
        // TODO : Set food sprite
        button.onClick.AddListener(() => onClick?.Invoke(food));
    }
}
