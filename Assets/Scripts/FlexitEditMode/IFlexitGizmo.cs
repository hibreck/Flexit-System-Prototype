using UnityEngine;

public interface IFlexitGizmo
{
    void Initialize(Transform target);
    void SetHandlesActive(bool active);
    void SetInputEnabled(bool enabled);
    void DestroyHandles();
}
