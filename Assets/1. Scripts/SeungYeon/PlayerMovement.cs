using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    float hAxis;
    float vAxis;
    bool runHeld;
    [SerializeField] private bool useInternalInput = true;
    public float speed = 3;
    public float runMultiplier = 1.8f;
    public float turnSpeed = 15f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundedStickVelocity = -2f;

    Vector3 moveVec;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string isMovingParam = "isMoving";
    [SerializeField] private string isRunningParam = "isRunning";

    [Header("Stamina")]
    [SerializeField] private Stamina stamina;

    CharacterController cc;
    float yVelocity;
    float cachedSpeed;

    public void SetUseInternalInput(bool enabled) => useInternalInput = enabled;

    public void SetMoveInput(float horizontal, float vertical, bool runHeld)
    {
        hAxis = horizontal;
        vAxis = vertical;
        this.runHeld = runHeld;
    }

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        cc.skinWidth = Mathf.Min(cc.skinWidth, 0.03f);
        cc.stepOffset = 0f;

        if (Mathf.Abs(cc.center.y) < 0.0001f)
            cc.center = new Vector3(cc.center.x, cc.height * 0.5f, cc.center.z);

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (stamina == null)
            stamina = GetComponent<Stamina>();
    }

    private void Update()
    {
        if (useInternalInput)
        {
            hAxis = Input.GetAxisRaw("Horizontal");
            vAxis = Input.GetAxisRaw("Vertical");
            runHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        moveVec = new Vector3(hAxis, 0, vAxis).normalized;

        bool isMoving = moveVec.sqrMagnitude > 0.0001f;
        bool wantsRun = isMoving && runHeld;

        bool canRun = wantsRun;
        if (wantsRun && stamina != null)
            canRun = stamina.TryDrainForRunning(Time.deltaTime);

        if (!isMoving && stamina != null)
            stamina.RegenWhileIdle(Time.deltaTime);

        bool isRunning = canRun;
        cachedSpeed = speed * (isRunning ? runMultiplier : 1f);

        if (animator != null)
        {
            animator.SetBool(isMovingParam, isMoving);
            animator.SetBool(isRunningParam, isRunning);
        }

        if (cc == null)
            return;

        if (cc.isGrounded)
            yVelocity = groundedStickVelocity;
        else
            yVelocity += gravity * Time.deltaTime;

        var move = moveVec * cachedSpeed;
        move.y = yVelocity;
        cc.Move(move * Time.deltaTime);

        if (moveVec.sqrMagnitude > 0.0001f)
        {
            var targetRot = Quaternion.LookRotation(moveVec, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }
    }
}
