using UnityEngine;

public class PlayerVision : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float visionDistance = 10f;
    [SerializeField] private OutlineManager outlineManager;

    public GameObject CurrentlyLookedObject { get; private set; }
    private GameObject lastLookedObject;

    private void Update()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        Debug.DrawRay(ray.origin, ray.direction * visionDistance, Color.green);

        if (Physics.Raycast(ray, out RaycastHit hit, visionDistance))
        {
            var target = hit.collider.gameObject;
            UpdateTarget(target);
        }
        else
        {
            UpdateTarget(null);
        }
    }

    private void UpdateTarget(GameObject newTarget)
    {
        if (newTarget != lastLookedObject)
        {
            // Прибираємо підсвітку з попереднього
            if (lastLookedObject != null)
            {
                var r = lastLookedObject.GetComponent<Renderer>();
                if (r != null)
                    outlineManager.RemoveOutline(r);
            }

            // Додаємо підсвітку на новий
            if (newTarget != null)
            {
                var r = newTarget.GetComponent<Renderer>();
                if (r != null)
                    outlineManager.AddOutline(r, Color.black);
            }

            lastLookedObject = newTarget;
            CurrentlyLookedObject = newTarget;
        }
    }
}
