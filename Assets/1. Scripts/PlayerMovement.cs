using System.Collections;
using System.Collections.Generic;
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

    Rigidbody2D rigid;

    private void Start()
    {
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
        float currentSpeed = speed * (isRunning ? runMultiplier : 1f);

        if (animator != null)
        {
            animator.SetBool(isMovingParam, isMoving);
            animator.SetBool(isRunningParam, isRunning);
        }

        transform.position += moveVec * currentSpeed * Time.deltaTime;

        if (isMoving)
        {
            var targetRot = Quaternion.LookRotation(moveVec, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }
    }
}