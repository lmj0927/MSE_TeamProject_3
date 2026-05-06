using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    float hAxis;
    float vAxis;
    bool runHeld;

    [SerializeField] private bool useInternalInput = true;
    [SerializeField] private float speed = 3f;
    [SerializeField] private float runMultiplier = 1.8f;
    [SerializeField] private float turnSpeed = 15f;
    [SerializeField] private float gravity = -9.81f;

    Vector3 moveVec;
    Vector3 velocity;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string isMovingParam = "isMoving";
    [SerializeField] private string isRunningParam = "isRunning";
    [SerializeField] private string isCarryingParam = "isCarrying";
    bool hasCarryingParam;

    [Header("Stamina")]
    [SerializeField] private Stamina stamina;

    CharacterController controller;
    PlayerController playerController;
    float cachedSpeed;
    bool isInteracting;

    public void SetUseInternalInput(bool enabled) => useInternalInput = enabled;

    public void SetMoveInput(float horizontal, float vertical, bool runHeld)
    {
        hAxis = horizontal;
        vAxis = vertical;
        this.runHeld = runHeld;
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        controller.skinWidth = Mathf.Min(controller.skinWidth, 0.03f);
        controller.stepOffset = 0f;

        if (Mathf.Abs(controller.center.y) < 0.0001f)
            controller.center = new Vector3(controller.center.x, controller.height * 0.5f, controller.center.z);

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (stamina == null)
            stamina = GetComponent<Stamina>();

        playerController = GetComponent<PlayerController>();

        if (animator != null)
        {
            foreach (var p in animator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Bool && p.name == isCarryingParam)
                {
                    hasCarryingParam = true;
                    break;
                }
            }
        }
    }

    private void Update()
    {
        if (useInternalInput && !isInteracting)
        {
            hAxis = Input.GetAxisRaw("Horizontal");
            vAxis = Input.GetAxisRaw("Vertical");
            runHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        moveVec = new Vector3(hAxis, 0, vAxis).normalized;
        if (isInteracting)
        {
            moveVec = Vector3.zero;
            runHeld = false;
        }

        bool isMoving = moveVec.sqrMagnitude > 0.0001f;
        bool wantsRun = isMoving && runHeld;

        bool canRun = wantsRun;
        if (wantsRun && stamina != null)
            canRun = stamina.TryDrainForRunning(Time.deltaTime);

        bool isCarrying = playerController != null && playerController.HasFood();

        if (!wantsRun && stamina != null)
        {
            float weight = 1f;
            if (isMoving) weight = 0.4f;
            if (isCarrying) weight *= 0.85f;
            stamina.RegenWhileIdle(Time.deltaTime * weight);
        }

        bool isRunning = canRun;
        cachedSpeed = speed * (isRunning ? runMultiplier : 1f);

        if (animator != null)
        {
            if (hasCarryingParam)
                animator.SetBool(isCarryingParam, isCarrying);
            animator.SetBool(isMovingParam, isMoving);
            animator.SetBool(isRunningParam, isRunning);
        }

        if (moveVec.sqrMagnitude > 0.0001f)
        {
            var targetRot = Quaternion.LookRotation(moveVec, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        var moveVelocity = moveVec * cachedSpeed;

        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move((moveVelocity + velocity) * Time.deltaTime);
    }

    public void SetInteracting(bool flag)
    {
        isInteracting = flag;
    }
}
