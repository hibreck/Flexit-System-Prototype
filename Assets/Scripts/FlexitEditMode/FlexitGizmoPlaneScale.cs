using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class FlexitGizmoPlaneScale : MonoBehaviour
{
    [Header("Handle Prefabs Paths")]
    [SerializeField] private string xyHandlePath = "Gismos_elements/ScalePlane/ScaleHandleXY";
    [SerializeField] private string xzHandlePath = "Gismos_elements/ScalePlane/ScaleHandleXZ";
    [SerializeField] private string yzHandlePath = "Gismos_elements/ScalePlane/ScaleHandleYZ";

    private GameObject[] handlesXY = new GameObject[4];
    private GameObject[] handlesXZ = new GameObject[4];
    private GameObject[] handlesYZ = new GameObject[4];

    private GameObject xyPrefab, xzPrefab, yzPrefab;
    private GameObject activeHandle;

    private Transform target;
    public Transform handlesRoot;

    private InputAction moveAction;
    private InputAction controlAction;
    private InputAction ctrlAction;
    private InputAction shiftAction;

    private Camera cam;
    private Plane dragPlane;
    private Vector3 initialMouseWorldPos;
    private Vector3 initialTargetScale;
    private Vector3 initialTargetPosition;
    private bool isDragging = false;
    private Vector3 customRight;
    private Vector3 customUp;
    private Vector3 customForward;



    private GizmoInfoUI gizmoInfoUI;



    private Vector3 handleDirection = Vector3.one; // +1 по кожній осі за замовчуванням


    public void Initialize(Transform targetBlock)
    {
        if (handlesXY[0] != null) return; // ❗ Уникнути дублювання

        target = targetBlock;
        cam = Camera.main;

        gizmoInfoUI = FindAnyObjectByType<GizmoInfoUI>();

        xyPrefab = Resources.Load<GameObject>(xyHandlePath);
        xzPrefab = Resources.Load<GameObject>(xzHandlePath);
        yzPrefab = Resources.Load<GameObject>(yzHandlePath);

        for (int i = 0; i < 4; i++)
        {
            handlesXY[i] = Instantiate(xyPrefab, handlesRoot);
            handlesXY[i].name = $"HandleXY_{i}";

            handlesXZ[i] = Instantiate(xzPrefab, handlesRoot);
            handlesXZ[i].name = $"HandleXZ_{i}";

            handlesYZ[i] = Instantiate(yzPrefab, handlesRoot);
            handlesYZ[i].name = $"HandleYZ_{i}";
        }

        SetupInput();
        UpdateHandles();
    }



    private void SetupInput()
    {
        moveAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        controlAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/leftCtrl");
        shiftAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/leftShift");

        moveAction.Enable();
        controlAction.Enable();
        shiftAction.Enable();
    }

    private void Update()
    {
        UpdateHandles();

        if (moveAction.WasPressedThisFrame()) TryStartDrag();
        else if (moveAction.IsPressed()) UpdateDrag();
        else if (moveAction.WasReleasedThisFrame()) EndDrag();
        
        UpdateBindHelpUI();
        
    }
    
    public void UpdateHandles()
    {
        if (handlesRoot == null) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 center = handlesRoot.position;
        Vector3 camPos = cam.transform.position;
        Vector3 camDir = (camPos - center).normalized;

        float distance = Vector3.Distance(camPos, center);
        float baseDistance = 5f;
        float scaleFactor = Mathf.Max(distance / baseDistance, 0.5f);

        float offset = 0.5f * scaleFactor;
        Vector3 handleScale = Vector3.one * 0.1f * scaleFactor;

        // Використовуємо локальні осі target
        Vector3 right = target.right;
        Vector3 up = target.up;
        Vector3 forward = target.forward;

        float dotX = Vector3.Dot(camDir, right);
        float dotY = Vector3.Dot(camDir, up);
        float dotZ = Vector3.Dot(camDir, forward);

        Quaternion rotXY = Quaternion.LookRotation(forward, up);
        Quaternion rotXZ = Quaternion.LookRotation(up, forward);
        Quaternion rotYZ = Quaternion.LookRotation(right, up);

        // XY
        SetHandle(handlesXY[0], dotX > 0 && dotY > 0, center + right * offset + up * offset, rotXY, handleScale);
        SetHandle(handlesXY[1], dotX > 0 && dotY < 0, center + right * offset - up * offset, rotXY, handleScale);
        SetHandle(handlesXY[2], dotX < 0 && dotY > 0, center - right * offset + up * offset, rotXY, handleScale);
        SetHandle(handlesXY[3], dotX < 0 && dotY < 0, center - right * offset - up * offset, rotXY, handleScale);

        // XZ
        SetHandle(handlesXZ[0], dotX > 0 && dotZ > 0, center + right * offset + forward * offset, rotXZ, handleScale);
        SetHandle(handlesXZ[1], dotX > 0 && dotZ < 0, center + right * offset - forward * offset, rotXZ, handleScale);
        SetHandle(handlesXZ[2], dotX < 0 && dotZ > 0, center - right * offset + forward * offset, rotXZ, handleScale);
        SetHandle(handlesXZ[3], dotX < 0 && dotZ < 0, center - right * offset - forward * offset, rotXZ, handleScale);

        // YZ
        SetHandle(handlesYZ[0], dotY > 0 && dotZ > 0, center + up * offset + forward * offset, rotYZ, handleScale);
        SetHandle(handlesYZ[1], dotY > 0 && dotZ < 0, center + up * offset - forward * offset, rotYZ, handleScale);
        SetHandle(handlesYZ[2], dotY < 0 && dotZ > 0, center - up * offset + forward * offset, rotYZ, handleScale);
        SetHandle(handlesYZ[3], dotY < 0 && dotZ < 0, center - up * offset - forward * offset, rotYZ, handleScale);
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

    private void TryStartDrag()
    {
        if (cam == null || isDragging) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        int gizmoLayerMask = 1 << LayerMask.NameToLayer("GizmoHandle");

        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, gizmoLayerMask);

        foreach (var hit in hits)
        {
            GameObject go = hit.collider.gameObject;

            if (System.Array.IndexOf(handlesXY, go) >= 0 ||
                System.Array.IndexOf(handlesXZ, go) >= 0 ||
                System.Array.IndexOf(handlesYZ, go) >= 0)
            {
                activeHandle = go;
                isDragging = true;
                initialTargetScale = target.localScale;
                initialTargetPosition = target.position;

                string name = activeHandle.name;

                // Локальні осі
                Vector3 localForward = target.forward;
                Vector3 localUp = target.up;
                Vector3 localRight = target.right;

                if (name.Contains("XY"))
                {
                    dragPlane = new Plane(localForward, target.position);
                    if (name.EndsWith("0")) handleDirection = new Vector3(+1, +1, 0);
                    else if (name.EndsWith("1")) handleDirection = new Vector3(+1, -1, 0);
                    else if (name.EndsWith("2")) handleDirection = new Vector3(-1, +1, 0);
                    else if (name.EndsWith("3")) handleDirection = new Vector3(-1, -1, 0);
                }
                else if (name.Contains("XZ"))
                {
                    dragPlane = new Plane(localUp, target.position);
                    if (name.EndsWith("0")) handleDirection = new Vector3(+1, 0, +1);
                    else if (name.EndsWith("1")) handleDirection = new Vector3(+1, 0, -1);
                    else if (name.EndsWith("2")) handleDirection = new Vector3(-1, 0, +1);
                    else if (name.EndsWith("3")) handleDirection = new Vector3(-1, 0, -1);
                }
                else if (name.Contains("YZ"))
                {
                    dragPlane = new Plane(localRight, target.position);
                    if (name.EndsWith("0")) handleDirection = new Vector3(0, +1, +1);
                    else if (name.EndsWith("1")) handleDirection = new Vector3(0, +1, -1);
                    else if (name.EndsWith("2")) handleDirection = new Vector3(0, -1, +1);
                    else if (name.EndsWith("3")) handleDirection = new Vector3(0, -1, -1);
                }

                if (dragPlane.Raycast(ray, out float enter))
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
        if (!dragPlane.Raycast(ray, out float enter)) return;

        Vector3 currentMouseWorldPos = ray.GetPoint(enter);
        Vector3 deltaWorld = currentMouseWorldPos - initialMouseWorldPos;

        // Отримуємо локальні осі target
        Vector3 right = target.right;
        Vector3 up = target.up;
        Vector3 forward = target.forward;

        // Розклад дельти по осях залежно від напряму хендлу
        Vector3 deltaPerAxis = new Vector3(
            handleDirection.x != 0 ? Vector3.Dot(deltaWorld, right) * Mathf.Sign(handleDirection.x) : 0,
            handleDirection.y != 0 ? Vector3.Dot(deltaWorld, up) * Mathf.Sign(handleDirection.y) : 0,
            handleDirection.z != 0 ? Vector3.Dot(deltaWorld, forward) * Mathf.Sign(handleDirection.z) : 0
        );

        float pixelStep = 4f / 64f;
        float minScale = 0.1f;

        bool shiftHeld = shiftAction != null && shiftAction.IsPressed();
        bool ctrlHeld = controlAction != null && controlAction.IsPressed();

        // Якщо Shift не натиснуто — прив’язка до кроку
        if (!shiftHeld)
        {
            deltaPerAxis.x = Mathf.Round(deltaPerAxis.x / pixelStep) * pixelStep;
            deltaPerAxis.y = Mathf.Round(deltaPerAxis.y / pixelStep) * pixelStep;
            deltaPerAxis.z = Mathf.Round(deltaPerAxis.z / pixelStep) * pixelStep;
        }

        Vector3 newScale = initialTargetScale;

        if (ctrlHeld)
        {
            // Рівномірне масштабування
            float maxAxisDelta = Mathf.Max(
                Mathf.Abs(deltaPerAxis.x),
                Mathf.Abs(deltaPerAxis.y),
                Mathf.Abs(deltaPerAxis.z)
            );

            float signedDelta = maxAxisDelta * Mathf.Sign(
                deltaPerAxis.x != 0 ? deltaPerAxis.x :
                deltaPerAxis.y != 0 ? deltaPerAxis.y :
                deltaPerAxis.z
            );

            Vector3 uniformDelta = new Vector3(
                handleDirection.x != 0 ? signedDelta : 0,
                handleDirection.y != 0 ? signedDelta : 0,
                handleDirection.z != 0 ? signedDelta : 0
            );

            newScale += uniformDelta;
        }
        else
        {
            // Звичайне масштабування
            newScale += deltaPerAxis;
        }

        // Обмеження мінімального масштабу
        newScale.x = Mathf.Max(minScale, newScale.x);
        newScale.y = Mathf.Max(minScale, newScale.y);
        newScale.z = Mathf.Max(minScale, newScale.z);

        // Обчислюємо дельту масштабу
        Vector3 scaleDelta = newScale - initialTargetScale;

        // Центр зміщується в протилежний бік масштабування
        Vector3 positionOffset =
            right * scaleDelta.x * 0.5f * Mathf.Sign(handleDirection.x) +
            up * scaleDelta.y * 0.5f * Mathf.Sign(handleDirection.y) +
            forward * scaleDelta.z * 0.5f * Mathf.Sign(handleDirection.z);

        target.localScale = newScale;
        target.position = initialTargetPosition + positionOffset;

        if (controlAction == null || !controlAction.IsPressed())
        {
            handlesRoot.position = target.position;
        }
        gizmoInfoUI?.SetScaleInfo(target.position);
    }



    private void EndDrag()
    {
        isDragging = false;
        activeHandle = null;
    }

    


    private void UpdateBindHelpUI()
    {
        if (HintUIManager.Instance == null) return;
        if (isDragging)
        {
            HintUIManager.Instance.ShowHint("PlaneScale", HintUIManager.Tips.PlaneScale);
        }
        else
        {
            HintUIManager.Instance.ClearHint("PlaneScale");
        }
    }

    public void DestroyHandles()
    {
        if (handlesXY != null)
        {
            for (int i = 0; i < handlesXY.Length; i++)
            {
                if (handlesXY[i] != null)
                    Destroy(handlesXY[i]);
            }
        }

        if (handlesXZ != null)
        {
            for (int i = 0; i < handlesXZ.Length; i++)
            {
                if (handlesXZ[i] != null)
                    Destroy(handlesXZ[i]);
            }
        }

        if (handlesYZ != null)
        {
            for (int i = 0; i < handlesYZ.Length; i++)
            {
                if (handlesYZ[i] != null)
                    Destroy(handlesYZ[i]);
            }
        }

        // Видаляємо кореневий об'єкт, якщо він існує
        if (handlesRoot != null)
        {
            Destroy(handlesRoot.gameObject);
            handlesRoot = null;
        }

        // Відключення InputAction'ів
        moveAction?.Disable();
        controlAction?.Disable();
        shiftAction?.Disable();
    }


    public void SetHandlesActive(bool active)
    {
        if (handlesRoot != null)
            handlesRoot.gameObject.SetActive(active);
    }
}
