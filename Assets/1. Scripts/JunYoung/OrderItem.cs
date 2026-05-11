using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class OrderItem : MonoBehaviour
{
    [System.Serializable]
    public struct IngredientSlot
    {
        public GameObject root;  
        public Image icon;   
        public TextMeshProUGUI text; 
    }

    [SerializeField] private Image customerPortrait;
    [SerializeField] private Image menuImage;
    [SerializeField] private IngredientSlot[] slots;

    public void SetOrder(Sprite portrait, RecipeSO recipe)
    {
        if (customerPortrait == null) { Debug.LogError($"{gameObject.name}의 customerPortrait가 비어있습니다!"); return; }
        if (recipe == null) { Debug.LogError("넘어온 RecipeSO 데이터가 null입니다!"); return; }
        if (recipe.Result == null) { Debug.LogError($"{recipe.name} 레시피의 Result 데이터가 비어있습니다!"); return; }
        if (recipe.Ingredients == null) { Debug.LogError($"{recipe.name} 레시피의 Ingredients 리스트가 null입니다!"); return; }

        customerPortrait.sprite = portrait;
        menuImage.sprite = recipe.Result.Sprite;

        var grouped = recipe.Ingredients
            .GroupBy(i => i)
            .Select(g => new { Food = g.Key, Count = g.Count() })
            .ToList();

        for (int i = 0; i < slots.Length; i++)
        {
            if (i < grouped.Count)
            {
                slots[i].root.SetActive(true);
                slots[i].icon.sprite = grouped[i].Food.Sprite;

                if (grouped[i].Count > 1)
                {
                    slots[i].text.gameObject.SetActive(true);
                    slots[i].text.text = $"x{grouped[i].Count}";
                }
                else
                {
                    slots[i].text.gameObject.SetActive(false);
                }
            }
            else
            {
                slots[i].root.SetActive(false);
            }
        }
    }
}