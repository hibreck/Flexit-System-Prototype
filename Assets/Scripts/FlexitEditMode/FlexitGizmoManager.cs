using UnityEngine;
using UnityEngine.InputSystem;

public enum GizmoMode { Move, Scale, RotatePivot }

[DisallowMultipleComponent]
public class FlexitGizmoManager : MonoBehaviour
{
    public static FlexitGizmoManager Instance { get; private set; }

    private Transform moveGizmoRoot;
    private Transform scaleGizmoRoot;
    private Transform rotateGizmoRoot;

    private EditableBlock currentEditableBlock;

    private FlexitGizmoMove moveGizmo;
    private FlexitGizmoPlaneMove movePlaneGizmo;
    private FlexitGizmoScale scaleGizmo;
    private FlexitGizmoPlaneScale scalePlaneGizmo;
    private FlexitGizmoPivot pivotGizmo;
    private FlexitGizmoRotate rotateGizmo;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference switchToMoveActionRef;
    [SerializeField] private InputActionReference switchToScaleActionRef;
    [SerializeField] private InputActionReference switchToRotatePivotActionRef;

    [SerializeField] private InputActionReference resetActionRef;
    [SerializeField] private InputActionReference pivotResetActionRef;
    
    [SerializeField] private InputActionReference modeScrollSwitchActionRef;

