using UnityEngine;
using UnityEngine.InputSystem;

public class FlexitGizmoPivot : MonoBehaviour
{
    [SerializeField] private string redHandlePrefabPath = "Gismos_elements/Pivot/PivotX";
    [SerializeField] private string greenHandlePrefabPath = "Gismos_elements/Pivot/PivotY";
    [SerializeField] private string blueHandlePrefabPath = "Gismos_elements/Pivot/PivotZ";

    private GameObject redHandlePos, redHandleNeg;
    private GameObject greenHandlePos, greenHandleNeg;
    private GameObject blueHandlePos, blueHandleNeg;

    private GameObject redHandlePrefab, greenHandlePrefab, blueHandlePrefab;

    public Transform handlesRoot;

    private InputAction handleControlAction;
    private InputAction shiftAction;
    private InputAction resetPivotAction;
    private InputAction middleClickAction;

    private bool isDragging = false;
    private GameObject activeHandle = null;
    private Vector3 initialMouseWorldPos;
    private Vector3 initialPivotPos;
    private Transform target;
    private GizmoInfoUI gizmoInfoUI;
    private Plane dragPlane;

    private Vector3 pivotLocalOffset = Vector3.zero;
    public bool isPivotDragged { get; private set; } = false;

    private Camera cam;

    private Transform cubeGizmoRoot;

    public void Initialize(Transform targetBlock)
    {
        target = targetBlock;

        if (target != null && handlesRoot != null)
        {
            var eb = target.GetComponent<EditableBlock>();
            if (eb != null)
                pivotLocalOffset = eb.savedPivotLocalOffset;

            handlesRoot.position = target.TransformPoint(pivotLocalOffset);
            transform.position = target.position;
            cubeGizmoRoot = targetBlock;
        }
    }


    private void Start()
    {
        cam = Camera.main;
        gizmoInfoUI = FindAnyObjectByType<GizmoInfoUI>();

        LoadPrefabs();
        CreateHandlesRoot();
        SetupInput();
    }

    private void OnDestroy()
    {
        // Відписка і вимкнення InputActions
        if (handleControlAction != null) handleControlAction.Disable();
        if (shiftAction != null) shiftAction.Disable();
        if (resetPivotAction != null) resetPivotAction.Disable();
        if (middleClickAction != null)
        {
            middleClickAction.performed += ctx =>
            {
                if (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed)
                {
                    OnCtrlMiddleClick();
                }
            };


            middleClickAction.Disable();
        }
    }

    private void LoadPrefabs()
    {
        redHandlePrefab = Resources.Load<GameObject>(redHandlePrefabPath);
        greenHandlePrefab = Resources.Load<GameObject>(greenHandlePrefabPath);
        blueHandlePrefab = Resources.Load<GameObject>(blueHandlePrefabPath);

        if (!redHandlePrefab) Debug.LogError($"Не знайдено prefab: {redHandlePrefabPath}");
        if (!greenHandlePrefab) Debug.LogError($"Не знайдено prefab: {greenHandlePrefabPath}");
        if (!blueHandlePrefab) Debug.LogError($"Не знайдено prefab: {blueHandlePrefabPath}");
    }

    private void CreateHandlesRoot()
    {
        redHandlePos = Instantiate(redHandlePrefab, handlesRoot);
        redHandleNeg = Instantiate(redHandlePrefab, handlesRoot);
        redHandleNeg.name += "_Neg";

        greenHandlePos = Instantiate(greenHandlePrefab, handlesRoot);
        greenHandleNeg = Instantiate(greenHandlePrefab, handlesRoot);
        greenHandleNeg.name += "_Neg";

        blueHandlePos = Instantiate(blueHandlePrefab, handlesRoot);
        blueHandleNeg = Instantiate(blueHandlePrefab, handlesRoot);
        blueHandleNeg.name += "_Neg";
    }

    private void SetupInput()
    {
        handleControlAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        shiftAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/leftShift");
        resetPivotAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/r");
        middleClickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/middleButton");

        handleControlAction.Enable();
        shiftAction.Enable();
        resetPivotAction.Enable();
        middleClickAction.Enable();

        middleClickAction.performed += ctx =>
        {
            if (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed)
            {
                OnCtrlMiddleClick();
            }
        };


    }

