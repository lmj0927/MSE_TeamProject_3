// Owned by JunYoung Park
using System.Collections;
using UnityEngine;

public class DrinkCounter : ACounter
{
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

    private Coroutine colorRoutine;

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

        AddFood(foodSpawner.SpawnFood(drinks[selected]));

        recipe = RecipeManager.Instance.Cook(GetFoodSOs(), RecipeType.Beverage);
        maxTimingRange = recipe.Value;

        interactingTiming = Random.Range(maxTimingRange * 0.1f, maxTimingRange * 0.9f);

        SetRangeUI();

        if (progressColor != null)
        {
            if (colorRoutine != null) StopCoroutine(colorRoutine);
            colorRoutine = StartCoroutine(ColorSequenceRoutine());
        }
    }

    private void EndDispensing(bool isSuccess)
    {
        isUsing = false;
        currentUser.FreezeMovement(false);
        progressBar.gameObject.SetActive(false);

        if (colorRoutine != null)
        {
            StopCoroutine(colorRoutine);
            colorRoutine = null;
        }

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

    private IEnumerator ColorSequenceRoutine()
    {
        float tolerance = maxTimingRange * acceptableRatio;
        float b1 = interactingTiming - tolerance * 3f;
        float b2 = interactingTiming - tolerance;
        float b3 = interactingTiming + tolerance;
        float b4 = interactingTiming + tolerance * 3f;

        if (b2 <= 0f) progressColor.SetColorState(0, 0f);
        else if (b1 <= 0f) progressColor.SetColorState(1, 0f);
        else progressColor.SetColorState(2, 0f);

        if (b1 > 0f)
        {
            progressColor.SetColorState(1, b1);
            yield return new WaitForSeconds(b1);
        }

        if (b2 > 0f)
        {
            float waitTime = b2 - Mathf.Max(0f, b1);
            progressColor.SetColorState(0, waitTime);
            yield return new WaitForSeconds(waitTime);
        }

        if (b3 > 0f)
        {
            float waitTime = b3 - Mathf.Max(0f, b2);
            progressColor.SetColorState(1, waitTime);
            yield return new WaitForSeconds(waitTime);
        }

        if (b4 > 0f)
        {
            float waitTime = b4 - Mathf.Max(0f, b3);
            progressColor.SetColorState(2, waitTime);
        }
    }
}