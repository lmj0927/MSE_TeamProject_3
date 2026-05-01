using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    float hAxis;
    float vAxis;
    public float speed = 3;
    public float runMultiplier = 1.8f;
    public float turnSpeed = 15f;

    Vector3 moveVec;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string isMovingParam = "isMoving";
    [SerializeField] private string isRunningParam = "isRunning";

    [Header("Stamina")]
    [SerializeField] private Stamina stamina;

    Rigidbody rb;
    float cachedSpeed;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (stamina == null)
            stamina = GetComponent<Stamina>();
    }

    private void Update()
    {
        hAxis = Input.GetAxisRaw("Horizontal");
        vAxis = Input.GetAxisRaw("Vertical");

        moveVec = new Vector3(hAxis, 0, vAxis).normalized;

        bool isMoving = moveVec.sqrMagnitude > 0.0001f;
        bool wantsRun = isMoving && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

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
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        Vector3 v = rb.linearVelocity;
        v.x = moveVec.x * cachedSpeed;
        v.z = moveVec.z * cachedSpeed;
        rb.linearVelocity = v;

        if (moveVec.sqrMagnitude > 0.0001f)
        {
            var targetRot = Quaternion.LookRotation(moveVec, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime));
        }
    }
}
