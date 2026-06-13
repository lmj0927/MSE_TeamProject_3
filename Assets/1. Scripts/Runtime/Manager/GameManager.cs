// Owned by JunYoung Park
using System;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

// Singleton manager for game states, network sync, stage progression, and API saving
public class GameManager : NetworkSingleton<GameManager>, ISceneLoadDone, IPlayerJoined
{
    public enum GameState
    {
        MainMenu,
        Loading,
        WaitSync,      //Wait for other players to finish loading
        Playing,
        EndPlay,
        Result
    }

    [Networked] private GameState state { get; set; }
    [SerializeField] private StageSO[] stages;

    public StageSO reading =>
        (stages != null && readingIdx >= 0 && readingIdx < stages.Length) ? stages[readingIdx] : null;

    // It helps the same stage moving across all players.
    [Networked, OnChangedRender(nameof(OnReadingIdxChanged))] private int readingIdx { get; set; }

    [Networked] private bool isPlaying { get; set; }

    public Action OnStageStart;
    public Action<int, int> OnPointUpdated;
    public Action OnStageEnd;
    public Action OnResult;
    private int task;

    [Networked] public float StageT { get; private set; } = 60f;
    [Networked] public float stageTimer { get; private set; } = 0f;

    [Networked] public int currentP { get; private set; } = 0;

    // Guard to trigger automatic EnterStage only once when all players join the lobby.
    [Networked] private bool stageAutoStarted { get; set; }

    private int pastP = 0;
    private SceneRef? inGameScene = null;
    private int appliedReadingIdx = -1;

    public override void Spawned()
    {
        base.Spawned();
        if (HasStateAuthority)
        {
            state = GameState.MainMenu;
            stageTimer = 0f;
            currentP = 0;
            isPlaying = false;
            readingIdx = -1;

            TryAutoStartStage();
        }
    }

    // Re-checks if the room is full whenever a new player joins the session.
    public void PlayerJoined(PlayerRef player)
    {
        TryAutoStartStage();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        switch (state)
        {
            case GameState.WaitSync:
                if (true) ChangeState(GameState.Playing);
                break;
            case GameState.Playing:
                stageTimer += Runner.DeltaTime;

                // End stage when timer runs out
                if (stageTimer >= StageT)
                {
                    stageTimer = 0f;
                    ChangeState(GameState.EndPlay);
                }

                if (pastP != currentP)
                {
                    pastP = currentP;
                    print(currentP);

                    int star = 0;
                    if (reading != null)
                    {
                        if (currentP >= reading.oneStarScore) star++; 
                        if (currentP >= reading.twoStarScore) star++;
                        if (currentP >= reading.threeStarScore) star++;
                    }

                    OnPointUpdated?.Invoke(currentP, star);
                }
                break;
            case GameState.MainMenu:
                TryAutoStartStage();
                break;
            default:
                break;
        }

    }

