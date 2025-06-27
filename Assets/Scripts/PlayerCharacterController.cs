using UnityEngine;
using UnityEngine.InputSystem;  // підключаємо нову Input System

[RequireComponent(typeof(CharacterController))]
public class SimplePlayerController : MonoBehaviour
{
    [Header("References")]
    public Camera PlayerCamera;

    [Header("Movement Settings")]
    public float WalkSpeed = 5f;
    public float SprintMultiplier = 2f;
    public float JumpForce = 9f;
    public float Gravity = 20f;
    public float RotationSpeed = 200f;

    [Header("Ground Check")]
    public LayerMask GroundCheckLayers = -1;
    public float GroundCheckDistance = 0.05f;

    CharacterController controller;
    Vector3 velocity;
    bool isGrounded;
    float verticalAngle;

    // Нові поля для зберігання інпуту
    private Vector2 moveInput = Vector2.zero;
    private Vector2 lookInput = Vector2.zero;
    private bool jumpPressed = false;
    private bool sprintPressed = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleCameraLook();
        GroundCheck();
        HandleMovement();
    }

    void GroundCheck()
    {
        Vector3 spherePos = transform.position + Vector3.down * 0.1f;
        float sphereRadius = 0.3f;
        isGrounded = Physics.CheckSphere(spherePos, sphereRadius, GroundCheckLayers);
    }


    void HandleCameraLook()
    {
        float mouseX = lookInput.x * RotationSpeed * Time.deltaTime;
        float mouseY = lookInput.y * RotationSpeed * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);
        verticalAngle -= mouseY;
        verticalAngle = Mathf.Clamp(verticalAngle, -89f, 89f);

        PlayerCamera.transform.localEulerAngles = new Vector3(verticalAngle, 0, 0);
    }

    void HandleMovement()
    {
        Vector3 input = new Vector3(moveInput.x, 0, moveInput.y);
        Vector3 move = transform.TransformDirection(input.normalized);

        float speed = WalkSpeed;
        if (sprintPressed)
        {
            speed *= SprintMultiplier;
        }

        controller.Move(move * speed * Time.deltaTime);

        if (isGrounded && jumpPressed)
        {
            velocity.y = JumpForce;
            jumpPressed = false; // скидаємо стан джамп
        }

        velocity.y -= Gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // Ці методи будуть викликані PlayerInput або вручну через Input Action Events:
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
            jumpPressed = true;
            Debug.Log("Jump pressed!");
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        sprintPressed = context.ReadValueAsButton();
    }
}
