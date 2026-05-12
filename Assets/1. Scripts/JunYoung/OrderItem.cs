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
    [SerializeField] private Image[] menuImages;
    [SerializeField] private IngredientSlot[] slots;
    private Customer current;
    [SerializeField] private StateChanger patienceColor;

    private void Update()
    {
        if (current.isBored && !current.isAngry) patienceColor.SetColorState(1);
        else if (current.isAngry) patienceColor.SetColorState(2);
    }
    public void SetOrder(Customer customer)
    {
        patienceColor.SetColorState(0);
        current = customer;
        customerPortrait.sprite = customer.portrait;
        menuImages[0].sprite = customer.mainOrder.Result.Sprite;

        SetMenuImg(customer.drinkOrder, 1);
        SetMenuImg(customer.sideOrder, 2);

        var grouped = customer.mainOrder.Ingredients
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

    private void SetMenuImg(RecipeSO r, int idx)
    {
        if (r == null)
        {
            menuImages[idx].gameObject.SetActive(false);
            return;
        }

        menuImages[idx].gameObject.SetActive(true);
        menuImages[idx].sprite = r.Result.Sprite;
    }
}