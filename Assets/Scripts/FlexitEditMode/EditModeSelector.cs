using UnityEngine.InputSystem;

using UnityEngine;

public class EditModeSelector : MonoBehaviour
{
    [SerializeField] private PlayerVision vision;
    [SerializeField] private InputActionReference enterEditmodeAction1;
    [SerializeField] private InputActionReference enterEditmodeActionMouse;

    

    private void OnEnable()
    {
        enterEditmodeAction1.action.Enable();
        enterEditmodeActionMouse.action.Enable();
        enterEditmodeActionMouse.action.performed += OnRightClick;
    }

    private void OnDisable()
    {
        enterEditmodeActionMouse.action.performed -= OnRightClick;
        enterEditmodeAction1.action.Disable();
        enterEditmodeActionMouse.action.Disable();
    }

    private void OnRightClick(InputAction.CallbackContext context)
    {
        if (!enterEditmodeAction1.action.IsPressed()) return;

        var editable = vision.CurrentlyLookedObject?.GetComponent<EditableBlock>();
        if (editable == null) return;

        var manager = Object.FindFirstObjectByType<EditModeManager>();
        if (manager != null)
        {
            // 🔧 Перевірити: якщо вже інший блок активний, вийти з нього
            if (!manager.IsCurrentlyEditing(editable))
            {
                manager.SelectBlock(editable);
                Debug.Log("✅ Блок обрано!");
            }
            else
            {
                Debug.Log("🟡 Цей блок вже в режимі редагування");
            }
        }
    }

}
