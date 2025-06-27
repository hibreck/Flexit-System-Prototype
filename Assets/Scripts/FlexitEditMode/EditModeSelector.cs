using UnityEngine.InputSystem;

using UnityEngine;

public class EditModeSelector : MonoBehaviour
{
    [SerializeField] private PlayerVision vision;
    [SerializeField] private InputActionReference shiftAction;
    [SerializeField] private InputActionReference rightClickAction;

    

    private void OnEnable()
    {
        shiftAction.action.Enable();
        rightClickAction.action.Enable();
        rightClickAction.action.performed += OnRightClick;
    }

    private void OnDisable()
    {
        rightClickAction.action.performed -= OnRightClick;
        shiftAction.action.Disable();
        rightClickAction.action.Disable();
    }

    private void OnRightClick(InputAction.CallbackContext context)
    {
        if (!shiftAction.action.IsPressed()) return;

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
