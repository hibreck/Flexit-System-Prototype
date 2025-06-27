using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class EditModeManager : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private InputActionReference leftClickAction;
    [SerializeField] private InputActionReference middleClickAction;


    // Додамо посилання на GizmoManager
    [SerializeField] private FlexitGizmoManager gizmoManager;

    private TextMeshProUGUI bindHelpText;
    private EditableBlock currentEditable;
    private InputAction resetAction;

    public static EditModeManager Instance { get; private set; }
    public bool IsEditModeActive => currentEditable != null;

    private void Awake()
    {
        Instance = this;
        FindBindHelpText();

        // Якщо gizmoManager не встановлено через інспектор, шукаємо в сцені
        if (gizmoManager == null)
        {
            gizmoManager = FindAnyObjectByType<FlexitGizmoManager>();
            if (gizmoManager == null)
            {
                Debug.LogWarning("GizmoManager не знайдено у сцені!");
            }
        }
    }
    private void OnEnable()
    {
        middleClickAction.action.Enable();
        middleClickAction.action.performed += OnMiddleClick;
    }
    private void OnDisable()
    {
        middleClickAction.action.performed -= OnMiddleClick;
        middleClickAction.action.Disable();
    }

    private void FindBindHelpText()
    {
        GameObject go = GameObject.Find("GismoBindHelp");
        if (go != null)
        {
            bindHelpText = go.GetComponent<TextMeshProUGUI>();
            if (bindHelpText == null)
            {
                Debug.LogError("❌ Не знайдено компонент TextMeshProUGUI на об'єкті GismoBindHelp!");
            }
        }
        else
        {
            Debug.LogError("❌ Не знайдено об'єкт з назвою 'GismoBindHelp' у сцені!");
        }
    }

    public void SelectBlock(EditableBlock newBlock)
    {
        if (currentEditable == newBlock) return;

        // Вийти з попереднього, якщо є
        if (currentEditable != null)
        {
            currentEditable.ExitEditMode();
            if (gizmoManager != null)
            {
                gizmoManager.SetTarget(null); // Відключити гізмо
            }
        }

        // Увійти в новий
        currentEditable = newBlock;
        currentEditable.EnterEditMode();

        if (gizmoManager != null)
        {
            gizmoManager.SetTarget(currentEditable.transform);
        }

        ShowEditHint();
    }


    void Update()
    {
        // Якщо в Edit Mode, то ЛКМ вихід з режиму
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (currentEditable == null)
            {
                // 🟩 Спроба знищити блок, якщо не в Edit Mode
                var inventory = FindAnyObjectByType<PlayerInventory>();
                if (inventory != null)
                {
                    var slot = inventory.slots[inventory.activeSlot];
                    if (slot.item == null || slot.item.IsBuildTool())
                    {
                        FlexitDestroyer.TryDestroyFlexitFromCamera();
                    }

                }
            }

            return; // Щоб не дублювати логіку далі
        }

        if (leftClickAction.action.WasPressedThisFrame())
        {
            Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (!hit.collider.gameObject.TryGetComponent(out EditableBlock clickedBlock))
                {
                    ExitCurrent();
                }
            }
            else
            {
                ExitCurrent();
            }
        }

        // Вихід по ESC
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ExitCurrent();
        }
    }

    public void ExitCurrent()
    {
        if (currentEditable != null)
        {
            currentEditable.ExitEditMode();
            currentEditable = null;

            // Приховуємо гізмо
            if (gizmoManager != null)
            {
                gizmoManager.SetTarget(null);
            }

            ClearHint();
        }
    }

    private void ShowEditHint()
    {
        if (HintUIManager.Instance != null)
        {
            HintUIManager.Instance.ShowHint("Edit", HintUIManager.Tips.EditMode);
        }
        else
        {
            Debug.LogWarning("HintUIManager.Instance is null!");
        }
    }



    private void ClearHint()
    {
        if (HintUIManager.Instance.IsCurrentHint("Edit"))
        {
            HintUIManager.Instance.ClearHint("Edit");
        }
    }
    private void OnMiddleClick(InputAction.CallbackContext context)
    {
        if (Keyboard.current.leftCtrlKey.isPressed)
        {
            // Якщо Ctrl затиснутий — викликаємо півот-дію
            if (gizmoManager != null)
            {
                gizmoManager.OnCtrlMiddleClick();
            }
            return;
        }

        if (currentEditable == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Заборона телепортації на гравця
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                Debug.Log("Телепортація на гравця заборонена");
                return;
            }

            Collider blockCollider = currentEditable.GetComponent<Collider>();
            if (blockCollider == null)
            {
                Debug.LogWarning("EditableBlock не має колайдера!");
                return;
            }

            // Отримуємо найближчу точку на поверхні editable до хіта
            Vector3 pointOnSurface = blockCollider.ClosestPoint(hit.point - hit.normal * 10f);

            // Розраховуємо зсув (щоб точка на об'єкті співпала з хітом)
            Vector3 offset = currentEditable.transform.position - pointOnSurface;

            // Телепортуємо об'єкт з урахуванням повороту та форми
            currentEditable.transform.position = hit.point + offset;

            // Зупиняємо фізику після переміщення
            Rigidbody rb = currentEditable.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            Debug.Log("Не знайдено поверхню для телепортації");
        }
    }

    public bool IsCurrentlyEditing(EditableBlock block)
    {
        return currentEditable == block;
    }






}
