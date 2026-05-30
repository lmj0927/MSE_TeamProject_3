using System;
using System.Collections;
using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkSingleton<GameManager>, ISceneLoadDone
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
    public StageSO reading { get; private set; }
    
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

    public bool IsBusy => throw new NotImplementedException();

    public Scene MainRunnerScene => throw new NotImplementedException();

    private int pastP = 0;

    private SceneRef? inGameScene = null;

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
        }
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

                    //OnPointUpdated?.Invoke(currentP, star);
                }
                break;
            default:
                break;
        }
        
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
                if(HasStateAuthority)
                {
                    inGameScene = SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath("Assets/3. Scenes/YongKyu/" + reading.sceneName + ".unity"));
                    Runner.LoadScene(inGameScene.Value, LoadSceneMode.Single);
                }
                break;

            case GameState.Playing:
                RPC_StartStage();
                break;

            case GameState.EndPlay:
                OnStageEnd?.Invoke();
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
                reading = null;
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

    public void EnterStage(int idx)         // 버튼에서 구독할 함수
    {
        if(!HasStateAuthority)
        {
            Debug.LogWarning("[GameManager EnterStage] You have no authority to enter stage");
            return;
        }
        Debug.Log($"[GameManager EnterStage] stage {readingIdx} to {idx}");
        readingIdx = idx;
    }

    public void OnReadingIdxChanged()
    {
        Debug.Log($"[GameManager OnReadingIdxChanged] called with readingIdx {readingIdx}");
        if(readingIdx < 0) return;
        reading = stages[readingIdx];
        RecipeManager.Instance.SetData(reading.availableIngredients.ToList(), reading.availableAssemble.ToList());
        ChangeState(GameState.Loading);
    }

    public void LeaveStage()               // 버튼에서 구독할 함수
    {
        ChangeState(GameState.MainMenu);
    }

    public void SceneLoadDone(in SceneLoadDoneArgs sceneInfo)
    {
        if(inGameScene.HasValue && sceneInfo.SceneRef == inGameScene.Value)
        {
            Debug.Log("[GameManager SceneLoadDone] Scene Loaded?");
            ChangeState(GameState.WaitSync);
        }
    }
}
