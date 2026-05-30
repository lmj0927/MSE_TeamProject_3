// Owned by SeungYeon Jung
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    float hAxis;
    float vAxis;
    [SerializeField] private float speed = 3;
    [SerializeField] private float runMultiplier = 1.8f;
    [SerializeField] private float turnSpeed = 15f;

    private float gravity = -9.81f;
    [SerializeField] private PlayerController controller;
    Vector3 moveVec;
    Vector3 velocity;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string isMovingParam = "isMoving";
    [SerializeField] private string isRunningParam = "isRunning";
    [SerializeField] private string isCarryingParam = "isCarrying";

    [Header("Stamina")]
    [SerializeField] private Stamina stamina;
    [SerializeField] private GameObject staminaUI; // ⭐ 추가: 스태미나 게이지 UI 오브젝트

    CharacterController playerController;
    float cachedSpeed;
    bool isFreezing = false;

    private void Start()
    {
        playerController = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (stamina == null)
            stamina = GetComponent<Stamina>();
    }

    private void Update()
    {
        // ⭐ 추가: UI 표시 로직을 위해 wantsRun 변수를 밖으로 빼냈습니다.
        bool wantsRun = false;

        if (!isFreezing)
        {
            hAxis = Input.GetAxisRaw("Horizontal");
            vAxis = Input.GetAxisRaw("Vertical");

            moveVec = new Vector3(hAxis, 0, vAxis).normalized;

            bool isMoving = moveVec.sqrMagnitude > 0.0001f;
            wantsRun = isMoving && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

            bool canRun = wantsRun;
            if (wantsRun && stamina != null)
            {
                canRun = stamina.TryDrainForRunning(Time.deltaTime);
            }

            bool isCarrying = gameObject.GetComponent<PlayerController>().HasFood();

            if (!wantsRun && stamina != null)
            {
                float weight = 1f;
                if (isMoving) weight = 0.8f;

                stamina.RegenWhileIdle(Time.deltaTime * weight);
            }

            bool isRunning = canRun;
            cachedSpeed = speed * (isRunning ? runMultiplier : 1f);

            if (animator != null)
            {
                animator.SetBool(isCarryingParam, isCarrying);
                animator.SetBool(isMovingParam, isMoving);
                animator.SetBool(isRunningParam, isRunning);
            }

            if (isMoving)
            {
                var targetRot = Quaternion.LookRotation(moveVec, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }

            Vector3 moveVelocity = moveVec * cachedSpeed;

            if (playerController.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            velocity.y += gravity * Time.deltaTime;

            playerController.Move((moveVelocity + velocity) * Time.deltaTime);
        }
        else
        {
            if (stamina != null)
                stamina.RegenWhileIdle(Time.deltaTime);

            if (animator != null)
            {
                animator.SetBool(isMovingParam, false);
                animator.SetBool(isRunningParam, false);
            }

            if (playerController.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            velocity.y += gravity * Time.deltaTime;
            playerController.Move(velocity * Time.deltaTime);
        }

        // ⭐ 추가: UI 표시 제어 로직 (Update의 마지막에 처리)
        if (stamina != null && staminaUI != null)
        {
            // 스태미나가 꽉 차 있지 않으면 회복 중인 상태입니다.
            bool isRecovering = stamina.Current < stamina.Max;

            // 달리기 시도 중이거나, 회복 중일 때만 UI를 띄웁니다.
            bool shouldShowUI = wantsRun || isRecovering;

            // 매 프레임 SetActive가 불리는 걸 막기 위해 상태가 다를 때만 호출합니다.
            if (staminaUI.activeSelf != shouldShowUI)
            {
                staminaUI.SetActive(shouldShowUI);
            }
        }
    }

    public void SetInteracting(bool flag)
    {
        isFreezing = flag;
    }
}