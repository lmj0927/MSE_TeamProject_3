using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    float hAxis;
    float vAxis;
    public float speed = 3;
    public float runMultiplier = 1.8f;
    public float turnSpeed = 15f;

    public float gravity = -9.81f;

    Vector3 moveVec;
    Vector3 velocity;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string isMovingParam = "isMoving";
    [SerializeField] private string isRunningParam = "isRunning";
    [SerializeField] private string isCarryingParam = "isCarrying";

    [Header("Stamina")]
    [SerializeField] private Stamina stamina;

    CharacterController controller;
    float cachedSpeed;
    bool isInteracting = false;

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (stamina == null)
            stamina = GetComponent<Stamina>();
    }

    private void Update()
    {
        if (!isInteracting)
        {
            hAxis = Input.GetAxisRaw("Horizontal");
            vAxis = Input.GetAxisRaw("Vertical");

            moveVec = new Vector3(hAxis, 0, vAxis).normalized;

            bool isMoving = moveVec.sqrMagnitude > 0.0001f;
            bool wantsRun = isMoving && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

            bool canRun = wantsRun;
            if (wantsRun && stamina != null)
                canRun = stamina.TryDrainForRunning(Time.deltaTime);

            bool isCarrying = gameObject.GetComponent<PlayerController>().HasFood();

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

            if (controller.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            velocity.y += gravity * Time.deltaTime;

            controller.Move((moveVelocity + velocity) * Time.deltaTime);
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

            if (controller.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }
    }

    public void IsInteracting(bool flag)
    {
        isInteracting = flag;
    }
}