    private int GetSceneIndexByName(string sceneName)
    {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (path.Contains(sceneName + ".unity"))
            {
                return i;
            }
        }
        return -1;
    }

    // Handle game state transitions and trigger corresponding logic (Server-Side)
    public void ChangeState(GameState newState)
    {
        if (!HasStateAuthority) return; // run on first-created player.

        Debug.Log($"[GameManager ChangeState] {state} to {newState}");
        state = newState;

        switch (state)
        {
            case GameState.Loading:
                if (reading != null) StageT = reading.stageTimeLimit;

                if (HasStateAuthority)
                {
                    int buildIndex = GetSceneIndexByName(reading.sceneName);

                    if (buildIndex < 0)
                    {
                        Debug.LogError($"[GameManager] '{reading.sceneName}' Can't found! Please check whether the scene is registered in Build Settings.");
                        return;
                    }

                    inGameScene = SceneRef.FromIndex(buildIndex);
                    Runner.LoadScene(inGameScene.Value, LoadSceneMode.Single); // move!
                }
                break;

            case GameState.Playing:
                RPC_StartStage();
                break;

            case GameState.EndPlay:
                OnStageEnd?.Invoke();
                RPC_SaveGameProgress();
                RPC_ReturnToLobby();
                break;

            case GameState.Result:
                RPC_SaveGameProgress();
                OnResult?.Invoke();
                break;

            case GameState.MainMenu:
                SoundManager.Instance.ChangeBGM(0);

                isPlaying = false;
                stageTimer = 0f;
                currentP = 0;
                task = 0;
                readingIdx = -1;
                stageAutoStarted = false;   // Allow auto-entry for next round upon returning to lobby
                break;

            default:
                break;

        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    // Start stage.
    // The boolean parameter is considered for manual UI button usage.
    public void RPC_StartStage(bool immediate = false)
    {
        if (immediate)
        {
            isPlaying = true;
            OnStageStart?.Invoke();
            Debug.Log("[GameManager] OnStageStart Called.");
        }
        else StartCoroutine(EnterRoutine());
    }

    private IEnumerator EnterRoutine()
    {
        yield return new WaitForSeconds(0.1f);

        if (state == GameState.Playing && !isPlaying)
        {
            isPlaying = true;
            OnStageStart?.Invoke();
            Debug.Log("[GameManager] OnStageStart Called.");
        }
    }

    public void RegisterTask()
    {
        task++;
    }

    public void CompleteTask()
    {
        if (task <= 0) return;

        task--;

        if (task == 0)
        {
            ChangeState(GameState.Result);
        }
    }

    public void AddPoint(int p)
    {
        currentP += p;
    }

    private string lastAutoGate;
    private void LogAutoGate(string gate)
    {
        if (gate == lastAutoGate) return;
        lastAutoGate = gate;
        Debug.Log($"[GameManager AutoStart] blocked at: {gate}");
    }

    // Automatically enter the selected stage when all roster members join the Photon session in the lobby.
    private void TryAutoStartStage()
    {
        if (!HasStateAuthority) { LogAutoGate("not master (no state authority)"); return; }
        if (stageAutoStarted) return;
        if (state != GameState.MainMenu) { LogAutoGate($"state={state} (not MainMenu)"); return; }
        if (!RoomSession.HasRoom) { LogAutoGate("RoomSession empty on master"); return; }
        if (stages == null || stages.Length == 0) { LogAutoGate("stages array empty"); return; }

        int expected = RoomSession.CurrentRoom.participantUserIds?.Length ?? 1;
        if (expected < 1) expected = 1;

        int active = Runner.ActivePlayers.Count();
        if (active < expected) { LogAutoGate($"active={active} < expected={expected} session='{RoomSession.RoomId}'"); return; }

        stageAutoStarted = true;
        int idx = Mathf.Clamp(RoomSession.CurrentRoom.stage - 1, 0, stages.Length - 1);
        Debug.Log($"[GameManager] All {expected} player(s) joined → auto EnterStage({idx}) for room stage {RoomSession.CurrentRoom.stage}");
        EnterStage(idx);
    }

    public void EnterStage(int idx)
    {
        if (!HasStateAuthority)
        {
            Debug.LogWarning("[GameManager EnterStage] You have no authority to enter stage");
            return;
        }
        Debug.Log($"[GameManager EnterStage] stage {readingIdx} to {idx}");
        readingIdx = idx;
        ChangeState(GameState.Loading);
        ApplyRecipeData();
    }

    // when the valid reading idx(stage idx) is set, fill the recipe data of the stage.
    public void OnReadingIdxChanged()
    {
        Debug.Log($"[GameManager OnReadingIdxChanged] called with readingIdx {readingIdx}");
        if (readingIdx < 0) return;
        ApplyRecipeData();
    }

    // Inject stage-specific recipe data into RecipeManager
    private void ApplyRecipeData()
    {
        var r = reading;
        if (r == null || RecipeManager.Instance == null) return;
        RecipeManager.Instance.SetData(
            (r.availableIngredients ?? new FoodSO[0]).ToList(),
            (r.availableAssemble ?? new RecipeSO[0]).ToList(),
            (r.availableSide ?? new RecipeSO[0]).ToList(),
            (r.availableBeverage ?? new RecipeSO[0]).ToList());
        appliedReadingIdx = readingIdx;
    }

    public override void Render()
    {
        if (readingIdx >= 0 && readingIdx != appliedReadingIdx)
            ApplyRecipeData();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SaveGameProgress()
    {
        SaveGameProgressFlow().Forget();
    }

    // Async task using UniTask to save the player's stage score to the backend server via API
    private async UniTaskVoid SaveGameProgressFlow()
    {
        var network = NetworkManager.Instance;
        if (network == null || !network.HasAccessToken)
        {
            Debug.LogWarning("[GameManager] Skip gameProgress save: not logged in.");
            return;
        }

        int stageNumber = ResolveStageNumberForSave();
        if (stageNumber < 1)
        {
            Debug.LogWarning("[GameManager] Skip gameProgress save: invalid stage number.");
            return;
        }

        int score = currentP;
        var result = await network.UpdateStageBestScoreAsync(stageNumber, score);
        if (result.Ok)
            Debug.Log($"[GameManager] gameProgress saved stage={stageNumber} score={score}");
        else
            Debug.LogWarning(
                $"[GameManager] gameProgress save failed ({result.StatusCode} {result.ErrorCode}): {result.ErrorMessage}");
    }

    private int ResolveStageNumberForSave()
    {
        if (RoomSession.HasRoom && RoomSession.CurrentRoom.stage >= 1)
            return RoomSession.CurrentRoom.stage;
        if (readingIdx >= 0)
            return readingIdx + 1;
        return 0;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ReturnToLobby()
    {
        GameLauncher.ReturnToLobby();
    }

    public void LeaveStage()
    {
        ChangeState(GameState.MainMenu);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Inspector Debug: Immediately ends the current stage with a 1-star score, triggers progress save, and returns to lobby.
    /// </summary>
    public void DebugForceEndStageWithOneStar()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[GameManager] Debug cheat is only available in Play mode.");
            return;
        }

        if (!HasStateAuthority)
        {
            Debug.LogWarning("[GameManager] Debug cheat requires State Authority (host).");
            return;
        }

        if (state != GameState.Playing && state != GameState.WaitSync)
        {
            Debug.LogWarning($"[GameManager] Debug cheat requires Playing/WaitSync. Current state={state}");
            return;
        }

        var targetScore = reading != null && reading.oneStarScore > 0 ? reading.oneStarScore : 1;
        currentP = Mathf.Max(currentP, targetScore);
        pastP = currentP - 1;
        stageTimer = 0f;

        Debug.Log(
            $"[GameManager] Debug cheat → EndPlay with score={currentP} (1★ target={targetScore}) stage={ResolveStageNumberForSave()}");
        ChangeState(GameState.EndPlay);
    }
#endif

    // Callback invoked when the target scene finishes loading via Photon Fusion
    public void SceneLoadDone(in SceneLoadDoneArgs sceneInfo)
    {
        ApplyRecipeData();
        if (inGameScene.HasValue && sceneInfo.SceneRef == inGameScene.Value)
        {
            Debug.Log("[GameManager SceneLoadDone] Scene Loaded?");
            ChangeState(GameState.WaitSync);
        }
    }
}