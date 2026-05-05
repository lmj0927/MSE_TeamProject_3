using UnityEngine;
using System.Reflection;

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
            // Customer의 isWaiting 상태를 안전하게 가져옴
            FieldInfo waitingField = typeof(Customer).GetField("isWaiting", BindingFlags.NonPublic | BindingFlags.Instance);
            bool isWaiting = (bool)waitingField.GetValue(c);

            // 손님이 의자에 완전히 앉아서 밥을 기다리는 중일 때만 서빙!
            if (isWaiting)
            {
                // [1] 이 손님이 원하는 주문 이름(order)을 몰래 알아냅니다.
                FieldInfo orderField = typeof(Customer).GetField("order", BindingFlags.NonPublic | BindingFlags.Instance);
                string expectedOrder = (string)orderField.GetValue(c);

                // [2] 가짜 FoodSO(데이터) 생성
                FoodSO dummyData = ScriptableObject.CreateInstance<FoodSO>();

                // 💡 해결 1: 프로퍼티(FoodName)가 아닌 실제 멤버 변수(foodName)에 강제로 값을 주입합니다!
                FieldInfo nameField = typeof(FoodSO).GetField("foodName", BindingFlags.NonPublic | BindingFlags.Instance);
                if (nameField != null)
                {
                    nameField.SetValue(dummyData, isCorrect ? expectedOrder : "Trash_Burger");
                }

                // [3] 임시 큐브(음식) 생성 및 Food 부착
                GameObject foodObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                foodObj.name = isCorrect ? "Correct_Food" : "Wrong_Food";
                foodObj.GetComponent<MeshRenderer>().enabled = false;
                Food dummyFood = foodObj.AddComponent<Food>();

                // 💡 해결 2: Food 클래스에 존재하는 SetData 함수를 직접 호출하여 dummyData를 안전하게 넣어줍니다!
                // (FoodSO 코드에서 SetData를 사용하는 것을 참고함)
                MethodInfo setDataMethod = typeof(Food).GetMethod("SetData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (setDataMethod != null)
                {
                    setDataMethod.Invoke(dummyFood, new object[] { dummyData });
                }
                else
                {
                    Debug.LogWarning("Food 클래스에서 SetData 함수를 찾을 수 없습니다!");
                }

                // [4] 리플렉션으로 손님의 getFood(Food served) 강제 호출
                MethodInfo getFoodMethod = typeof(Customer).GetMethod("getFood", BindingFlags.NonPublic | BindingFlags.Instance);
                if (getFoodMethod != null)
                {
                    getFoodMethod.Invoke(c, new object[] { dummyFood });
                    servedCount++;
                }

                // [5] 테스트가 끝난 큐브와 메모리 즉시 정리
                Destroy(foodObj);
                Destroy(dummyData); // ScriptableObject 메모리 누수 방지
            }
        }

        if (servedCount > 0)
        {
            string color = isCorrect ? "green" : "red";
            string msg = isCorrect ? "정확한 주문" : "엉뚱한 쓰레기";
            Debug.Log($"<color={color}>[Test]</color> {servedCount}명의 대기 중인 손님에게 {msg}을 서빙했습니다.");
        }
        else
        {
            Debug.Log("<color=yellow>[Test]</color> 현재 밥을 기다리는(isWaiting) 손님이 없습니다.");
        }
    }

    private void PrintStatus()
    {
        Customer[] activeCustomers = FindObjectsOfType<Customer>();
        Debug.Log($"<color=cyan>[Status]</color> 씬에 활성화된 손님: {activeCustomers.Length}명");
    }
}