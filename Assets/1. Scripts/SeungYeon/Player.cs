using UnityEngine;


[DisallowMultipleComponent]
public sealed class Player : MonoBehaviour
{
    [SerializeField] private GameObject hat;
    [SerializeField] private GameObject body;

    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerController controller;
    [SerializeField] private PlayerInteractInput interactInput;

    [SerializeField] private bool useLegacyInput = true;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode runKeyLeft = KeyCode.LeftShift;
    [SerializeField] private KeyCode runKeyRight = KeyCode.RightShift;

    [SerializeField] private bool isFrozen;

    private void Awake()
    {
        if (movement == null)
            movement = GetComponent<PlayerMovement>();
        if (controller == null)
            controller = GetComponent<PlayerController>();
        if (interactInput == null)
            interactInput = GetComponent<PlayerInteractInput>();

        if (movement != null)
            movement.SetUseInternalInput(false);
        if (interactInput != null)
            interactInput.SetUseInternalInput(false);
    }

    private void Update()
    {
        if (!useLegacyInput || isFrozen)
        {
            if (movement != null)
                movement.SetMoveInput(0f, 0f, false);
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool runHeld = Input.GetKey(runKeyLeft) || Input.GetKey(runKeyRight);

        if (movement != null)
            movement.SetMoveInput(h, v, runHeld);

        if (interactInput != null && Input.GetKeyDown(interactKey))
            interactInput.TryInteract();
    }

}