    private void Update()
    {
        if (cam == null) cam = Camera.main;
        UpdateHandles();

        if (handleControlAction.WasPressedThisFrame()) TryStartDrag();
        else if (handleControlAction.IsPressed()) UpdateDrag();
        else if (handleControlAction.WasReleasedThisFrame()) EndDrag();

        if (!isPivotDragged && target != null)
        {
            handlesRoot.position = target.TransformPoint(pivotLocalOffset);
        }
    }

    public void ResetPivotPosition()
    {
        if (target == null) return;

        pivotLocalOffset = Vector3.zero;
        handlesRoot.position = target.TransformPoint(pivotLocalOffset);

        var eb = target.GetComponent<EditableBlock>();
        if (eb != null)
            eb.savedPivotLocalOffset = pivotLocalOffset;
    }


    private void UpdateHandles()
    {
        if (handlesRoot == null || cam == null) return;

        Vector3 center = handlesRoot.position;
        Vector3 camPos = cam.transform.position;

        float distance = Vector3.Distance(camPos, center);
        float scaleFactor = Mathf.Max(distance / 5f, 0.5f);

        float baseHandleScale = 0.1f;
        float baseHandleOffset = 0.5f;

        Vector3 right = Vector3.right;
        Vector3 left = -Vector3.right;
        Vector3 up = Vector3.up;
        Vector3 down = -Vector3.up;
        Vector3 forward = Vector3.forward;
        Vector3 back = -Vector3.forward;

        Vector3 dirToCam = (camPos - center).normalized;

        SetHandleTransform(redHandlePos, center + right * baseHandleOffset * scaleFactor, baseHandleScale * scaleFactor, Quaternion.LookRotation(right, Vector3.up), Vector3.Dot(dirToCam, right));
        SetHandleTransform(redHandleNeg, center + left * baseHandleOffset * scaleFactor, baseHandleScale * scaleFactor, Quaternion.LookRotation(left, Vector3.up), Vector3.Dot(dirToCam, left));
        SetHandleTransform(greenHandlePos, center + up * baseHandleOffset * scaleFactor, baseHandleScale * scaleFactor, Quaternion.LookRotation(up, Vector3.up), Vector3.Dot(dirToCam, up));
        SetHandleTransform(greenHandleNeg, center + down * baseHandleOffset * scaleFactor, baseHandleScale * scaleFactor, Quaternion.LookRotation(down, Vector3.up), Vector3.Dot(dirToCam, down));
        SetHandleTransform(blueHandlePos, center + forward * baseHandleOffset * scaleFactor, baseHandleScale * scaleFactor, Quaternion.LookRotation(forward, Vector3.up), Vector3.Dot(dirToCam, forward));
        SetHandleTransform(blueHandleNeg, center + back * baseHandleOffset * scaleFactor, baseHandleScale * scaleFactor, Quaternion.LookRotation(back, Vector3.up), Vector3.Dot(dirToCam, back));
    }

    private void SetHandleTransform(GameObject handle, Vector3 position, float baseScale, Quaternion rotation, float dot)
    {
        if (handle == null) return;

        bool visible = dot > 0f;
        handle.SetActive(visible);
        if (!visible) return;

        handle.transform.position = position;
        handle.transform.localScale = Vector3.one * baseScale / handlesRoot.localScale.x;
        handle.transform.rotation = rotation;
    }

    private void TryStartDrag()
    {
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        int mask = 1 << LayerMask.NameToLayer("GizmoHandle");
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, mask);

        foreach (var hit in hits)
        {
            if (IsHandle(hit.collider.gameObject))
            {
                StartDrag(hit.collider.gameObject);
                return;
            }
        }

