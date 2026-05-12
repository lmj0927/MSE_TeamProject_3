using UnityEngine;

public class DrinkCounter : ACounter
{
    /// <summary>
    /// It can have multiple drinks, but now it have only coke
    /// </summary>
    [SerializeField] private FoodSO[] drinks;
    [SerializeField] private ProgressBar progressBar;
    private StateChanger progressColor;

    [Header("Mini Game UI")]
    [SerializeField] private RectTransform range;
    [SerializeField] private float acceptableRatio = 0.1f; 

    private int selected = 0;
    private bool isUsing;
    private PlayerController currentUser;

    private float maxTimingRange;
    private float interactingTiming;
    private float current = 0f; 
    private RecipeSO recipe;

    private void Awake()
    {
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(false);
            progressBar.SetProgress(0f);
            progressColor = progressBar.GetComponent<StateChanger>();
        }
    }

    private void Update()
    {
        if (isUsing)
        {
            current += Time.deltaTime;
            progressBar.SetProgress(current / maxTimingRange);

            if (progressColor != null)
            {
                float tolerance = maxTimingRange * acceptableRatio;
                float distance = Mathf.Abs(current - interactingTiming);

                if (distance <= tolerance)
                {
                    progressColor.SetColorState(0);
                }
                else if (distance <= tolerance * 2f)
                {
                    progressColor.SetColorState(1);
                }
                else
                {
                    progressColor.SetColorState(2);
                }
            }

            if (current >= maxTimingRange)
            {
                EndDispensing(false);
            }
        }
    }

    public override void Interact(PlayerController player)
    {
        if (player.HasFood()) return;

        if (!isUsing)
        {
            StartDispensing(player);
        }
        else if (player == currentUser)
        {
            float tolerance = maxTimingRange * acceptableRatio;
            bool isSuccess = Mathf.Abs(current - interactingTiming) <= tolerance;

            EndDispensing(isSuccess);
        }
    }

    private void StartDispensing(PlayerController player)
    {
        currentUser = player;
        isUsing = true;
        current = 0f;

        currentUser.FreezeMovement(true);
        progressBar.gameObject.SetActive(true); 

        AddFood(drinks[selected].CreateFood());

        recipe = RecipeManager.Instance.Cook(GetFoodSOs(), RecipeType.Beverage);
        maxTimingRange = recipe.Value;

        interactingTiming = Random.Range(maxTimingRange * 0.1f, maxTimingRange * 0.9f);

        SetRangeUI();
    }

    private void EndDispensing(bool isSuccess)
    {
        isUsing = false;
        currentUser.FreezeMovement(false);
        progressBar.gameObject.SetActive(false); 

        if (isSuccess)
        {
            currentUser.AddFood(RemoveFood());
        }
        else
        {
            ClearFood();
        }
    }

    private void SetRangeUI()
    {
        if (range == null || progressBar == null) return;

        RectTransform pbRect = progressBar.GetComponent<RectTransform>();
        float totalWidth = pbRect.rect.width;

        float toleranceWidth = totalWidth * (acceptableRatio * 2f);
        range.sizeDelta = new Vector2(toleranceWidth, range.sizeDelta.y);

        float ratio = interactingTiming / maxTimingRange;

        float targetPosX = ratio * totalWidth;
        range.anchoredPosition = new Vector2(targetPosX, range.anchoredPosition.y);
    }
}