    public GizmoMode CurrentMode { get; private set; } = GizmoMode.Move;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"🟥 Зайвий FlexitGizmoManager видалено з {gameObject.name}");
            Destroy(this); // тільки компонент
            return;
        }
        Instance = this;

        DontDestroyOnLoad(this); // якщо треба, щоб лишався між сценами
    }



    private void OnEnable()
    {
        // Підписка на події натискання клавіш 1,2,3
        switchToMoveActionRef.action.performed += OnSwitchToMove;
        switchToScaleActionRef.action.performed += OnSwitchToScale;
        switchToRotatePivotActionRef.action.performed += OnSwitchToRotatePivot;

        resetActionRef.action.performed += OnReset;
        pivotResetActionRef.action.performed += OnPivotReset;

        modeScrollSwitchActionRef.action.performed += OnModeScroll;

        // Активуємо дії
        switchToMoveActionRef.action.Enable();
        switchToScaleActionRef.action.Enable();
        switchToRotatePivotActionRef.action.Enable();

        resetActionRef.action.Enable();
        pivotResetActionRef.action.Enable();
        
        modeScrollSwitchActionRef.action.Enable();
    }

    private void OnDisable()
    {
        // Відписка від подій
        switchToMoveActionRef.action.performed -= OnSwitchToMove;
        switchToScaleActionRef.action.performed -= OnSwitchToScale;
        switchToRotatePivotActionRef.action.performed -= OnSwitchToRotatePivot;

        resetActionRef.action.performed -= OnReset;
        pivotResetActionRef.action.performed -= OnPivotReset;

        modeScrollSwitchActionRef.action.performed -= OnModeScroll;

        // Вимикаємо дії
        switchToMoveActionRef.action.Disable();
        switchToScaleActionRef.action.Disable();
        switchToRotatePivotActionRef.action.Disable();

        resetActionRef.action.Disable();
        pivotResetActionRef.action.Disable();
        
        modeScrollSwitchActionRef.action.Disable();
    }

    private void CreateGizmoRoots()
    {
        moveGizmoRoot = CreateRootIfNotExists("_Move_GizmoRoot");
        scaleGizmoRoot = CreateRootIfNotExists("_Scale_GizmoRoot");
        rotateGizmoRoot = CreateRootIfNotExists("_Rotate_GizmoRoot");
    }

    private Transform CreateRootIfNotExists(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;
        }
        return go.transform;
    }

    private void OnSwitchToMove(InputAction.CallbackContext ctx)
    {
        SetMode(GizmoMode.Move);
    }

    private void OnSwitchToScale(InputAction.CallbackContext ctx)
    {
        SetMode(GizmoMode.Scale);
    }

    private void OnSwitchToRotatePivot(InputAction.CallbackContext ctx)
    {
        SetMode(GizmoMode.RotatePivot);
    }

    private void OnReset(InputAction.CallbackContext ctx)
    {
        switch (CurrentMode)
        {
            case GizmoMode.Move:
                moveGizmo?.SnapToPixelGrid();
                break;
            case GizmoMode.Scale:
                scaleGizmo?.ResetScaleToNearestPixelStep();
                break;
            case GizmoMode.RotatePivot:
                rotateGizmo?.ResetRotation();
                break;
        }
    }

    private void OnPivotReset(InputAction.CallbackContext ctx)
    {
        if (CurrentMode == GizmoMode.RotatePivot)
        {
            pivotGizmo?.ResetPivotPosition();
        }
    }

    private void OnModeScroll(InputAction.CallbackContext ctx)
    {
        Vector2 scroll = ctx.ReadValue<Vector2>();

        if (Mathf.Abs(scroll.y) > 0.01f)
        {
            CycleMode(scroll.y);
        }
    }

    private void CycleMode(float scrollDelta)
    {
        int dir = scrollDelta > 0 ? 1 : -1;
        int modeCount = System.Enum.GetNames(typeof(GizmoMode)).Length;

        int newModeIndex = ((int)CurrentMode + dir + modeCount) % modeCount;
        SetMode((GizmoMode)newModeIndex);
    }

    public void SetTarget(Transform newTarget)
    {
        currentEditableBlock = newTarget != null ? newTarget.GetComponent<EditableBlock>() : null;

        if (newTarget == null || currentEditableBlock == null || !currentEditableBlock.IsInEditMode)
        {
            DisableAllGizmos();

            // Знищуємо гізмо-рути (гізмо-об'єкти)
            DestroyGizmoGameObjects();

            ClearCachedGizmos();
            return;
        }
        // Якщо рути не існують, створюємо їх ТІЛЬКИ тут
        if (moveGizmoRoot == null || scaleGizmoRoot == null || rotateGizmoRoot == null)
        {
            CreateGizmoRoots();
        }

        // Якщо гізмо-рути були видалені раніше, відновлюємо їх
        if (moveGizmoRoot == null || scaleGizmoRoot == null || rotateGizmoRoot == null)
        {
            CreateGizmoRoots();
        }

        CacheGizmos(newTarget);

        if (moveGizmo != null)
        {
            moveGizmo.handlesRoot = moveGizmoRoot;
            moveGizmo.Initialize(newTarget);
        }
        if (movePlaneGizmo != null)
        {
            movePlaneGizmo.handlesRoot = moveGizmoRoot;
            movePlaneGizmo.Initialize(newTarget);
        }
        if (scaleGizmo != null)
        {
            scaleGizmo.handlesRoot = scaleGizmoRoot;
            scaleGizmo.Initialize(newTarget);
        }
        if (scalePlaneGizmo != null)
        {
            scalePlaneGizmo.handlesRoot = scaleGizmoRoot;
            scalePlaneGizmo.Initialize(newTarget);
        }
        if (pivotGizmo != null)
        {
            pivotGizmo.handlesRoot = rotateGizmoRoot;
            pivotGizmo.Initialize(newTarget);
        }
        
        if (rotateGizmo != null && pivotGizmo != null)
        {
            rotateGizmo.SetPivotReference(pivotGizmo);
        }
        if (rotateGizmo != null)
        {
            rotateGizmo.handlesRoot = rotateGizmoRoot;
            rotateGizmo.Initialize(newTarget);
        }

        UpdateActiveGizmo();
    }
    public bool TargetIs(Transform t)
    {
        return currentEditableBlock != null && currentEditableBlock.transform == t;
    }


    private void DestroyGizmoGameObjects()
    {
        if (moveGizmoRoot != null)
        {
            GameObject.Destroy(moveGizmoRoot.gameObject);
            moveGizmoRoot = null;
        }
        if (scaleGizmoRoot != null)
        {
            GameObject.Destroy(scaleGizmoRoot.gameObject);
            scaleGizmoRoot = null;
        }
        if (rotateGizmoRoot != null)
        {
            GameObject.Destroy(rotateGizmoRoot.gameObject);
            rotateGizmoRoot = null;
        }
    }


    private void CacheGizmos(Transform target)
    {
        moveGizmo = target.GetComponent<FlexitGizmoMove>();
        movePlaneGizmo = target.GetComponent<FlexitGizmoPlaneMove>();
        scaleGizmo = target.GetComponent<FlexitGizmoScale>();
        scalePlaneGizmo = target.GetComponent<FlexitGizmoPlaneScale>();
        pivotGizmo = target.GetComponent<FlexitGizmoPivot>();
        rotateGizmo = target.GetComponent<FlexitGizmoRotate>();
    }

    private void ClearCachedGizmos()
    {
        moveGizmo = null;
        movePlaneGizmo = null;
        scaleGizmo = null;
        scalePlaneGizmo = null;
        pivotGizmo = null;
        rotateGizmo = null;
    }

    public void SetMode(GizmoMode newMode)
    {
        if (newMode == CurrentMode) return;

        CurrentMode = newMode;
        UpdateActiveGizmo();

        if (HintUIManager.Instance != null)
            HintUIManager.Instance.ShowHint("Mode", $"<b>Mode:</b> {newMode}");
        else
            Debug.LogWarning("HintUIManager.Instance is null!");
    }


    private void UpdateActiveGizmo()
    {
        DisableAllGizmos();

        switch (CurrentMode)
        {
            case GizmoMode.Move:
                moveGizmo?.SetHandlesActive(true);
                movePlaneGizmo?.SetHandlesActive(true);
                break;

            case GizmoMode.Scale:
                scaleGizmo?.SetHandlesActive(true);
                scalePlaneGizmo?.SetHandlesActive(true);
                break;

            case GizmoMode.RotatePivot:
                pivotGizmo?.SetHandlesActive(true);
                rotateGizmo?.SetHandlesActive(true);
                break;
        }
    }

    public void OnCtrlMiddleClick()
    {
        if (CurrentMode != GizmoMode.RotatePivot)
            return;  // Якщо не режим RotatePivot — вихід

        if (pivotGizmo != null)
        {
            pivotGizmo.OnCtrlMiddleClick();
        }
    }


    private void DisableAllGizmos()
    {
        moveGizmo?.SetHandlesActive(false);
        movePlaneGizmo?.SetHandlesActive(false);
        scaleGizmo?.SetHandlesActive(false);
        scalePlaneGizmo?.SetHandlesActive(false);
        pivotGizmo?.SetHandlesActive(false);
        rotateGizmo?.SetHandlesActive(false);
    }
}
