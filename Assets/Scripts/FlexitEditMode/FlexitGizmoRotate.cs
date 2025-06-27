using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class FlexitGizmoRotate : MonoBehaviour
{
    [Header("Handle Prefabs Paths")]
    [SerializeField] private string ringXPath = "Gismos_elements/Rotate/RingX";
    [SerializeField] private string ringYPath = "Gismos_elements/Rotate/RingY";
    [SerializeField] private string ringZPath = "Gismos_elements/Rotate/RingZ";


    [SerializeField] private FlexitGizmoPivot pivotGizmo;





    private GameObject ringX, ringY, ringZ;
    private GameObject activeHandle;

    private Transform target;
    public Transform handlesRoot;

    private InputAction leftClick;
    private InputAction shiftAction;
    private InputAction resetRotationAction;
    private InputAction scrollAction;

    private Camera cam;

    private bool isDragging = false;

    private Vector2 initialMousePos;

    private Vector3 rotationAxis;

    private const float rotationStep = 5f;
    
    private const float scrollRotationStep = 5f; // крок обертання градусами


    private int gizmoLayerMask;

    private GameObject dragAnchor;
    private Vector3 dragCenter;
    private Vector3 startDirection;
    private float dragRadius;
    private float currentAngle = 0f;

    private Quaternion initialRotation;
    private GizmoInfoUI gizmoInfoUI;

    public void Initialize(Transform targetObj)
    {
        if (ringX != null || ringY != null || ringZ != null)
            return; // ❗ Запобігти дублюванню кілець

        gizmoInfoUI = FindFirstObjectByType<GizmoInfoUI>();

        target = targetObj;
        cam = Camera.main;
        initialRotation = target.rotation;

        // Створення кілець
        ringX = Instantiate(Resources.Load<GameObject>(ringXPath));
        ringY = Instantiate(Resources.Load<GameObject>(ringYPath));
        ringZ = Instantiate(Resources.Load<GameObject>(ringZPath));

        ringX.transform.SetParent(handlesRoot, false);
        ringY.transform.SetParent(handlesRoot, false);
        ringZ.transform.SetParent(handlesRoot, false);

        ringX.name = "RingX";
        ringY.name = "RingY";
        ringZ.name = "RingZ";

        // Input
        leftClick = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        shiftAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/leftShift");
        resetRotationAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/r");
        scrollAction = new InputAction(type: InputActionType.Value, binding: "<Mouse>/scroll");
        scrollAction.Enable();


        leftClick.Enable();
        shiftAction.Enable();
        resetRotationAction.Enable();

        gizmoLayerMask = 1 << LayerMask.NameToLayer("GizmoHandle");
        
    }



    private void Update()
    {
        if (FlexitGizmoManager.Instance.CurrentMode != GizmoMode.RotatePivot) return;
        if (cam == null || target == null) return;

        UpdateHandles();

        if (leftClick.WasPressedThisFrame())
        {
            TryStartDrag();
        }
        else if (leftClick.IsPressed() && isDragging)
        {
            UpdateDrag();
        }
        else if (leftClick.WasReleasedThisFrame() && isDragging)
        {
            EndDrag();
        }
        if (scrollAction != null && scrollAction.enabled)
        {
            Vector2 scrollValue = scrollAction.ReadValue<Vector2>();
            if (scrollValue.y != 0)
            {
                HandleScroll(scrollValue);
            }
        }




        UpdateBindHelpUI();

    }
    public void SetPivotReference(FlexitGizmoPivot pivot)
    {
        pivotGizmo = pivot;
    }

    public void ResetRotation()
    {
        if (target == null || pivotGizmo == null) return;

        // 1. Запам’ятовуємо глобальну позицію півота ДО скидання обертання
        Vector3 pivotWorldBefore = target.TransformPoint(pivotGizmo.PivotLocalOffset);

        // 2. Скидаємо обертання
        target.rotation = Quaternion.identity;

        // 3. Після скидання — обчислюємо нову позицію півота
        Vector3 pivotWorldAfter = target.TransformPoint(pivotGizmo.PivotLocalOffset);

        // 4. Об’єкт зсувається так, щоб півот лишився на місці
        Vector3 correction = pivotWorldBefore - pivotWorldAfter;
        target.position += correction;
    }



    private void HandleScroll(Vector2 scrollValue)
    {
        if (Keyboard.current != null && Keyboard.current.altKey.isPressed)
            return;
        if (target == null || isDragging) return;

        float deltaAngle;

        if (shiftAction != null && shiftAction.IsPressed())
        {
            deltaAngle = scrollValue.y * 1f; // плавне обертання
        }
        else
        {
            deltaAngle = Mathf.Sign(scrollValue.y) * rotationStep; // крок 5 градусів
        }

        Vector3 pivot = handlesRoot.position; // півот для обертання

        // Обираємо вісь обертання (наприклад, Y - Vector3.up)
        Vector3 axis = Vector3.up;

        // Зсув позиції target в систему координат півота, обертання, і повернення назад
        Vector3 dir = target.position - pivot;         // вектор від півота до позиції target
        dir = Quaternion.AngleAxis(deltaAngle, axis) * dir;  // повертаємо вектор на deltaAngle навколо осі
        target.position = pivot + dir;                 // нова позиція target після обертання

        target.Rotate(axis, deltaAngle, Space.World); // обертання самого target на deltaAngle

        Vector3 normalizedRotation = NormalizeEuler(target.localEulerAngles);
        gizmoInfoUI.SetRotateInfo(normalizedRotation);
    }






    private void TryStartDrag()
    {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        float sphereRadius = 0.02f;
        float maxDistance = 100f;

        if (Physics.SphereCast(ray, sphereRadius, out RaycastHit hit, maxDistance, gizmoLayerMask))
        {
            if (hit.collider != null)
            {
                activeHandle = hit.collider.gameObject;

                if (activeHandle == ringX)
                    rotationAxis = Vector3.right;
                else if (activeHandle == ringY)
                    rotationAxis = Vector3.up;
                else if (activeHandle == ringZ)
                    rotationAxis = Vector3.forward;
                else
                {
                    activeHandle = null;
                    return;
                }

                isDragging = true;
                initialMousePos = Mouse.current.position.ReadValue();

                dragCenter = target.position;

                if (dragAnchor != null)
                    Destroy(dragAnchor);

                dragAnchor = new GameObject("DragAnchor");
                dragAnchor.transform.position = hit.point;
                dragAnchor.transform.rotation = Quaternion.identity;
                dragAnchor.transform.SetParent(activeHandle.transform);

                GameObject debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                debugSphere.transform.SetParent(dragAnchor.transform);
                debugSphere.transform.localPosition = Vector3.zero;
                debugSphere.transform.localScale = Vector3.one * 0.15f;
                Destroy(debugSphere.GetComponent<Collider>());

                Vector3 toAnchor = dragAnchor.transform.position - handlesRoot.position; // Працюємо з pivot
                Vector3 projected = Vector3.ProjectOnPlane(toAnchor, rotationAxis);
                dragRadius = projected.magnitude;
                startDirection = projected.normalized;

                currentAngle = 0f;

                Debug.Log($"Started dragging {activeHandle.name} on axis {rotationAxis}. Radius: {dragRadius}, startDir: {startDirection}");
            }
        }
    }

    private void UpdateDrag()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);

        Vector3 pivot = handlesRoot.position; // Півот для обертання

        Plane rotationPlane = new Plane(rotationAxis, pivot);

        if (rotationPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            Vector3 dir = hitPoint - pivot;
            dir = Vector3.ProjectOnPlane(dir, rotationAxis);

            if (dir.sqrMagnitude > 0.001f)
            {
                dir.Normalize();

                float rawAngle = Vector3.SignedAngle(startDirection, dir, rotationAxis);
                float deltaAngle;

                if (shiftAction != null && shiftAction.IsPressed())
                {
                    // Плавне обертання
                    deltaAngle = rawAngle - currentAngle;
                }
                else
                {
                    // Обертання з кроком
                    float snappedAngle = Mathf.Round(rawAngle / rotationStep) * rotationStep;
                    deltaAngle = snappedAngle - currentAngle;
                    rawAngle = snappedAngle; // для оновлення позиції dragAnchor
                }


                // Оновлення dragAnchor (якщо використовується)
                Vector3 newPos = pivot + Quaternion.AngleAxis(rawAngle, rotationAxis) * startDirection * dragRadius;

                if (dragAnchor != null)
                    dragAnchor.transform.position = newPos;

                // Обертання target навколо pivot (handlesRoot.position)
                Vector3 offset = target.position - pivot;
                offset = Quaternion.AngleAxis(deltaAngle, rotationAxis) * offset;
                target.position = pivot + offset;

                target.Rotate(rotationAxis, deltaAngle, Space.World);

                currentAngle = rawAngle;


            }
        }
        Vector3 normalizedRotation = NormalizeEuler(target.localEulerAngles);
        gizmoInfoUI.SetRotateInfo(normalizedRotation);
    }

    private void EndDrag()
    {
        isDragging = false;
        activeHandle = null;

        if (dragAnchor != null)
        {
            Destroy(dragAnchor);
            dragAnchor = null;
        }
    }

    private void UpdateHandles()
    {
        if (target == null || ringX == null || ringY == null || ringZ == null || cam == null)
            return;

        Vector3 center = handlesRoot.position;

        float distance = Vector3.Distance(cam.transform.position, center);
        float baseDistance = 5f;
        float scaleFactor = Mathf.Max(distance / baseDistance, 0.5f);

        float baseHandleScale = 2.2f;
        Vector3 handleScale = Vector3.one * baseHandleScale * scaleFactor;

        ringX.transform.position = center;
        ringY.transform.position = center;
        ringZ.transform.position = center;

        ringX.transform.localScale = handleScale;
        ringY.transform.localScale = handleScale;
        ringZ.transform.localScale = handleScale;

        ringX.transform.rotation = Quaternion.Euler(0, 0, 90);
        ringY.transform.rotation = Quaternion.identity;
        ringZ.transform.rotation = Quaternion.Euler(90, 0, 0);
    }

    public void SetHandlesActive(bool active)
    {
        if (handlesRoot != null)
            handlesRoot.gameObject.SetActive(active);
    }

    


    private float NormalizeAngle(float angle)
    {
        angle = angle % 360f;
        if (angle > 180f) angle -= 360f;
        return angle;
    }

    private Vector3 NormalizeEuler(Vector3 euler)
    {
        return new Vector3(
            NormalizeAngle(euler.x),
            NormalizeAngle(euler.y),
            NormalizeAngle(euler.z)
        );
    }

    private void UpdateBindHelpUI()
    {
        if (HintUIManager.Instance == null) return;
        if (isDragging)
        {
            HintUIManager.Instance.ShowHint("Rotate", HintUIManager.Tips.Rotate);
        }
        else
        {
            HintUIManager.Instance.ClearHint("Rotate");
        }
    }

    public void DestroyHandles()
    {
        if (handlesRoot != null)
        {
            Destroy(handlesRoot.gameObject);
            handlesRoot = null;
        }

        leftClick?.Disable();
        shiftAction?.Disable();
        resetRotationAction?.Disable();
        scrollAction.Disable();

        if (dragAnchor != null)
        {
            Destroy(dragAnchor);
            dragAnchor = null;
        }
    }
}
