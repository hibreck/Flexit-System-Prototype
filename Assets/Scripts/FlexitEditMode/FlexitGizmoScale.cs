using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;


public class FlexitGizmoScale : MonoBehaviour
{
    [SerializeField] private string redHandlePrefabPath = "Gismos_elements/Scale/RedScaleHandleX";
    [SerializeField] private string greenHandlePrefabPath = "Gismos_elements/Scale/GreenScaleHandleY";
    [SerializeField] private string blueHandlePrefabPath = "Gismos_elements/Scale/BlueScaleHandleZ";
    private GizmoInfoUI gizmoInfoUI;

    


    private GameObject redHandlePrefab;
    private GameObject greenHandlePrefab;
    private GameObject blueHandlePrefab;

    private GameObject redHandlePos;
    private GameObject redHandleNeg;
    private GameObject greenHandlePos;
    private GameObject greenHandleNeg;
    private GameObject blueHandlePos;
    private GameObject blueHandleNeg;

    private Transform target;
    public Transform handlesRoot;

    // InputAction 
    private InputAction handleControlAction;
    private InputAction ctrlAction;
    private InputAction shiftAction;
    private InputAction resetScaleAction;
    private InputAction scrollAction;




    // Для драггінгу
    private bool isDragging = false;
    private GameObject activeHandle = null;
    private Vector3 initialMouseWorldPos;
    private Vector3 initialTargetScale;
    private Vector3 initialTargetPosition;
    private Camera cam;

    private Vector3 customRight;
    private Vector3 customUp;
    private Vector3 customForward;
    public bool isScaleActive = false;
    private Vector3 lastScaleInfo = Vector3.zero;

    

    private void Start()
    {
        gizmoInfoUI = FindAnyObjectByType<GizmoInfoUI>();
    }
    public void Initialize(Transform targetBlock)
    {
        target = targetBlock;

        if (redHandlePrefab == null)
            redHandlePrefab = Resources.Load<GameObject>(redHandlePrefabPath);

        if (greenHandlePrefab == null)
            greenHandlePrefab = Resources.Load<GameObject>(greenHandlePrefabPath);

        if (blueHandlePrefab == null)
            blueHandlePrefab = Resources.Load<GameObject>(blueHandlePrefabPath);

        if (redHandlePos == null && redHandlePrefab != null)
        {
            redHandlePos = Instantiate(redHandlePrefab, handlesRoot);
            redHandlePos.name = "RedGizmoHandleX_Pos";
        }
        if (redHandleNeg == null && redHandlePrefab != null)
        {
            redHandleNeg = Instantiate(redHandlePrefab, handlesRoot);
            redHandleNeg.name = "RedGizmoHandleX_Neg";
        }
        if (greenHandlePos == null && greenHandlePrefab != null)
        {
            greenHandlePos = Instantiate(greenHandlePrefab, handlesRoot);
            greenHandlePos.name = "GreenGizmoHandleY_Pos";
        }
        if (greenHandleNeg == null && greenHandlePrefab != null)
        {
            greenHandleNeg = Instantiate(greenHandlePrefab, handlesRoot);
            greenHandleNeg.name = "GreenGizmoHandleY_Neg";
        }
        if (blueHandlePos == null && blueHandlePrefab != null)
        {
            blueHandlePos = Instantiate(blueHandlePrefab, handlesRoot);
            blueHandlePos.name = "BlueGizmoHandleZ_Pos";
        }
        if (blueHandleNeg == null && blueHandlePrefab != null)
        {
            blueHandleNeg = Instantiate(blueHandlePrefab, handlesRoot);
            blueHandleNeg.name = "BlueGizmoHandleZ_Neg";
        }

        SetHandlesActive(true);
        SetupInputActions();

        cam = Camera.main;

        
        if (handlesRoot != null && target != null)
        {
            handlesRoot.position = target.position;
            UpdateHandles();
        }
    }






    private void SetupInputActions()
    {
        handleControlAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        handleControlAction.Enable();

        ctrlAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/leftCtrl");
        ctrlAction.Enable();

        shiftAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/leftShift");
        shiftAction.Enable();

        resetScaleAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/r");
        resetScaleAction.Enable();

        scrollAction = new InputAction(type: InputActionType.PassThrough, binding: "<Mouse>/scroll");
        scrollAction.Enable();


    }

    private void OnDestroy()
    {
        handleControlAction?.Disable();
        ctrlAction?.Disable();
        shiftAction?.Disable();
        resetScaleAction?.Disable();

    }


