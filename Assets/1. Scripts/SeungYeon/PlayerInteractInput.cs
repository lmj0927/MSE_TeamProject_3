using UnityEngine;

public class PlayerInteractInput : MonoBehaviour
{
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private void Update()
    {
        if (Input.GetKeyDown(interactKey))
            Interact();
    }

    // 추후: 주변 오브젝트와 상호작용 로직 연결
    public void Interact()
    {
    }
}

