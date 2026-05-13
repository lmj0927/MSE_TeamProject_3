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
    [SerializeField] private PlayerController controller;
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

    CharacterController playerController;
    float cachedSpeed;
    bool isInteracting = false;

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
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority == false)
        {
            return;
        }
        if (!isInteracting)
        {
            hAxis = Input.GetAxisRaw("Horizontal");
            vAxis = Input.GetAxisRaw("Vertical");

            moveVec = new Vector3(hAxis, 0, vAxis).normalized;

            // networked property
            isMoving = moveVec.sqrMagnitude > 0.0001f;
            bool wantsRun = isMoving && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

            bool canRun = wantsRun;
            if (wantsRun && stamina != null)
                canRun = stamina.TryDrainForRunning(Runner.DeltaTime);

            // networked property
            isCarrying = gameObject.GetComponent<PlayerController>().HasFood();

            if (!wantsRun && stamina != null)
            {
                float weight = 1f;
                if (isMoving) weight = 0.4f;
                if (isCarrying) weight *= 0.85f;

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
            
            // no need to change here because it will be managed via callbacks
            // if (animator != null)
            // {
            //     animator.SetBool(isMovingParam, false);
            //     animator.SetBool(isRunningParam, false);
            // }

            if (playerController.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            velocity.y += gravity * Runner.DeltaTime;
            playerController.Move(velocity * Runner.DeltaTime);
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
        isInteracting = flag;
    }
}