// Owned by MinJun Lee
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;
using DG.Tweening;

public class IngredientPopupUI : BasePopupUI
{
    [SerializeField] private List<FoodSO> ingredients;

    [Header("UI References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private IngredientButton buttonPrefab;
    private List<IngredientButton> ingredientButtons = new List<IngredientButton>();

    public Action<FoodSO> OnIngredientSelected;

    private GameObject lastSelected;

    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    private void Update()
    {
        if (EventSystem.current == null) return;

        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        if (currentSelected != null && currentSelected != lastSelected)
        {
            lastSelected = currentSelected;

            if (lastSelected.GetComponent<IngredientButton>() != null)
            {
                SnapTo(lastSelected.GetComponent<RectTransform>());
            }
        }
        else if (currentSelected == null && lastSelected != null)
        {
            EventSystem.current.SetSelectedGameObject(lastSelected);
        }
    }

    protected override void ResetTop()
    {
        if (scrollRect != null && scrollRect.content != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());

            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f;
        }

        if (ingredientButtons.Count > 0)
        {
            lastSelected = ingredientButtons[0].gameObject;

            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(lastSelected);

            Button firstBtn = ingredientButtons[0].GetComponent<Button>();
            if (firstBtn != null)
            {
                firstBtn.Select();
            }
        }
    }

    protected override void OnShow()
    {
        if (lastSelected != null && EventSystem.current.currentSelectedGameObject == null)
        {
            EventSystem.current.SetSelectedGameObject(lastSelected);
        }
    }

    public void Initialize()
    {
        foreach (var btn in ingredientButtons)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        ingredientButtons.Clear();

        for (int i = 0; i < ingredients.Count; i++)
        {
            IngredientButton newBtn = Instantiate(buttonPrefab, scrollRect.content);
            newBtn.Initialize(ingredients[i], OnIngredientButtonClick);

            ingredientButtons.Add(newBtn);
        }

        for (int i = 0; i < ingredientButtons.Count; i++)
        {
            Button btn = ingredientButtons[i].GetComponent<Button>();
            if (btn != null)
            {
                Navigation nav = new Navigation { mode = Navigation.Mode.Explicit };

                int prevIndex = (i == 0) ? ingredientButtons.Count - 1 : i - 1;
                int nextIndex = (i == ingredientButtons.Count - 1) ? 0 : i + 1;

                nav.selectOnUp = ingredientButtons[prevIndex].GetComponent<Button>();
                nav.selectOnDown = ingredientButtons[nextIndex].GetComponent<Button>();

                btn.navigation = nav;
            }
        }
    }

    private void OnIngredientButtonClick(FoodSO food)
    {
        // var instantiatedFood = food.CreateFood();
        OnIngredientSelected?.Invoke(food);
        Hide();
    }

    private void SnapTo(RectTransform target)
    {
        if (scrollRect == null || scrollRect.content == null) return;

        int index = target.GetSiblingIndex();
        int total = scrollRect.content.childCount;

        if (total > 1)
        {
            float targetNormalizedPos = 1f - ((float)index / (total - 1));

            DOTween.To(() => scrollRect.verticalNormalizedPosition,
                       x => scrollRect.verticalNormalizedPosition = x,
                       targetNormalizedPos,
                       0.2f).SetEase(Ease.OutQuad);
        }
    }
}