        isPivotDragged = false;
    }

    private bool IsHandle(GameObject go)
    {
        return go == redHandlePos || go == redHandleNeg ||
               go == greenHandlePos || go == greenHandleNeg ||
               go == blueHandlePos || go == blueHandleNeg;
    }

    private void StartDrag(GameObject handle)
    {
        isDragging = true;
        isPivotDragged = true;
        activeHandle = handle;
        initialPivotPos = handlesRoot.position;

        if (cam == null) cam = Camera.main;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        dragPlane = new Plane(cam.transform.forward, handlesRoot.position);

        if (dragPlane.Raycast(ray, out float enter))
            initialMouseWorldPos = ray.GetPoint(enter);
        else
            initialMouseWorldPos = transform.position;

        UpdateBindHelpUI();
    }

    private void UpdateDrag()
    {
        if (!isDragging || activeHandle == null || target == null) return;

        if (cam == null) cam = Camera.main;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!dragPlane.Raycast(ray, out float enter)) return;

        Vector3 currentMouseWorldPos = ray.GetPoint(enter);
        Vector3 deltaWorld = currentMouseWorldPos - initialMouseWorldPos;

        Vector3 axis = GetHandleAxis(activeHandle);

        float pixelStep = 4f / 64f;
        float deltaRaw = Vector3.Dot(deltaWorld, axis);
        float delta = shiftAction.IsPressed() ? deltaRaw : Mathf.Round(deltaRaw / pixelStep) * pixelStep;

        Vector3 offset = axis * delta;

        Vector3 newPosition = initialPivotPos + offset;

        if (Keyboard.current.leftCtrlKey.isPressed)
        {
            Collider col = target.GetComponent<Collider>();
            if (col != null)
            {
                newPosition = ClampPositionToCollider(newPosition, col);
            }
        }

        handlesRoot.position = newPosition;
        gizmoInfoUI?.SetPivotPositionInfo(handlesRoot.position);
    }

    private Vector3 ClampPositionToCollider(Vector3 worldPos, Collider col)
    {
        Vector3 localPos = target.InverseTransformPoint(worldPos);

        if (col is BoxCollider box)
        {
            Vector3 halfSize = box.size * 0.5f;
            Vector3 center = box.center;

            Vector3 min = center - halfSize;
            Vector3 max = center + halfSize;

            localPos.x = Mathf.Clamp(localPos.x, min.x, max.x);
            localPos.y = Mathf.Clamp(localPos.y, min.y, max.y);
            localPos.z = Mathf.Clamp(localPos.z, min.z, max.z);
        }

        return target.TransformPoint(localPos);
    }

    public void OnCtrlMiddleClick()
    {
        // Якщо handlesRoot не активний, то дія не виконується.
        if (handlesRoot == null || !handlesRoot.gameObject.activeInHierarchy)
            return;

        if (target == null) return;

        Collider col = target.GetComponent<Collider>();
        if (col == null) return;

        if (cam == null) cam = Camera.main;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            handlesRoot.position = hit.point;
            pivotLocalOffset = target.InverseTransformPoint(handlesRoot.position);
            gizmoInfoUI?.SetPivotPositionInfo(handlesRoot.position);
        }
    }


    private Vector3 GetHandleAxis(GameObject handle)
    {
        if (handle == redHandlePos) return Vector3.right;
        if (handle == redHandleNeg) return Vector3.left;
        if (handle == greenHandlePos) return Vector3.up;
        if (handle == greenHandleNeg) return Vector3.down;
        if (handle == blueHandlePos) return Vector3.forward;
        if (handle == blueHandleNeg) return Vector3.back;
        return Vector3.zero;
    }

    public void SetHandlesActive(bool active)
    {
        if (handlesRoot != null)
            handlesRoot.gameObject.SetActive(active);
    }

    private void UpdateBindHelpUI()
    {
        if (HintUIManager.Instance == null) return;
        if (isDragging)
        {
            HintUIManager.Instance.ShowHint("MovePivot", HintUIManager.Tips.MovePivot);
        }
        else
        {
            HintUIManager.Instance.ClearHint("MovePivot");
        }
    }

    private void EndDrag()
    {
        if (isDragging && isPivotDragged && target != null)
        {
            pivotLocalOffset = target.InverseTransformPoint(handlesRoot.position);

            var eb = target.GetComponent<EditableBlock>();
            if (eb != null)
                eb.savedPivotLocalOffset = pivotLocalOffset;
        }

        isDragging = false;
        activeHandle = null;
        isPivotDragged = false;

        UpdateBindHelpUI();
    }


    public Vector3 PivotLocalOffset => pivotLocalOffset;
}
