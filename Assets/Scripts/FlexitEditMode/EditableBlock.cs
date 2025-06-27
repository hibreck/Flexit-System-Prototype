using UnityEngine;

public class EditableBlock : MonoBehaviour
{
    public bool IsInEditMode { get; private set; } = false;

    private EditModeHighlighter highlighter;
    private FlexitAutoUV flexitAutoUV;

    private FlexitGizmoScale gizmoScale;
    private FlexitGizmoPlaneScale gizmoPlaneScale;
    private FlexitGizmoMove gizmoMove;
    private FlexitGizmoPlaneMove gizmoPlaneMove;
    private FlexitGizmoRotate gizmoRotate;
    private FlexitGizmoPivot gizmoPivot;
    public Vector3 savedPivotLocalOffset = Vector3.zero;

    private void Awake()
    {
        highlighter = GetComponent<EditModeHighlighter>();
    }

    public void EnterEditMode()
    {
        if (IsInEditMode) return;
        IsInEditMode = true;

        // Highlight
        if (highlighter == null)
        {
            highlighter = gameObject.AddComponent<EditModeHighlighter>();
            highlighter.Initialize();
        }
        highlighter.StartHighlight();

        // UV
        if (flexitAutoUV == null)
            flexitAutoUV = gameObject.AddComponent<FlexitAutoUV>();
        flexitAutoUV.enabled = true;

        // Ініціалізація гізмо-компонентів
        InitializeGizmoComponent(ref gizmoScale);
        InitializeGizmoComponent(ref gizmoPlaneScale);
        InitializeGizmoComponent(ref gizmoMove);
        InitializeGizmoComponent(ref gizmoPlaneMove);
        InitializeGizmoComponent(ref gizmoRotate);
        InitializeGizmoComponent(ref gizmoPivot);

        // Заборонити повторне редагування без виходу
        if (FlexitGizmoManager.Instance != null && FlexitGizmoManager.Instance.TargetIs(transform))
        {
            Debug.LogWarning("🟥 Спроба повторно увійти в редагування без виходу.");
            return;
        }


        Debug.Log($"{name} перейшов у режим редагування");
    }

    public void ExitEditMode()
    {
        IsInEditMode = false;

        // Highlight
        if (highlighter != null)
        {
            highlighter.StopHighlight();
            Destroy(highlighter);
            highlighter = null;
        }

        // UV
        if (flexitAutoUV != null)
        {
            flexitAutoUV.enabled = false;
            Destroy(flexitAutoUV);
            flexitAutoUV = null;
        }
        if (FlexitGizmoManager.Instance != null && FlexitGizmoManager.Instance.TargetIs(transform))
        {
            FlexitGizmoManager.Instance.SetTarget(null);
        }

        // Вимикаємо і видаляємо гізмо-компоненти
        DestroyIfExists(ref gizmoScale);
        DestroyIfExists(ref gizmoPlaneScale);
        DestroyIfExists(ref gizmoMove);
        DestroyIfExists(ref gizmoPlaneMove);
        DestroyIfExists(ref gizmoRotate);
        DestroyIfExists(ref gizmoPivot);

        
    }

    // Допоміжний метод для видалення компонентів та обнулення посилань
    private void DestroyIfExists<T>(ref T component) where T : Component
    {
        if (component != null)
        {
            if (component.gameObject == this.gameObject)
            {
                Destroy(component);
            }
            component = null;
        }
    }



    private void InitializeGizmoComponent<T>(ref T gizmo) where T : Component
    {
        if (gizmo == null)
        {
            gizmo = gameObject.AddComponent<T>();
            // ❌ Не викликаємо .Initialize() тут!
        }
    }

}
