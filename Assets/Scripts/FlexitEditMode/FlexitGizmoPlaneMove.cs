using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class FlexitGizmoPlaneMove : MonoBehaviour
{
    [Header("Resources Paths")]
    [SerializeField] private string xyHandlePath = "Gismos_elements/MovePlane/HandleXY";
    [SerializeField] private string xzHandlePath = "Gismos_elements/MovePlane/HandleXZ";
    [SerializeField] private string yzHandlePath = "Gismos_elements/MovePlane/HandleYZ";

    private GameObject handleXY_PXPY, handleXY_PXNY, handleXY_NXPY, handleXY_NXNY;
    private GameObject handleXZ_PXPZ, handleXZ_PXNZ, handleXZ_NXPZ, handleXZ_NXNZ;
    private GameObject handleYZ_PYPZ, handleYZ_PYNZ, handleYZ_NYPZ, handleYZ_NYNZ;

    private GameObject xyHandlePrefab, xzHandlePrefab, yzHandlePrefab;
    private GameObject activeHandle;

    private Transform target;
    public Transform handlesRoot;

    private InputAction moveAction;
    private InputAction shiftAction;
    private InputAction ctrlAction;

    private Camera cam;
    private Plane dragPlane;
    private Vector3 initialMouseWorldPos;
    private Vector3 initialTargetPos;
    private Vector3 initialHandlesRootPos;

    private Vector3 customRight;
    private Vector3 customForward;

    private bool isDragging;
    private bool isPivotDragged;

    private GizmoInfoUI gizmoInfoUI;

    private Transform cubeGizmoRoot; // зовнішній куб для локального офсету півоту
    private Vector3 pivotLocalOffset = Vector3.zero;

    public void Initialize(Transform targetBlock)
    {
        target = targetBlock;
        cam = Camera.main;

        gizmoInfoUI = FindAnyObjectByType<GizmoInfoUI>();

        xyHandlePrefab = Resources.Load<GameObject>(xyHandlePath);
        xzHandlePrefab = Resources.Load<GameObject>(xzHandlePath);
        yzHandlePrefab = Resources.Load<GameObject>(yzHandlePath);
        


        CreateHandles();

        SetupInput();
        UpdateHandles();
        InitializeCustomVectors();
        

        GameObject rootObj = GameObject.Find("Cube_GizmoRoot");
        if (rootObj != null)
            cubeGizmoRoot = rootObj.transform;
    }
    


    private void CreateHandles()
    {
        // Якщо вже створені — не створюємо вдруге
        if (handleXY_PXPY != null) return;

        handleXY_PXPY = Instantiate(xyHandlePrefab, handlesRoot); handleXY_PXPY.name += "_PXPY";
        handleXY_PXNY = Instantiate(xyHandlePrefab, handlesRoot); handleXY_PXNY.name += "_PXNY";
        handleXY_NXPY = Instantiate(xyHandlePrefab, handlesRoot); handleXY_NXPY.name += "_NXPY";
        handleXY_NXNY = Instantiate(xyHandlePrefab, handlesRoot); handleXY_NXNY.name += "_NXNY";

        handleXZ_PXPZ = Instantiate(xzHandlePrefab, handlesRoot); handleXZ_PXPZ.name += "_PXPZ";
        handleXZ_PXNZ = Instantiate(xzHandlePrefab, handlesRoot); handleXZ_PXNZ.name += "_PXNZ";
        handleXZ_NXPZ = Instantiate(xzHandlePrefab, handlesRoot); handleXZ_NXPZ.name += "_NXPZ";
        handleXZ_NXNZ = Instantiate(xzHandlePrefab, handlesRoot); handleXZ_NXNZ.name += "_NXNZ";

        handleYZ_PYPZ = Instantiate(yzHandlePrefab, handlesRoot); handleYZ_PYPZ.name += "_PYPZ";
        handleYZ_PYNZ = Instantiate(yzHandlePrefab, handlesRoot); handleYZ_PYNZ.name += "_PYNZ";
        handleYZ_NYPZ = Instantiate(yzHandlePrefab, handlesRoot); handleYZ_NYPZ.name += "_NYPZ";
        handleYZ_NYNZ = Instantiate(yzHandlePrefab, handlesRoot); handleYZ_NYNZ.name += "_NYNZ";
    }

    private void SetupInput()
    {
        moveAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        shiftAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/leftShift");
        ctrlAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/leftCtrl");

        moveAction.Enable();
        shiftAction.Enable();
        ctrlAction.Enable();
    }

    private void Update()
    {
        UpdateHandles();
        UpdateCustomVectorsByYRotation();

        if (cubeGizmoRoot != null && !isPivotDragged)
        {
            handlesRoot.position = cubeGizmoRoot.position + cubeGizmoRoot.TransformVector(pivotLocalOffset);
        }

        if (moveAction.WasPressedThisFrame()) TryStartDrag();
        else if (moveAction.IsPressed())
        {
            UpdateDrag();
            UpdateBindHelpUI();
            
        }
        else if (moveAction.WasReleasedThisFrame())
        {
            EndDrag();
            UpdateBindHelpUI();
            
        }
    }

    private void InitializeCustomVectors()
    {
        Quaternion yRotation = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
        customRight = yRotation * Vector3.right;
        customForward = yRotation * Vector3.forward;
    }

    private void UpdateCustomVectorsByYRotation()
    {
        Quaternion yRotation = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
        customRight = yRotation * Vector3.right;
        customForward = yRotation * Vector3.forward;
    }

    public void UpdateHandles()
    {
        if (handlesRoot == null || cam == null) return;

        Vector3 rootPos = handlesRoot.position;
        Vector3 camPos = cam.transform.position;
        float distance = Vector3.Distance(camPos, rootPos);
        float baseDistance = 5f;
        float scaleFactor = Mathf.Max(distance / baseDistance, 0.5f);

        float baseHandleScale = 0.032f;
        float baseHandleOffset = 0.5f;

        Vector3 handleScale = Vector3.one * baseHandleScale * scaleFactor / handlesRoot.localScale.x;
        Vector3 dirToCam = (camPos - rootPos).normalized;

        Quaternion onlyYRotation = Quaternion.Euler(0f, handlesRoot.rotation.eulerAngles.y, 0f);

        Vector3 right = onlyYRotation * Vector3.right;
        Vector3 up = Vector3.up;
        Vector3 forward = onlyYRotation * Vector3.forward;

        float dotX = Vector3.Dot(dirToCam, right);
        float dotY = Vector3.Dot(dirToCam, up);
        float dotZ = Vector3.Dot(dirToCam, forward);

        Quaternion rotXY = Quaternion.LookRotation(up, forward);
        Quaternion rotXZ = Quaternion.LookRotation(forward, up);
        Quaternion rotYZ = Quaternion.LookRotation(up, right);

        float offset = baseHandleOffset * scaleFactor;

        SetHandle(handleXY_PXPY, dotX > 0 && dotY > 0, rootPos + right * offset + up * offset, rotXY, handleScale);
        SetHandle(handleXY_PXNY, dotX > 0 && dotY < 0, rootPos + right * offset - up * offset, rotXY, handleScale);
        SetHandle(handleXY_NXPY, dotX < 0 && dotY > 0, rootPos - right * offset + up * offset, rotXY, handleScale);
        SetHandle(handleXY_NXNY, dotX < 0 && dotY < 0, rootPos - right * offset - up * offset, rotXY, handleScale);

        SetHandle(handleXZ_PXPZ, dotX > 0 && dotZ > 0, rootPos + right * offset + forward * offset, rotXZ, handleScale);
        SetHandle(handleXZ_PXNZ, dotX > 0 && dotZ < 0, rootPos + right * offset - forward * offset, rotXZ, handleScale);
        SetHandle(handleXZ_NXPZ, dotX < 0 && dotZ > 0, rootPos - right * offset + forward * offset, rotXZ, handleScale);
        SetHandle(handleXZ_NXNZ, dotX < 0 && dotZ < 0, rootPos - right * offset - forward * offset, rotXZ, handleScale);

        SetHandle(handleYZ_PYPZ, dotY > 0 && dotZ > 0, rootPos + up * offset + forward * offset, rotYZ, handleScale);
        SetHandle(handleYZ_PYNZ, dotY > 0 && dotZ < 0, rootPos + up * offset - forward * offset, rotYZ, handleScale);
        SetHandle(handleYZ_NYPZ, dotY < 0 && dotZ > 0, rootPos - up * offset + forward * offset, rotYZ, handleScale);
        SetHandle(handleYZ_NYNZ, dotY < 0 && dotZ < 0, rootPos - up * offset - forward * offset, rotYZ, handleScale);
    }

    private void SetHandle(GameObject handle, bool active, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if (handle == null) return;
        handle.SetActive(active);
        if (active)
        {
            handle.transform.position = position;
            handle.transform.rotation = rotation;
            handle.transform.localScale = scale;
        }
    }

    private bool RaycastPlaneBothSides(Plane plane, Ray ray, out float enter)
    {
        if (plane.Raycast(ray, out enter))
            return true;

        Plane flippedPlane = new Plane(-plane.normal, plane.distance);
        return flippedPlane.Raycast(ray, out enter);
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

            if (go == handleXY_PXPY || go == handleXY_PXNY || go == handleXY_NXPY || go == handleXY_NXNY ||
                go == handleXZ_PXPZ || go == handleXZ_PXNZ || go == handleXZ_NXPZ || go == handleXZ_NXNZ ||
                go == handleYZ_PYPZ || go == handleYZ_PYNZ || go == handleYZ_NYPZ || go == handleYZ_NYNZ)
            {
                activeHandle = go;
                isDragging = true;
                isPivotDragged = ctrlAction != null && ctrlAction.IsPressed();

                initialTargetPos = target.position;
                initialHandlesRootPos = handlesRoot.position;

                if (cubeGizmoRoot == null)
                {
                    GameObject rootObj = GameObject.Find("Cube_GizmoRoot");
                    if (rootObj != null)
                        cubeGizmoRoot = rootObj.transform;
                }

                Vector3 planeNormal;

                if (go.name.Contains("XY"))
                    planeNormal = customForward;
                else if (go.name.Contains("XZ"))
                    planeNormal = Vector3.up;
                else if (go.name.Contains("YZ"))
                    planeNormal = customRight;
                else
                    return;

                dragPlane = new Plane(planeNormal, target.position);

                if (RaycastPlaneBothSides(dragPlane, ray, out float enter))
                    initialMouseWorldPos = ray.GetPoint(enter);
                else
                    initialMouseWorldPos = target.position;

                return;
            }
        }
    }

    private void UpdateDrag()
    {
        if (!isDragging || activeHandle == null || cam == null) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!RaycastPlaneBothSides(dragPlane, ray, out float enter)) return;

        Vector3 currentMouseWorldPos = ray.GetPoint(enter);
        Vector3 rawDelta = currentMouseWorldPos - initialMouseWorldPos;
        Vector3 delta = Vector3.ProjectOnPlane(rawDelta, dragPlane.normal);

        float pixelStep = 4f / 64f;
        Vector3 offset = Vector3.zero;

        if (activeHandle.name.Contains("XY"))
        {
            Vector3 localRight = customRight;
            Vector3 localUp = Vector3.up;

            float deltaX = Vector3.Dot(delta, localRight);
            float deltaY = Vector3.Dot(delta, localUp);

            if (shiftAction != null && shiftAction.IsPressed())
            {
                offset = localRight * deltaX + localUp * deltaY;
            }
            else
            {
                deltaX = Mathf.Round(deltaX / pixelStep) * pixelStep;
                deltaY = Mathf.Round(deltaY / pixelStep) * pixelStep;
                offset = localRight * deltaX + localUp * deltaY;
            }
        }
        else if (activeHandle.name.Contains("YZ"))
        {
            Vector3 localForward = customForward;
            Vector3 localUp = Vector3.up;

            float deltaZ = Vector3.Dot(delta, localForward);
            float deltaY = Vector3.Dot(delta, localUp);

            if (shiftAction != null && shiftAction.IsPressed())
            {
                offset = localForward * deltaZ + localUp * deltaY;
            }
            else
            {
                deltaZ = Mathf.Round(deltaZ / pixelStep) * pixelStep;
                deltaY = Mathf.Round(deltaY / pixelStep) * pixelStep;
                offset = localForward * deltaZ + localUp * deltaY;
            }
        }
        else if (activeHandle.name.Contains("XZ"))
        {
            Vector3 worldRight = Vector3.right;
            Vector3 worldForward = Vector3.forward;

            delta.y = 0f;
            float deltaX = Vector3.Dot(delta, worldRight);
            float deltaZ = Vector3.Dot(delta, worldForward);

            if (shiftAction != null && shiftAction.IsPressed())
            {
                offset = worldRight * deltaX + worldForward * deltaZ;
            }
            else
            {
                deltaX = Mathf.Round(deltaX / pixelStep) * pixelStep;
                deltaZ = Mathf.Round(deltaZ / pixelStep) * pixelStep;
                offset = worldRight * deltaX + worldForward * deltaZ;
            }
        }

        if (isPivotDragged && cubeGizmoRoot != null)
        {
            handlesRoot.position = initialHandlesRootPos + offset;
            pivotLocalOffset = cubeGizmoRoot.InverseTransformPoint(handlesRoot.position);
        }
        else
        {
            Vector3 newPos = initialTargetPos + offset;
            target.position = newPos;
            handlesRoot.position = newPos;
        }
        gizmoInfoUI?.SetPositionInfo(target.position);
    }

    private void EndDrag()
    {
        isDragging = false;
        activeHandle = null;
        isPivotDragged = false;
        UpdateBindHelpUI();
        
    }

    

    private void UpdateBindHelpUI()
    {
        if (HintUIManager.Instance == null) return;
        if (isDragging)
        {
            HintUIManager.Instance.ShowHint("PlaneMove", HintUIManager.Tips.PlaneMove);
        }
        else
        {
            HintUIManager.Instance.ClearHint("PlaneMove");
        }
    }

    public void DestroyHandles()
    {
        DestroyIfNotNull(handleXY_PXPY);
        DestroyIfNotNull(handleXY_PXNY);
        DestroyIfNotNull(handleXY_NXPY);
        DestroyIfNotNull(handleXY_NXNY);

        DestroyIfNotNull(handleXZ_PXPZ);
        DestroyIfNotNull(handleXZ_PXNZ);
        DestroyIfNotNull(handleXZ_NXPZ);
        DestroyIfNotNull(handleXZ_NXNZ);

        DestroyIfNotNull(handleYZ_PYPZ);
        DestroyIfNotNull(handleYZ_PYNZ);
        DestroyIfNotNull(handleYZ_NYPZ);
        DestroyIfNotNull(handleYZ_NYNZ);

        moveAction?.Disable();
        shiftAction?.Disable();
        ctrlAction?.Disable();
    }

    private void DestroyIfNotNull(GameObject go)
    {
        if (go != null)
        {
            Destroy(go);
        }
    }

    public void SetHandlesActive(bool active)
    {
        if (handlesRoot != null)
            handlesRoot.gameObject.SetActive(active);
    }
}
