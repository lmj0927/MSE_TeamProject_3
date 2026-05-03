using UnityEngine;
using System.Reflection;
using minjun;

public class TestBootstrap : MonoBehaviour
{
    void Update()
    {
        // 1. 정상 서빙 테스트 (T 키)
        if (Input.GetKeyDown(KeyCode.T))
        {
            ServeToAll(true);
        }

        // 2. 오답 서빙 테스트 (Y 키)
        if (Input.GetKeyDown(KeyCode.Y))
        {
            ServeToAll(false);
        }

        // 3. 상태 확인 (I 키)
        if (Input.GetKeyDown(KeyCode.I))
        {
            PrintStatus();
        }
    }

    private void ServeToAll(bool isCorrect)
    {
        Customer[] allCustomers = FindObjectsOfType<Customer>();
        int servedCount = 0;

        foreach (Customer c in allCustomers)
        {
            if (c.IsReady() && !c.HasEaten())
            {
                // [1] 테스트용 가짜 음식(큐브) 생성 및 Food 스크립트 부착
                GameObject orderObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                orderObj.name = "Temp_Order_Food";
                orderObj.GetComponent<MeshRenderer>().enabled = false; // 화면에 안 보이게 숨김
                Food orderFood = orderObj.AddComponent<Food>();

                // 손님에게 이 큐브를 주문 내역으로 강제 설정
                c.SetOrder(orderFood);

                Food serveFood;
                GameObject wrongObj = null;

                // [2] 정답/오답에 따라 서빙할 객체 결정
                if (isCorrect)
                {
                    // 정답: 방금 주문으로 넣었던 '바로 그 객체(참조 동일)'를 서빙
                    serveFood = orderFood;
                }
                else
                {
                    // 오답: 완전히 새로운 큐브를 생성해서 서빙 (참조 다름)
                    wrongObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wrongObj.name = "Temp_Wrong_Food";
                    wrongObj.GetComponent<MeshRenderer>().enabled = false;
                    serveFood = wrongObj.AddComponent<Food>();
                }

                // [3] Reflection으로 getFood 강제 호출
                MethodInfo getFoodMethod = typeof(Customer).GetMethod("getFood", BindingFlags.NonPublic | BindingFlags.Instance);
                if (getFoodMethod != null)
                {
                    getFoodMethod.Invoke(c, new object[] { serveFood });
                    servedCount++;
                }

                // [4] 메모리 누수 방지: 테스트용으로 만든 큐브 파괴
                // getFood 로직이 끝났으므로 파괴해도 무방합니다.
                Destroy(orderObj, 1.0f);
                if (wrongObj != null) Destroy(wrongObj, 1.0f);
            }
        }

        if (servedCount > 0)
        {
            string color = isCorrect ? "green" : "red";
            string msg = isCorrect ? "정상 음식(동일 큐브)" : "잘못된 음식(다른 큐브)";
            Debug.Log($"<color={color}>[Test]</color> {servedCount}명의 손님에게 {msg}을 서빙했습니다.");
        }
        else
        {
            Debug.Log("<color=yellow>[Test]</color> 현재 서빙 가능한 손님이 없습니다.");
        }
    }

    private void PrintStatus()
    {
        Customer[] activeCustomers = FindObjectsOfType<Customer>();
        int waiting = 0, eating = 0, leaving = 0;

        foreach (var c in activeCustomers)
        {
            if (!c.IsReady()) waiting++;
            else if (c.HasEaten()) leaving++;
            else eating++;
        }

        Debug.Log($"<color=cyan>[Status]</color> 씬에 활성화된 손님: {activeCustomers.Length}명 " +
                  $"(키오스크/주문대기: {waiting}, 식사/대기 중: {eating}, 퇴장 중: {leaving})");
    }
}