using UnityEngine;

public class TestPhotoStudio : MonoBehaviour
{
    void Update()
    {
        // 게임 실행 중 P 키를 누르면 사진이 찍힙니다.
        if (Input.GetKeyDown(KeyCode.P))
        {
            // 씬에 돌아다니는 첫 번째 손님을 무작정 찾습니다.
            Customer activeCustomer = FindObjectOfType<Customer>();

            if (activeCustomer != null)
            {
                // 찾은 손님의 멱살을 잡고 사진을 찍습니다! (4번째 매개변수 true = 파일 저장)
                Sprite testSprite = PreviewGenerator.TakeLiveSnapshot(activeCustomer.gameObject, 256, 256, true);

                // 테스트용이니 메모리는 날려버립니다. (파일은 하드디스크에 남아있음)
                if (testSprite != null)
                {
                    Destroy(testSprite.texture);
                    Destroy(testSprite);
                }
            }
            else
            {
                Debug.LogWarning("현재 씬에 스폰된 손님이 없습니다! 손님이 온 뒤에 P 키를 눌러주세요.");
            }
        }
    }
}