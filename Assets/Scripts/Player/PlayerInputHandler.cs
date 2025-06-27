using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Tooltip("Sensitivity multiplier for moving the camera around")]
    public float LookSensitivity = 1f;

    [Tooltip("Additional sensitivity multiplier for WebGL")]
    public float WebglLookSensitivityMultiplier = 0.25f;

    [Tooltip("Used to flip the vertical input axis")]
    public bool InvertYAxis = false;

    [Tooltip("Used to flip the horizontal input axis")]
    public bool InvertXAxis = false;

    private InputAction m_MoveAction;
    private InputAction m_LookAction;
    private InputAction m_JumpAction;
    private InputAction m_CrouchAction;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        m_MoveAction = InputSystem.actions.FindAction("Player/Move");
        m_LookAction = InputSystem.actions.FindAction("Player/Look");
        m_JumpAction = InputSystem.actions.FindAction("Player/Jump");
        m_CrouchAction = InputSystem.actions.FindAction("Player/Crouch");

        m_MoveAction?.Enable();
        m_LookAction?.Enable();
        m_JumpAction?.Enable();
        m_CrouchAction?.Enable();
    }

    public Vector3 GetMoveInput()
    {
        var input = m_MoveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        return Vector3.ClampMagnitude(new Vector3(input.x, 0f, input.y), 1f);
    }

    public float GetLookInputsHorizontal()
    {
        float input = m_LookAction?.ReadValue<Vector2>().x ?? 0f;
        if (InvertXAxis) input *= -1;
        input *= LookSensitivity;

#if UNITY_WEBGL
        input *= WebglLookSensitivityMultiplier;
#endif

        return input;
    }

    public float GetLookInputsVertical()
    {
        float input = m_LookAction?.ReadValue<Vector2>().y ?? 0f;
        if (InvertYAxis) input *= -1;
        input *= LookSensitivity;

#if UNITY_WEBGL
        input *= WebglLookSensitivityMultiplier;
#endif

        return input;
    }

    public bool GetJumpInputDown()
    {
        return m_JumpAction != null && m_JumpAction.WasPressedThisFrame();
    }

    public bool GetJumpInputHeld()
    {
        return m_JumpAction != null && m_JumpAction.IsPressed();
    }

    public bool GetCrouchInputDown()
    {
        return m_CrouchAction != null && m_CrouchAction.WasPressedThisFrame();
    }

    public bool GetCrouchInputReleased()
    {
        return m_CrouchAction != null && m_CrouchAction.WasReleasedThisFrame();
    }
}
