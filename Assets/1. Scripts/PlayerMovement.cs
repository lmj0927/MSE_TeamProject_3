using AYellowpaper.SerializedCollections.Editor.Data;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerMovement : MonoBehaviour, IMovable
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float gravity = -30f;
    [SerializeField] bool useCameraRelative = true;
    [SerializeField] Transform cameraTransform;
    [SerializeField] bool rotateTowardMove = true;
    [SerializeField] float rotateSpeed = 720f;

    CharacterController _characterController;
    float _verticalVelocity;

    void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        if (useCameraRelative && cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        Vector2 input = ReadMoveInput();
        Vector3 dir = ToWorldDirection(input);
        Move(dir, moveSpeed, Time.deltaTime);

        if (rotateTowardMove && dir.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotateSpeed * Time.deltaTime);
        }
    }

    Vector2 ReadMoveInput()
    {
        Vector2 fromGamepad = Gamepad.current != null ? Gamepad.current.leftStick.ReadValue() : Vector2.zero;
        if (fromGamepad.sqrMagnitude > 0.01f)
            return fromGamepad;

        var kb = Keyboard.current;
        if (kb == null)
            return Vector2.zero;

        float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
        float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
                - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
        return new Vector2(x, y);
    }

    Vector3 ToWorldDirection(Vector2 input)
    {
        if (input.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        input = Vector2.ClampMagnitude(input, 1f);

        if (useCameraRelative && cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = cameraTransform.right;
            right.y = 0f;
            right.Normalize();
            return (right * input.x + forward * input.y).normalized;
        }

        return new Vector3(input.x, 0f, input.y).normalized;
    }

    public void Move(Vector3 worldDirection, float speed, float deltaTime)
    {
        Vector3 horizontalDelta = worldDirection * (speed * deltaTime);

        if (_characterController != null)
        {
            if (_characterController.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;

            _verticalVelocity += gravity * deltaTime;
            Vector3 motion = new Vector3(horizontalDelta.x, _verticalVelocity * deltaTime, horizontalDelta.z);
            _characterController.Move(motion);
        }
        else
            transform.position += horizontalDelta;
    }

    public void Dash(Vector3 worldDirection, float speed, float deltaTime)
    {
        Vector3 horizontalDelta = worldDirection * (speed * deltaTime);

        if (_characterController != null)
        {
            if (_characterController.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;

            _verticalVelocity += gravity * deltaTime;

        }
        else
            transform.position += horizontalDelta; 
    }
    
}
