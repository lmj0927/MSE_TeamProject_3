using System;
using System.Collections;
using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkSingleton<GameManager>, ISceneLoadDone, IPlayerJoined
{
    public enum GameState {
        MainMenu,
        Loading,
        WaitSync,       // 다른 플레이어 로딩 대기
        Playing,
        EndPlay,
        Result
    }

    [Networked] private GameState state { get; set; }
    [SerializeField] private StageSO[] stages;
    public StageSO reading =>
        (stages != null && readingIdx >= 0 && readingIdx < stages.Length) ? stages[readingIdx] : null;

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

    // 로비(Multi Main Test)에서 전원 합류 시 1회만 자동 EnterStage 하기 위한 가드.
    [Networked] private bool stageAutoStarted { get; set; }

    public bool IsBusy => throw new NotImplementedException();

    public Scene MainRunnerScene => throw new NotImplementedException();

    private int pastP = 0;

    private SceneRef? inGameScene = null;

    private int appliedReadingIdx = -1;

    public override void Spawned()
    {
        base.Spawned();
        if(HasStateAuthority)
        {
            state = GameState.MainMenu;
            stageTimer = 0f;
            currentP = 0;
            isPlaying = false;
            readingIdx = -1;

            // 마스터 스폰 시점에 이미 들어와 있는 인원(또는 솔로) 즉시 체크.
            TryAutoStartStage();
        }
    }

    // 플레이어가 세션에 합류할 때마다 마스터가 정원 충족 여부를 재확인.
    public void PlayerJoined(PlayerRef player)
    {
        TryAutoStartStage();
    }
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        switch(state)
        {
            case GameState.WaitSync:
                if (true) ChangeState(GameState.Playing);               // 모든 플레이어 로딩완료 체크하는 것으로 수정 예정
                break;
            case GameState.Playing:
                stageTimer += Runner.DeltaTime;

                if (stageTimer >= StageT)
                {
                    stageTimer = 0f;
                    // 현재 점수 서버에 저장 예정
                    ChangeState(GameState.EndPlay);
                }

                if (pastP != currentP)
                {
                    pastP = currentP;
                    print(currentP);

                    int star = 0;
                    if (reading != null)
                    {
                        if (currentP >= reading.oneStarScore) star++; // 첫번째 별 표시 예정
                        if (currentP >= reading.twoStarScore) star++; // 두번째 별 표시 예정
                        if (currentP >= reading.threeStarScore) star++; // 세번째 별 표시 예정
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

    public void ChangeState(GameState newState)
    {
        if(!HasStateAuthority) return;

        Debug.Log($"[GameManager ChangeState] {state} to {newState}");
        state = newState;

        switch(state)
        {
            case GameState.Loading:
                // RPC_ModifyRecipeManager();
                if (reading != null) StageT = reading.stageTimeLimit;


                // UnityEngine.Events.UnityAction<Scene, LoadSceneMode> OnLoad = null;
                // OnLoad = (scene, mode) =>
                // {
                //     ChangeState(GameState.WaitSync);

                //     SceneManager.sceneLoaded -= OnLoad;
                // };

                // SceneManager.sceneLoaded += OnLoad;

                // SceneManager.LoadScene(reading.sceneName);
                if (HasStateAuthority)
                {
                    int buildIndex = GetSceneIndexByName(reading.sceneName);

                    if (buildIndex < 0)
                    {
                        Debug.LogError($"[GameManager] '{reading.sceneName}' Can't found! Please check whether the scene is registered in Build Settings.");
                        return;
                    }

                    inGameScene = SceneRef.FromIndex(buildIndex);
                    Runner.LoadScene(inGameScene.Value, LoadSceneMode.Single);
                }
                break;

            case GameState.Playing:
                RPC_StartStage();
                break;

            case GameState.EndPlay:
                OnStageEnd?.Invoke();
                RPC_ReturnToLobby();
                break;

            case GameState.Result:
                // 결과창 띄우기 예정
                break;

            case GameState.MainMenu:
                SoundManager.Instance.ChangeBGM(0);

                isPlaying = false;
                stageTimer = 0f;
                currentP = 0;
                task = 0;
                readingIdx = -1;
                stageAutoStarted = false;   // 로비 복귀 시 다음 라운드 자동 진입 허용
                break;

            default:
                break;

        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StartStage(bool immediate = false)      // 스테이지 시작, bool은 버튼사용을 고려한 parameter
    {
        if (immediate)
        {
            isPlaying = true;
            OnStageStart?.Invoke();
            Debug.Log("[GameManager] OnStageStart Called.");
        }
        else StartCoroutine(EnterRoutine());
    }

    IEnumerator EnterRoutine()
    {
        yield return new WaitForSeconds(0.1f);        // 게임 시작 아이콘? 

        if (state == GameState.Playing && !isPlaying)
        {
            isPlaying = true;
            OnStageStart?.Invoke();
            Debug.Log("[GameManager] OnStageStart Called.");
        }
    }

    // [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    // private void RPC_ModifyRecipeManager()  
    // {
    //     if (reading == null) return;
    //     RecipeManager.Instance.SetData(reading.availableIngredients.ToList(), reading.availableAssemble.ToList());
    // }

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

    // 로비에서 roster 인원이 모두 Photon 세션에 합류하면, 방 생성 시 정해진 stage로 자동 진입.
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

    public void EnterStage(int idx)         // 버튼에서 구독할 함수
    {
        if(!HasStateAuthority)
        {
            Debug.LogWarning("[GameManager EnterStage] You have no authority to enter stage");
            return;
        }
        Debug.Log($"[GameManager EnterStage] stage {readingIdx} to {idx}");
        readingIdx = idx;
        ChangeState(GameState.Loading);
        ApplyRecipeData();
    }

    public void OnReadingIdxChanged()
    {
        Debug.Log($"[GameManager OnReadingIdxChanged] called with readingIdx {readingIdx}");
        if(readingIdx < 0) return;
        ApplyRecipeData();
    }

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
    private void RPC_ReturnToLobby()
    {
        GameLauncher.ReturnToLobby();
    }

    public void LeaveStage()               // 버튼에서 구독할 함수
    {
        ChangeState(GameState.MainMenu);
    }

    public void SceneLoadDone(in SceneLoadDoneArgs sceneInfo)
    {
        ApplyRecipeData();
        if(inGameScene.HasValue && sceneInfo.SceneRef == inGameScene.Value)
        {
            Debug.Log("[GameManager SceneLoadDone] Scene Loaded?");
            ChangeState(GameState.WaitSync);
        }
    }
}
