using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    public enum GameState { MainMenu, Loading, Playing, Result, Paused }

    private GameState state = GameState.MainMenu;
    [SerializeField] private StageSO[] stages;
    private StageSO reading;
    public bool stageStart { get; private set; }
    public bool isPaused { get; private set; }      // 설정창 등으로 인한 일시정지

    public Action OnStageStart;
    public Action OnStageEnd;                       //시간 종료로 인해 실행되야할것: 손님의 입장 중단, 있던 손님의 즉시 퇴장. 각각의 스크립트에서 별도 실행
    private int task;

    private float stageT;
    public float StageT => stageT;
    public float stageTimer { get; private set; } = 0f;    

    public int currentP { get; private set; } = 0;

    private void Update()
    {
        if (state == GameState.Playing)
        {
            stageTimer += Time.deltaTime;

            if (stageTimer >= stageT)
            {
                stageStart = false;
                stageTimer = 0f;
                // 현재 점수 서버에 저장 예정
                StageEnd();
            }

            if (currentP >= reading.oneStarScore) ; // 첫번째 별 표시 예정
            if (currentP >= reading.twoStarScore) ; // 두번째 별 표시 예정
            if (currentP >= reading.threeStarScore) ; // 세번째 별 표시 예정

        }
    }

    private void ModifyRecipeManager()  
    {
        RecipeManager.Instance.SetData(reading.availableIngredients.ToList(), reading.availableAssemble.ToList());
    }
    private void StageStart()
    {
        OnStageStart?.Invoke();
    }
    private void StageEnd()
    {
        OnStageEnd?.Invoke();
    }

    public void RegisterTask()
    {
        task++;
    }

    public void CompleteTask()
    {
        task--;

        if (task <= 0)
        {
            state = GameState.Result;
            //결과창 띄우기 예정
        }
    }

    public void AddPoint(int p)            
    {
        currentP += p;
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
    }

    public void EnterStage(int idx)         // 버튼에서 구독할 함수
    {
        state = GameState.Loading;
        reading = stages[idx];
        ModifyRecipeManager();
        stageT = reading.stageTimeLimit;

        SceneManager.LoadScene(reading.sceneName);
    }

    public void LeaveStage()               // 버튼에서 구독할 함수
    {
        state = GameState.Loading;
        stageTimer = 0f;
        currentP = 0;
        task = 0;
        stageStart = false;
    }
}
