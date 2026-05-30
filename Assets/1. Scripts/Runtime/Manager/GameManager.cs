using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    public enum GameState {
        MainMenu,
        Loading,       
        WaitSync,       // 다른 플레이어 로딩 대기
        Playing,
        EndPlay,
        Result
    }

    private GameState state = GameState.MainMenu;
    [SerializeField] private StageSO[] stages;
    private StageSO reading;
    public StageSO Reading => reading;

    private bool isPlaying = false;

    public Action OnStageStart;
    public Action<int, int> OnPointUpdated;
    public Action OnStageEnd;                   
    public Action OnResult;
    private int task;

    private float stageT = 60f;
    public float StageT => stageT;
    public float stageTimer { get; private set; } = 0f;    

    public int currentP { get; private set; } = 0;
    private int pastP = 0;       

    private void Start()
    {
    }
    private void Update()
    {
        switch(state)
        {
            case GameState.WaitSync:
                if (true) ChangeState(GameState.Playing);               // 모든 플레이어 로딩완료 체크하는 것으로 수정 예정
                break;
            case GameState.Playing:
                stageTimer += Time.deltaTime;

                if (stageTimer >= stageT)
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
        state = newState;

        switch(state)
        {
            case GameState.Loading:
                ModifyRecipeManager();
                if (reading != null) stageT = reading.stageTimeLimit;


                UnityEngine.Events.UnityAction<Scene, LoadSceneMode> OnLoad = null;
                OnLoad = (scene, mode) =>
                {
                    ChangeState(GameState.WaitSync);

                    SceneManager.sceneLoaded -= OnLoad;
                };

                SceneManager.sceneLoaded += OnLoad;

                SceneManager.LoadScene(reading.sceneName);
                break;

            case GameState.Playing:
                StartStage();
                break;

            case GameState.EndPlay:
                OnStageEnd?.Invoke();
                break;

            case GameState.Result:
                // 결과창 띄우기 예정
                break;

            case GameState.MainMenu:
                SoundManager.Instance.ChangeBGM(1);

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

    public void StartStage(bool immediate = false)      // 스테이지 시작, bool은 버튼사용을 고려한 parameter
    {
        if (immediate)
        {
            isPlaying = true;
            OnStageStart?.Invoke();
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
        }
    }

    private void ModifyRecipeManager()  
    {
        if (reading == null) return;
        RecipeManager.Instance.SetData(reading.availableIngredients.ToList(), reading.availableAssemble.ToList());
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

    public void EnterStage(int idx)         // 버튼에서 구독할 함수
    {
        reading = stages[idx];
        ChangeState(GameState.Loading);
    }

    public void LeaveStage()               // 버튼에서 구독할 함수
    {
        ChangeState(GameState.MainMenu);
    }
}
