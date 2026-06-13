// Owned by MinJun Lee
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;
using DG.Tweening;
using Fusion;

/// <summary>
/// Scrollable popup for selecting refrigerator ingredients.
/// </summary>
public class IngredientPopupUI : BasePopupUI
{
    [SerializeField] private List<FoodSO> ingredients; // available ingredients

    [Header("UI References")]
    [SerializeField] private ScrollRect scrollRect; // ingredient scroll view
    [SerializeField] private IngredientButton buttonPrefab; // button template
    private List<IngredientButton> ingredientButtons = new List<IngredientButton>(); // spawned buttons

    public Action<FoodSO> OnIngredientSelected; // selection callback

    private GameObject lastSelected; // last focused button
    private bool initialized = false; // build done flag

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (EventSystem.current == null) return;

        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        if (currentSelected != null && currentSelected != lastSelected)
        {
            lastSelected = currentSelected;

            // scroll list when focus moves to ingredient button
            if (lastSelected.GetComponent<IngredientButton>() != null)
            {
                SnapTo(lastSelected.GetComponent<RectTransform>());
            }
        }
        else if (currentSelected == null && lastSelected != null)
        {
            // restore focus if selection is lost
            EventSystem.current.SetSelectedGameObject(lastSelected);
        }
    }

    protected override void OnShow()
    {
        if(!initialized) Initialize();
        if (lastSelected != null && EventSystem.current.currentSelectedGameObject == null)
        {
            EventSystem.current.SetSelectedGameObject(lastSelected);
        }
    }

    
    // build ingredient buttons from recipe manager
    public void Initialize()
    {
        foreach (var btn in ingredientButtons)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        ingredientButtons.Clear();
        ingredients = RecipeManager.Instance.Ingredients;

        for (int i = 0; i < ingredients.Count; i++)
        {
            IngredientButton newBtn = Instantiate(buttonPrefab, scrollRect.content);
            newBtn.Initialize(ingredients[i], OnIngredientButtonClick);

            ingredientButtons.Add(newBtn);
        }

        // wire up/down navigation in a vertical loop
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

        ResetTop(); 

        if(ingredientButtons.Count > 0) {
            initialized = true;
            Debug.Log("[IngredientPopupUI Initialize] Initialized.");
        } else
        {
            Debug.LogWarning("[IngredientPopupUI Initialize] Fail to initialized refrigerator.");
        }
    }

    private void OnIngredientButtonClick(FoodSO food)
    {
        // var instantiatedFood = food.CreateFood();
        OnIngredientSelected?.Invoke(food);
        Hide();
    }

    // scroll to top and select first button
    protected override void ResetTop()
    {
        if (scrollRect != null && scrollRect.content != null)
        {
            Canvas.ForceUpdateCanvases();

            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);

            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f;

            // second pass after layout settles
            DOVirtual.DelayedCall(0.01f, () =>
            {
                if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
            });
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

    // tween scroll to selected item
    private void SnapTo(RectTransform target)
    {
        if (scrollRect == null || scrollRect.content == null) return;

        int index = target.GetSiblingIndex();
        int total = scrollRect.content.childCount;

        if (total > 1)
        {
            // top = 1, bottom = 0 in vertical scroll
            float targetNormalizedPos = 1f - ((float)index / (total - 1));

            DOTween.Kill("PopupScroll");

            DOTween.To(() => scrollRect.verticalNormalizedPosition,
                       x => scrollRect.verticalNormalizedPosition = x,
                       targetNormalizedPos,
                       0.2f)
                   .SetEase(Ease.OutQuad)
                   .SetId("PopupScroll");
        }
    }
}