    private void Update()
    {
        if (isScaleActive)
        {
            // Якщо зараз масштабування, не оновлюємо позицію хендлів тут
            return;
        }
        UpdateHandles();
        if (FlexitGizmoManager.Instance.CurrentMode != GizmoMode.Scale) return; // або Move / RotatePivot


        if (handleControlAction == null) return;

        if (handleControlAction.WasPressedThisFrame())
        {
            TryStartDrag();
        }
        else if (handleControlAction.IsPressed())
        {
            UpdateDrag();
        }
        else if (handleControlAction.WasReleasedThisFrame())
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
        UpdateCustomVectorsByYRotation();
        if (handlesRoot != null && target != null)
        {
            handlesRoot.position = target.position;
        }
        


    }
    private void UpdateCustomVectorsByYRotation()
    {
        float yRotation = target.eulerAngles.y;
        Quaternion yRot = Quaternion.Euler(0f, yRotation, 0f);
        customRight = yRot * Vector3.right;
        customUp = Vector3.up;  // завжди світовий up
        customForward = yRot * Vector3.forward;
    }
    private void TryStartDrag()
    {
        if (cam == null || isDragging) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Маска для шару GizmoHandle
        int gizmoLayerMask = 1 << LayerMask.NameToLayer("GizmoHandle");

        // RaycastAll дозволяє знайти хендл навіть через блоки
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, gizmoLayerMask);

