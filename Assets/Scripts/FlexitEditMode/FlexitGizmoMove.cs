using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class FlexitGizmoMove : MonoBehaviour, IFlexitGizmo
{
    [SerializeField] private string redHandlePrefabPath = "Gismos_elements/Move/RedMoveHandleX";
    [SerializeField] private string greenHandlePrefabPath = "Gismos_elements/Move/GreenMoveHandleY";
    [SerializeField] private string blueHandlePrefabPath = "Gismos_elements/Move/BlueMoveHandleZ";

    private GameObject redHandlePos, redHandleNeg;
    private GameObject greenHandlePos, greenHandleNeg;
    private GameObject blueHandlePos, blueHandleNeg;

    private GameObject redHandlePrefab, greenHandlePrefab, blueHandlePrefab;

    private Transform target;
    public Transform handlesRoot;

    private InputAction handleControlAction, shiftAction, resetMoveAction, ctrlAction, scrollAction;

    private bool isDragging = false;
    private GameObject activeHandle = null;
    private Vector3 initialMouseWorldPos, initialTargetPos, initialHandlesRootPos;
    private Vector3 customRight, customForward;
    private float lastTargetYRotation;

    private Camera cam;
    private GizmoInfoUI gizmoInfoUI;

    public bool isScaleActive = false;

    private void Start()
    {
        gizmoInfoUI = FindAnyObjectByType<GizmoInfoUI>();
        cam = Camera.main;
    }

    public void Initialize(Transform targetBlock)
    {
        target = targetBlock;
        InitializeCustomVectors();

        DestroyHandles();

        redHandlePrefab = LoadPrefab(redHandlePrefabPath, "Red");
        greenHandlePrefab = LoadPrefab(greenHandlePrefabPath, "Green");
        blueHandlePrefab = LoadPrefab(blueHandlePrefabPath, "Blue");

        redHandlePos = CreateHandle(redHandlePrefab, "Red_Pos");
        redHandleNeg = CreateHandle(redHandlePrefab, "Red_Neg");
        greenHandlePos = CreateHandle(greenHandlePrefab, "Green_Pos");
        greenHandleNeg = CreateHandle(greenHandlePrefab, "Green_Neg");
        blueHandlePos = CreateHandle(blueHandlePrefab, "Blue_Pos");
        blueHandleNeg = CreateHandle(blueHandlePrefab, "Blue_Neg");

        SetHandlesActive(false);
        SetupInput();
    }

    private GameObject LoadPrefab(string path, string color)
    {
        GameObject prefab = Resources.Load<GameObject>(path);
        if (!prefab) Debug.LogError($"[FlexitGizmoMove] Missing prefab: {color} at {path}");
        return prefab;
    }

    private GameObject CreateHandle(GameObject prefab, string name)
    {
        if (prefab == null || handlesRoot == null) return null;
        GameObject handle = Instantiate(prefab, handlesRoot);
        handle.name = name;
        return handle;
    }

    private void LateUpdate()
    {
        if (target != null && handlesRoot != null && !isDragging)
        {
            handlesRoot.position = target.position;
            handlesRoot.rotation = target.rotation;
        }
    }

    private void Update()
    {
        if (FlexitGizmoManager.Instance.CurrentMode != GizmoMode.Move) return; // або Move / RotatePivot

        if (handlesRoot == null || !handlesRoot.gameObject.activeSelf) return;

        UpdateHandles();

        if (handleControlAction == null) return;

        if (handleControlAction.WasPressedThisFrame()) TryStartDrag();
        else if (handleControlAction.IsPressed())
        {
            UpdateDrag();
            UpdateBindHelpUI();
        }
        else if (handleControlAction.WasReleasedThisFrame()) EndDrag();
        if (scrollAction != null && scrollAction.enabled)
        {
            Vector2 scrollValue = scrollAction.ReadValue<Vector2>();
            if (scrollValue.y != 0)
            {
                HandleScroll(scrollValue);
            }
        }

        UpdateCustomVectorsByYRotation();
    }

    public void SetHandlesActive(bool active)
    {
        if (handlesRoot != null)
            handlesRoot.gameObject.SetActive(active);
        SetInputEnabled(active);
    }

    public void SetInputEnabled(bool enabled)
    {
        if (enabled)
        {
            handleControlAction?.Enable();
            shiftAction?.Enable();
            resetMoveAction?.Enable();
            ctrlAction?.Enable();
            scrollAction?.Enable();
        }
        else
        {
            handleControlAction?.Disable();
            shiftAction?.Disable();
            resetMoveAction?.Disable();
            ctrlAction?.Disable();
            scrollAction?.Disable();
        }
    }

    public void DestroyHandles()
    {
        DestroySafe(redHandlePos); DestroySafe(redHandleNeg);
        DestroySafe(greenHandlePos); DestroySafe(greenHandleNeg);
        DestroySafe(blueHandlePos); DestroySafe(blueHandleNeg);

        redHandlePos = redHandleNeg = greenHandlePos = greenHandleNeg = blueHandlePos = blueHandleNeg = null;
        SetInputEnabled(false);
    }

    private void DestroySafe(GameObject go)
    {
        if (go != null) Destroy(go);
    }

    private void SetupInput()
    {
        handleControlAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        shiftAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/leftShift");
        resetMoveAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/r");
        ctrlAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/leftCtrl");
        scrollAction = new InputAction(type: InputActionType.Value, binding: "<Mouse>/scroll");

        handleControlAction.Enable();
        shiftAction.Enable();
        resetMoveAction.Enable();
        ctrlAction.Enable();
        scrollAction.Enable();
    }

    private void OnDestroy()
    {
        SetInputEnabled(false);
    }

    private void TryStartDrag()
    {
        if (cam == null || isDragging) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        int gizmoLayerMask = 1 << LayerMask.NameToLayer("GizmoHandle");

        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, gizmoLayerMask);

        foreach (var hit in hits)
        {
            GameObject go = hit.collider.gameObject;
            if (go == redHandlePos || go == redHandleNeg ||
                go == greenHandlePos || go == greenHandleNeg ||
                go == blueHandlePos || go == blueHandleNeg)
            {
                StartDrag(go);
                return;
            }
        }
    }

    private void StartDrag(GameObject handle)
    {
        isDragging = true;
        activeHandle = handle;
        initialTargetPos = target.position;
        initialHandlesRootPos = handlesRoot.position;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane dragPlane = new Plane(cam.transform.forward, target.position);
        if (dragPlane.Raycast(ray, out float enter))
            initialMouseWorldPos = ray.GetPoint(enter);
        else
            initialMouseWorldPos = target.position;
    }

    private void UpdateDrag()
    {
        if (isScaleActive) return;
        if (!isDragging || activeHandle == null || cam == null) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane dragPlane = new Plane(cam.transform.forward, handlesRoot.position);
        if (!dragPlane.Raycast(ray, out float enter)) return;

        Vector3 currentMouseWorldPos = ray.GetPoint(enter);
        Vector3 deltaWorld = currentMouseWorldPos - initialMouseWorldPos;

        Vector3 axis = Vector3.zero;
        if (activeHandle == redHandlePos) axis = customRight;
        else if (activeHandle == redHandleNeg) axis = -customRight;
        else if (activeHandle == greenHandlePos) axis = Vector3.up;
        else if (activeHandle == greenHandleNeg) axis = -Vector3.up;
        else if (activeHandle == blueHandlePos) axis = customForward;
        else if (activeHandle == blueHandleNeg) axis = -customForward;

        float deltaRaw = Vector3.Dot(deltaWorld, axis);
        float pixelStep = 4f / 64f;
        float delta = (shiftAction != null && shiftAction.IsPressed())
            ? deltaRaw
            : Mathf.Round(deltaRaw / pixelStep) * pixelStep;

        Vector3 offset = axis * delta;
        target.position = initialTargetPos + offset;
        handlesRoot.position = initialHandlesRootPos + offset;
        gizmoInfoUI?.SetPositionInfo(target.position);
    }

    private void EndDrag()
    {
        isDragging = false;
        activeHandle = null;
        UpdateBindHelpUI();
    }

    public void HandleScroll(Vector2 scrollDelta)
    {
        if (target == null || scrollAction == null) return;

        Vector2 scroll = scrollAction.ReadValue<Vector2>();
        float scrollAmount = scroll.y;

        if (Mathf.Abs(scrollAmount) < 0.01f) return; // нічого не крутили

        Vector3 camForward = cam.transform.forward.normalized;

        Vector3[] axes = new Vector3[] { customRight, Vector3.up, customForward };
        string[] axisNames = new string[] { "X", "Y", "Z" };

        float maxDot = -1f;
        int selectedAxis = -1;

        for (int i = 0; i < axes.Length; i++)
        {
            float dot = Vector3.Dot(camForward, axes[i]);
            float absDot = Mathf.Abs(dot);

            if (absDot > maxDot)
            {
                maxDot = absDot;
                selectedAxis = i;
            }
        }

        if (selectedAxis == -1) return;

        Vector3 axis = axes[selectedAxis];
        float direction = Vector3.Dot(camForward, axis) > 0 ? 1f : -1f;

        float baseStep = 4f / 64f;
        float step = (shiftAction != null && shiftAction.IsPressed()) ? baseStep * 0.2f : baseStep;
        float moveAmount = scrollAmount * step * direction;

        Vector3 offset = axis * moveAmount;

        target.position += offset;
        handlesRoot.position += offset;

        gizmoInfoUI?.SetPositionInfo(target.position);
    }

   

    public void InitializeCustomVectors()
    {
        customRight = Vector3.ProjectOnPlane(target.right, Vector3.up).normalized;
        customForward = Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;
        lastTargetYRotation = target.eulerAngles.y;
    }

    private void UpdateCustomVectorsByYRotation()
    {
        float currentYRotation = target.eulerAngles.y;
        float deltaY = Mathf.DeltaAngle(lastTargetYRotation, currentYRotation);
        if (Mathf.Abs(deltaY) > 0.01f)
        {
            Quaternion yRotation = Quaternion.AngleAxis(deltaY, Vector3.up);
            customRight = yRotation * customRight;
            customForward = yRotation * customForward;
            lastTargetYRotation = currentYRotation;
        }
    }

    private void UpdateBindHelpUI()
    {
        if (HintUIManager.Instance == null) return;
        if (isScaleActive)
        {
            HintUIManager.Instance.ClearHint("Move");
            return; // Якщо скейл активний — не показуємо підказки Move
        }
        if (isDragging)
        {
            HintUIManager.Instance.ShowHint("Move", HintUIManager.Tips.Move);
        }
        else
        {
            HintUIManager.Instance.ClearHint("Move");
        }
    }

    public void UpdateHandles()
    {
        if (target == null || handlesRoot == null) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 center = handlesRoot.position;
        Vector3 camPos = cam.transform.position;
        float distance = Vector3.Distance(camPos, center);
        float scaleFactor = Mathf.Max(distance / 5f, 0.5f);

        float baseHandleScale = 0.1f;
        float baseHandleOffset = 0.5f;
        Quaternion onlyYRotation = Quaternion.Euler(0f, target.eulerAngles.y, 0f);

        Vector3 right = onlyYRotation * Vector3.right;
        Vector3 left = -right;
        Vector3 forward = onlyYRotation * Vector3.forward;
        Vector3 back = -forward;
        Vector3 up = Vector3.up;
        Vector3 down = -Vector3.up;

        Vector3 handleScale = Vector3.one * baseHandleScale * scaleFactor / handlesRoot.localScale.x;

        SetHandleTransform(redHandlePos, center + right * baseHandleOffset * scaleFactor, handleScale, Quaternion.LookRotation(right, up), Vector3.Dot((camPos - center).normalized, right));
        SetHandleTransform(redHandleNeg, center + left * baseHandleOffset * scaleFactor, handleScale, Quaternion.LookRotation(left, up), Vector3.Dot((camPos - center).normalized, left));
        SetHandleTransform(greenHandlePos, center + up * baseHandleOffset * scaleFactor, handleScale, Quaternion.LookRotation(up, up), Vector3.Dot((camPos - center).normalized, up));
        SetHandleTransform(greenHandleNeg, center + down * baseHandleOffset * scaleFactor, handleScale, Quaternion.LookRotation(down, up), Vector3.Dot((camPos - center).normalized, down));
        SetHandleTransform(blueHandlePos, center + forward * baseHandleOffset * scaleFactor, handleScale, Quaternion.LookRotation(forward, up), Vector3.Dot((camPos - center).normalized, forward));
        SetHandleTransform(blueHandleNeg, center + back * baseHandleOffset * scaleFactor, handleScale, Quaternion.LookRotation(back, up), Vector3.Dot((camPos - center).normalized, back));
    }

    private void SetHandleTransform(GameObject handle, Vector3 position, Vector3 scale, Quaternion rotation, float dot)
    {
        if (handle == null) return;
        bool visible = dot > 0f;
        handle.SetActive(visible);
        if (!visible) return;

        handle.transform.position = position;
        handle.transform.localScale = scale;
        handle.transform.rotation = rotation;
    }
}
