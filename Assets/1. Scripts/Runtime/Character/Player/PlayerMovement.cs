// Owned by SeungYeon Jung
using Fusion;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    float hAxis;
    float vAxis;
    [SerializeField] private float speed = 3;
    [SerializeField] private float runMultiplier = 1.8f;
    [SerializeField] private float turnSpeed = 15f;

    private float gravity = -9.81f;
    // [SerializeField] private PlayerController controller;
    Vector3 moveVec;
    Vector3 velocity;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string isMovingParam = "isMoving";
    [SerializeField] private string isRunningParam = "isRunning";
    [SerializeField] private string isCarryingParam = "isCarrying";
    
    // Networked: synchronized by photon fusion
    // OnchangedRender(nameof(function)): callback when the property value changed
    [Networked, OnChangedRender(nameof(IsMovingChanged))] private bool isMoving { get; set; }
    [Networked, OnChangedRender(nameof(IsRunningChanged))] private bool isRunning { get; set; }
    [Networked, OnChangedRender(nameof(IsCarryingChanged))] private bool isCarrying { get; set; }

    [Header("Stamina")]
    [SerializeField] private Stamina stamina;
    [SerializeField] private GameObject staminaUI; // ⭐ 추가: 스태미나 게이지 UI 오브젝트

    CharacterController playerController;
    float cachedSpeed;
    [Networked] bool isFreezing { get; set; }
    [Networked] bool shouldShowUI { get; set; }

    // Tick-level update of the position.
    Vector3 _lastTickPos;
    bool _predictionApplied;

    public Camera Camera;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            Debug.Log("Player spawned");
            Camera = Camera.main;
            Debug.Log("Camera " + ((Camera == null) ? "not found" : "found"));
            Camera.GetComponent<FollowCamera>().target = transform;


            playerController = GetComponent<CharacterController>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            if (stamina == null)
                stamina = GetComponent<Stamina>();

            isFreezing = false;
            shouldShowUI = false;
        }
    }

    public override void FixedUpdateNetwork()
    {

        if (HasStateAuthority == false)
        {
            return;
        }

        // tick-unit force update
        if (_predictionApplied)
        {
            playerController.enabled = false;
            transform.position = _lastTickPos;
            playerController.enabled = true;
            _predictionApplied = false;
        }

        // ⭐ 추가: UI 표시 로직을 위해 wantsRun 변수를 밖으로 빼냈습니다.
        bool wantsRun = false;

        if (!isFreezing)
        {
            hAxis = Input.GetAxisRaw("Horizontal");
            vAxis = Input.GetAxisRaw("Vertical");

            moveVec = new Vector3(hAxis, 0, vAxis).normalized;

            // networked property
            isMoving = moveVec.sqrMagnitude > 0.0001f;
            wantsRun = isMoving && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

            bool canRun = wantsRun;
            if (wantsRun && stamina != null)
            {
                canRun = stamina.TryDrainForRunning(Runner.DeltaTime);
            }

            // networked property
            isCarrying = gameObject.GetComponent<PlayerController>().HasFood();

            if (!wantsRun && stamina != null)
            {
                float weight = 1f;
                if (isMoving) weight = 0.8f;

                stamina.RegenWhileIdle(Runner.DeltaTime * weight);
            }

            // networked property
            isRunning = canRun;
            cachedSpeed = speed * (isRunning ? runMultiplier : 1f);

            // no need to change here because it will be managed via callbacks
            // if (animator != null)
            // {
            //     animator.SetBool(isCarryingParam, isCarrying);
            //     animator.SetBool(isMovingParam, isMoving);
            //     animator.SetBool(isRunningParam, isRunning);
            // }

            if (isMoving)
            {
                var targetRot = Quaternion.LookRotation(moveVec, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Runner.DeltaTime);
            }

            Vector3 moveVelocity = moveVec * cachedSpeed;

            if (playerController.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            velocity.y += gravity * Runner.DeltaTime;

            playerController.Move((moveVelocity + velocity) * Runner.DeltaTime);
        }
        else
        {
            if (stamina != null)
                stamina.RegenWhileIdle(Runner.DeltaTime);
            
            isMoving = false;
            isRunning = false;

            if (playerController.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            velocity.y += gravity * Runner.DeltaTime;
            playerController.Move(velocity * Runner.DeltaTime);
        }

        // ⭐ 추가: UI 표시 제어 로직 (Update의 마지막에 처리)
        if (stamina != null && staminaUI != null)
        {
            // 스태미나가 꽉 차 있지 않으면 회복 중인 상태입니다.
            bool isRecovering = stamina.Current < stamina.Max;

            // 달리기 시도 중이거나, 회복 중일 때만 UI를 띄웁니다.
            shouldShowUI = wantsRun || isRecovering;
        }

        // Snapshot the position
        _lastTickPos = transform.position;
    }

    public override void Render()
    {
        base.Render();

        if (HasStateAuthority && !isFreezing && playerController != null)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 dir = new Vector3(h, 0, v).normalized;
            if (dir.sqrMagnitude > 0.0001f)
            {
                playerController.Move(dir * cachedSpeed * Time.deltaTime);
                _predictionApplied = true;
            }
        }

        // 매 프레임 SetActive가 불리는 걸 막기 위해 상태가 다를 때만 호출합니다.
        if (staminaUI.activeSelf != shouldShowUI)
        {
            staminaUI.SetActive(shouldShowUI);
        }
    }

    void IsMovingChanged()
    {
        animator.SetBool(isMovingParam, isMoving);
    }
    void IsRunningChanged()
    {
        animator.SetBool(isRunningParam, isRunning);
    }
    void IsCarryingChanged()
    {
        animator.SetBool(isCarryingParam, isCarrying);
    }


    public void SetInteracting(bool flag)
    {
        isFreezing = flag;
    }
}