        foreach (var hit in hits)
        {
            GameObject hitGO = hit.collider.gameObject;

            if (hitGO == redHandlePos || hitGO == redHandleNeg ||
                hitGO == greenHandlePos || hitGO == greenHandleNeg ||
                hitGO == blueHandlePos || hitGO == blueHandleNeg)
            {
                StartDrag(hitGO);
                return;
            }
        }
    }


    private void StartDrag(GameObject handle)
    {
        isDragging = true;
        activeHandle = handle;
       

        // Вимикаємо показ позиції UI (якщо це потрібно)
        if (gizmoInfoUI != null)
        {
            gizmoInfoUI.ClearExcept(GizmoInfoUI.GizmoInfoType.Scale);
            // або, якщо потрібно, додатково gizmoInfoUI.HidePositionInfo();
        }

        initialTargetScale = target.localScale;
        initialTargetPosition = target.position;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane dragPlane = new Plane(cam.transform.forward, target.position);
        float enter;
        if (dragPlane.Raycast(ray, out enter))
        {
            initialMouseWorldPos = ray.GetPoint(enter);
        }
        else
        {
            // Якщо не вдалось - просто поставимо початкову позицію миші як позицію об'єкта
            initialMouseWorldPos = target.position;
        }
    }

    private void UpdateDrag()
    {
        if (!isDragging || activeHandle == null || cam == null) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane dragPlane = new Plane(cam.transform.forward, target.position);
        if (!dragPlane.Raycast(ray, out float enter)) return;

        Vector3 currentMouseWorldPos = ray.GetPoint(enter);
        Vector3 deltaWorld = currentMouseWorldPos - initialMouseWorldPos;

        // Використовуємо локальні вектори target для врахування поворотів по будь-яких осях
        Vector3 right = target.right;
        Vector3 up = target.up;
        Vector3 forward = target.forward;

        Vector3 axis = Vector3.zero;
        float direction = 1f;

        if (activeHandle == redHandlePos)
        {
            axis = right;
            direction = 1f;
        }
        else if (activeHandle == redHandleNeg)
        {
            axis = right;
            direction = -1f;
        }
        else if (activeHandle == greenHandlePos)
        {
            axis = up;
            direction = 1f;
        }
        else if (activeHandle == greenHandleNeg)
        {
            axis = up;
            direction = -1f;
        }
        else if (activeHandle == blueHandlePos)
        {
            axis = forward;
            direction = 1f;
        }
        else if (activeHandle == blueHandleNeg)
        {
            axis = forward;
            direction = -1f;
        }
        else
        {
            return;
        }

        float deltaRaw = Vector3.Dot(deltaWorld, axis) * direction;

        float pixelStep = 4f / 64f;

        float delta;
        if (shiftAction != null && shiftAction.IsPressed())
        {
            delta = deltaRaw; // плавне масштабування
        }
        else
        {
            delta = Mathf.Round(deltaRaw / pixelStep) * pixelStep; // крокове масштабування
        }

        Vector3 newScale = initialTargetScale;

        if (ctrlAction != null && ctrlAction.IsPressed())
        {
            float minScale = 0.1f;

            float newX = Mathf.Max(minScale, initialTargetScale.x + delta);
            float newY = Mathf.Max(minScale, initialTargetScale.y + delta);
            float newZ = Mathf.Max(minScale, initialTargetScale.z + delta);

            newScale = new Vector3(newX, newY, newZ);

            target.localScale = newScale;
            // При пропорційному масштабуванні позицію не змінюємо
            return;
        }
        else
        {
            if (axis == right)
                newScale.x = Mathf.Max(0.1f, initialTargetScale.x + delta);
            else if (axis == up)
                newScale.y = Mathf.Max(0.1f, initialTargetScale.y + delta);
            else if (axis == forward)
                newScale.z = Mathf.Max(0.1f, initialTargetScale.z + delta);
        }

        Vector3 scaleDiff = newScale - initialTargetScale;

        // Зсув позиції — без множення на 0.5f, повний зсув у напрямку осі та активного хендла
        Vector3 positionOffset = axis * scaleDiff[(axis == right) ? 0 : (axis == up ? 1 : 2)] * 0.5f * direction;


        target.localScale = newScale;
        target.position = initialTargetPosition + positionOffset;

        if (ctrlAction == null || !ctrlAction.IsPressed())
        {
            handlesRoot.position = target.position;
        }
         if ((target.localScale - lastScaleInfo).sqrMagnitude > 0.0001f)
    {
        gizmoInfoUI.SetScaleInfo(target.localScale);
        lastScaleInfo = target.localScale;
    }
    }





    private void EndDrag()
    {
        isDragging = false;
        activeHandle = null;
        
        UpdateBindHelpUI();

    }
    public void HandleScroll(Vector2 scrollDelta)
    {
        // ❌ Alt затиснутий — виходимо
        if (Keyboard.current != null && Keyboard.current.altKey.isPressed)
            return;

        if (target == null || cam == null) return;

        float scrollAmount = scrollDelta.y;
        if (Mathf.Abs(scrollAmount) < 0.01f) return;

        // Оновлюємо кастомні осі
        UpdateCustomVectorsByYRotation();

        Vector3 camForward = cam.transform.forward.normalized;
        Vector3[] axes = new Vector3[] { customRight, customUp, customForward };
        int selectedAxis = -1;
        float maxDot = -1f;

        // Визначаємо, яка вісь найближча до погляду камери
        for (int i = 0; i < axes.Length; i++)
        {
            float dot = Mathf.Abs(Vector3.Dot(camForward, axes[i]));
            if (dot > maxDot)
            {
                maxDot = dot;
                selectedAxis = i;
            }
        }

        if (selectedAxis == -1) return;

        Vector3 axis = axes[selectedAxis].normalized;
        float direction = Mathf.Sign(Vector3.Dot(camForward, axis));

        float baseStep = 4f / 64f;
        float step = (shiftAction != null && shiftAction.IsPressed()) ? baseStep * 0.2f : baseStep;
        float delta = scrollAmount * step;

        Vector3 newScale = target.localScale;
        float minScale = 0.1f;

        if (ctrlAction != null && ctrlAction.IsPressed())
        {
            float scaleDelta = delta;

            float newX = Mathf.Max(minScale, newScale.x + scaleDelta);
            float newY = Mathf.Max(minScale, newScale.y + scaleDelta);
            float newZ = Mathf.Max(minScale, newScale.z + scaleDelta);

            newScale = new Vector3(newX, newY, newZ);
        }
        else
        {
            float scaleDelta = delta;

            switch (selectedAxis)
            {
                case 0:
                    newScale.x = Mathf.Max(minScale, newScale.x + scaleDelta);
                    break;
                case 1:
                    newScale.y = Mathf.Max(minScale, newScale.y + scaleDelta);
                    break;
                case 2:
                    newScale.z = Mathf.Max(minScale, newScale.z + scaleDelta);
                    break;
            }
        }

        Vector3 scaleDiff = newScale - target.localScale;
        float offset = 0.5f * scaleDiff[selectedAxis] * direction;
        Vector3 positionOffset = axis * offset;

        target.localScale = newScale;
        target.position += positionOffset;
        handlesRoot.position = target.position;

        gizmoInfoUI?.SetScaleInfo(target.localScale);
    }



    private void UpdateHandles()
    {
        if (handlesRoot == null || cam == null || target == null) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 center = handlesRoot.position;
        Vector3 camPos = cam.transform.position;

        float distance = Vector3.Distance(camPos, center);
        float baseDistance = 5f;
        float scaleFactor = Mathf.Max(distance / baseDistance, 0.5f);

        float baseHandleOffset = 0.5f;
        float baseHandleScale = 0.1f;

        // Повні локальні осі (враховують усі повороти)
        Vector3 right = target.right.normalized;
        Vector3 up = target.up.normalized;
        Vector3 forward = target.forward.normalized;

        Vector3 handleScale = Vector3.one * baseHandleScale * scaleFactor / handlesRoot.localScale.x;
        Quaternion rotation = target.rotation;

        Vector3 rightPos = center + right * (baseHandleOffset * scaleFactor);
        Vector3 leftPos = center - right * (baseHandleOffset * scaleFactor);

        Vector3 upPos = center + up * (baseHandleOffset * scaleFactor);
        Vector3 downPos = center - up * (baseHandleOffset * scaleFactor);

        Vector3 forwardPos = center + forward * (baseHandleOffset * scaleFactor);
        Vector3 backPos = center - forward * (baseHandleOffset * scaleFactor);

        Vector3 dirToCam = (camPos - center).normalized;

        float dotRight = Vector3.Dot(dirToCam, right);
        float dotLeft = Vector3.Dot(dirToCam, -right);
        float dotUp = Vector3.Dot(dirToCam, up);
        float dotDown = Vector3.Dot(dirToCam, -up);
        float dotForward = Vector3.Dot(dirToCam, forward);
        float dotBack = Vector3.Dot(dirToCam, -forward);

        UpdateHandle(redHandlePos, dotRight, rightPos, right, up, handleScale);
        UpdateHandle(redHandleNeg, dotLeft, leftPos, -right, up, handleScale);

        UpdateHandle(greenHandlePos, dotUp, upPos, up, forward, handleScale);
        UpdateHandle(greenHandleNeg, dotDown, downPos, -up, forward, handleScale);

        UpdateHandle(blueHandlePos, dotForward, forwardPos, forward, up, handleScale);
        UpdateHandle(blueHandleNeg, dotBack, backPos, -forward, up, handleScale);

    }

    private void UpdateHandle(GameObject handle, float dot, Vector3 position, Vector3 axis, Vector3 upHint, Vector3 scale)
    {
        if (handle == null) return;
        bool visible = dot > 0f;
        handle.SetActive(visible);
        if (!visible) return;

        handle.transform.position = position;
        handle.transform.rotation = Quaternion.LookRotation(axis, upHint); // ключовий момент!
        handle.transform.localScale = scale;
    }







    public void SetHandlesActive(bool active)
    {
        if (redHandlePos != null)
            redHandlePos.SetActive(active);
        if (redHandleNeg != null)
            redHandleNeg.SetActive(active);
        if (greenHandlePos != null)
            greenHandlePos.SetActive(active);
        if (greenHandleNeg != null)
            greenHandleNeg.SetActive(active);
        if (blueHandlePos != null)
            blueHandlePos.SetActive(active);
        if (blueHandleNeg != null)
            blueHandleNeg.SetActive(active);

        if (handlesRoot != null)
            handlesRoot.gameObject.SetActive(active);
    }

    


    private void UpdateBindHelpUI()
    {
        if (HintUIManager.Instance == null) return;
        if (isDragging)
        {
            HintUIManager.Instance.ShowHint("Scale", HintUIManager.Tips.Scale);
        }
        else
        {
            HintUIManager.Instance.ClearHint("Scale");
        }
    }



    public void ResetScaleToNearestPixelStep()
    {
        Debug.Log($"Reset scale action triggered in {this.GetType().Name} at frame {Time.frameCount}");
        if (target == null) return;

        float pixelStep = 4f / 64f; // або 0.06f, якщо ти округляєш

        Vector3 scale = target.localScale;

        scale.x = Mathf.Round(scale.x / pixelStep) * pixelStep;
        scale.y = Mathf.Round(scale.y / pixelStep) * pixelStep;
        scale.z = Mathf.Round(scale.z / pixelStep) * pixelStep;

        target.localScale = scale;
    }




    public void DestroyHandles()
    {
        if (redHandlePos != null)
        {
            Destroy(redHandlePos);
            redHandlePos = null;
        }
        if (redHandleNeg != null)
        {
            Destroy(redHandleNeg);
            redHandleNeg = null;
        }
        if (greenHandlePos != null)
        {
            Destroy(greenHandlePos);
            greenHandlePos = null;
        }
        if (greenHandleNeg != null)
        {
            Destroy(greenHandleNeg);
            greenHandleNeg = null;
        }
        if (blueHandlePos != null)
        {
            Destroy(blueHandlePos);
            blueHandlePos = null;
        }
        if (blueHandleNeg != null)
        {
            Destroy(blueHandleNeg);
            blueHandleNeg = null;
        }

        

        handleControlAction?.Disable();
        ctrlAction?.Disable();
    }
}
