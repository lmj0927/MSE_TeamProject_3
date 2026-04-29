using UnityEngine;

public class TestBootstrap : MonoBehaviour
{
    [Header("테스트 컨트롤 설정")]
    [Tooltip("서빙할 음식의 종류 (0: 정상, 1: 잘못된 음식 등)")]
    [SerializeField] private int foodToServe = 0;

    [Header("참조 객체")]
    [Tooltip("씬에 있는 CustomerManager를 연결하세요.")]
    [SerializeField] private CustomerManager manager;

    void Update()
    {
        // 1. 모든 의자에 앉아있는 손님에게 서빙 (T 키)
        if (Input.GetKeyDown(KeyCode.T))
        {
            ServeToAllSittingCustomers();
        }
        // 2. 잘못된 음식 서빙 테스트 (Y 키)
        if (Input.GetKeyDown(KeyCode.Y))
        {
            ServeWrongFoodToAllSittingCustomers();
        }
        // 3. 현재 씬의 손님 상태 요약 로그 출력 (I 키)
        if (Input.GetKeyDown(KeyCode.I))
        {
            PrintCustomerStatus();
        }
    }

    /// <summary>
    /// 현재 의자에 앉아 있는(Sitting) 모든 손님에게 설정된 음식을 서빙합니다.
    /// </summary>
    private void ServeToAllSittingCustomers()
    {
        Customer[] allCustomers = FindObjectsOfType<Customer>();
        int servedCount = 0;

        foreach (Customer c in allCustomers)
        {
            // 아직 먹지 않았고(hasEaten == false), 대기 중이거나 앉아있는 상태라면
            // (IsReady()는 주문 완료 여부이므로, 앉아 있다면 무조건 true인 상태일 것입니다.)
            if (c.IsReady() && !c.HasEaten())
            {
                // Reflection이나 Manager의 public 함수 없이, 
                // SendMessage를 통해 바로 getFood를 호출합니다.
                c.SendMessage("getFood", foodToServe, SendMessageOptions.DontRequireReceiver);
                servedCount++;
            }
        }

        if (servedCount > 0)
        {
            Debug.Log($"<color=green>[Test]</color> {servedCount}명의 손님에게 {foodToServe}번 음식을 정상 서빙했습니다.");
        }
        else
        {
            Debug.Log("<color=yellow>[Test]</color> 서빙할 수 있는 손님(앉아서 기다리는 중)이 없습니다.");
        }
    }

    /// <summary>
    /// 강제로 잘못된 음식(foodToServe와 다른 값)을 서빙하여 불만족 퇴장을 유도합니다.
    /// </summary>
    private void ServeWrongFoodToAllSittingCustomers()
    {
        Customer[] allCustomers = FindObjectsOfType<Customer>();
        int servedCount = 0;
        int wrongFood = foodToServe + 1; // 무조건 틀린 음식 번호 생성

        foreach (Customer c in allCustomers)
        {
            if (c.IsReady() && !c.HasEaten())
            {
                c.SendMessage("getFood", wrongFood, SendMessageOptions.DontRequireReceiver);
                servedCount++;
            }
        }

        if (servedCount > 0)
        {
            Debug.Log($"<color=red>[Test]</color> {servedCount}명의 손님에게 잘못된 음식({wrongFood})을 서빙하여 쫓아냅니다.");
        }
    }

    /// <summary>
    /// 현재 씬에 활성화된 손님들의 상태를 파악하여 콘솔에 출력합니다.
    /// 풀링 시스템이 정상 작동하는지 확인하는 데 유용합니다.
    /// </summary>
    private void PrintCustomerStatus()
    {
        Customer[] activeCustomers = FindObjectsOfType<Customer>();
        int waitingAtKiosk = 0;
        int eating = 0;
        int leaving = 0;

        foreach (var c in activeCustomers)
        {
            if (!c.IsReady()) waitingAtKiosk++;
            else if (c.HasEaten()) leaving++;
            else eating++;
        }

        Debug.Log($"<color=cyan>[Status]</color> 씬에 활성화된 손님: {activeCustomers.Length}명 " +
                  $"(키오스크: {waitingAtKiosk}, 식사/대기 중: {eating}, 퇴장 중: {leaving})");
    }
}