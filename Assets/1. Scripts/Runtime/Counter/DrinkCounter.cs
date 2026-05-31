// Owned by JunYoung Park
using System;
using System.Collections;
using Fusion;
using UnityEngine;

public class DrinkCounter : ACounter
{
    [SerializeField] private FoodSO[] drinks;
    [SerializeField] private ProgressBar progressBar;
    private StateChanger progressColor;

    [Header("Mini Game UI")]
    [SerializeField] private RectTransform range;
    [SerializeField] private float acceptableRatio = 0.1f;

    public Action OnDrinkFinished;

    private int selected = 0;
    [Networked] private bool isUsing { get; set; }
    [Networked] private PlayerController currentUser { get; set; }

    private float maxTimingRange;
    private float interactingTiming;
    [Networked] private float current { get; set; }
    private RecipeSO recipe;

    private Coroutine colorRoutine;

    public override void Spawned()
    {
        base.Spawned();

        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(false);
            progressBar.SetProgress(0f);
        }
        progressColor = progressBar != null ? progressBar.GetComponent<StateChanger>() : null;
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (!HasStateAuthority) return;
        if (!isUsing) return;

        current += Runner.DeltaTime;
        if (progressBar != null) progressBar.SetProgress(current / maxTimingRange);

        if (current >= maxTimingRange)
        {
            EndDispensing(false);
        }
    }

    public override void Interact(PlayerController player)
    {
        if(!player.HasStateAuthority) return;

        if (player.HasFood()) return;

        AuthorityHandler.RequestStateAuthority(
            onAuthorized: () =>
            {
                if (!isUsing)
                {
                    RPC_PlaySound();
                    StartDispensing(player);
                }
                else if (player == currentUser)
                {
                    float tolerance = maxTimingRange * acceptableRatio;
                    bool isSuccess = Mathf.Abs(current - interactingTiming) <= tolerance;

                    EndDispensing(isSuccess);
                }
            },
            onNotAuthorized: () =>
            {
                Debug.LogWarning("[DrinkCounter Interact] denied.");
            }
        );
    }

    private void StartDispensing(PlayerController player)
    {
        currentUser = player;
        current = 0f;

        currentUser.FreezeMovement(true);
        if (progressBar != null) progressBar.gameObject.SetActive(true);

        Place(drinks[selected], Vector3.zero, spawned =>
        {
            if (spawned == null)
            {
                if (currentUser != null) currentUser.FreezeMovement(false);
                if (progressBar != null) progressBar.gameObject.SetActive(false);
                currentUser = null;
                return;
            }

            recipe = RecipeManager.Instance.Cook(GetFoodSOs(), RecipeType.Beverage);
            maxTimingRange = recipe.Value;

            interactingTiming = UnityEngine.Random.Range(maxTimingRange * 0.1f, maxTimingRange * 0.9f);

            SetRangeUI();

            if (progressColor != null)
            {
                if (colorRoutine != null) StopCoroutine(colorRoutine);
                colorRoutine = StartCoroutine(ColorSequenceRoutine());
            }

            isUsing = true;
        });
    }

    private void EndDispensing(bool isSuccess)
    {
        RPC_StopSound();
        isUsing = false;
        if (currentUser != null) currentUser.FreezeMovement(false);
        if (progressBar != null) progressBar.gameObject.SetActive(false);

        if (colorRoutine != null)
        {
            StopCoroutine(colorRoutine);
            colorRoutine = null;
        }

        if (isSuccess && currentUser != null)
        {
            HandoffTo(currentUser, GetLastFood(), Vector3.zero, () => currentUser = null);
        }
        else
        {
            ClearAll(() => currentUser = null);
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlaySound()
    {
        SoundManager.Instance.DrinkStart(this);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StopSound()
    {
        OnDrinkFinished?.Invoke(); // fire on every client so each local SoundManager stops its drink audio
    }
}