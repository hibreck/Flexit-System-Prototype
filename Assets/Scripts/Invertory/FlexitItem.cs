using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


[CreateAssetMenu(menuName = "Items/FlexitItem")]
public class FlexitItem : Item
{
    [Header("Flexit Settings")]
    public GameObject flexitPrefab;

    public override Dictionary<string, int> GetStats()
    {
        // Можна додати кастомні характеристики блоку
        return new Dictionary<string, int>
        {
            { "buildable", 1 } // для UI-підказок, умов у грі тощо
        };
    }

    public override void Use(GameObject user)
    {
        // ⛔ Не ставити, якщо Alt затиснутий
        if (Keyboard.current != null &&
            (Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed))
        {
            Debug.Log("Alt затиснутий — блок не буде встановлено.");
            return;
        }

        if (flexitPrefab == null)
        {
            Debug.LogWarning("flexitPrefab is not assigned.");
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("No main camera found.");
            return;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, 5f))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                Debug.Log("Не можна встановити на гравця");
                return;
            }

            GameObject instance = Object.Instantiate(flexitPrefab);
            instance.name = flexitPrefab.name;

            Collider instanceCollider = instance.GetComponent<Collider>();

            Vector3 placePos;
            if (instanceCollider != null)
            {
                Vector3 pointOnSurface = instanceCollider.ClosestPoint(hit.point - hit.normal * 10f);
                Vector3 offset = instance.transform.position - pointOnSurface;
                placePos = hit.point + offset;
            }
            else
            {
                placePos = hit.point;
            }

            // Визначаємо горизонтальний напрямок камери (ігноруємо Y)
            Vector3 forward = cam.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;

            Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);

            instance.transform.position = placePos;
            instance.transform.rotation = rotation;

            Debug.Log($"Placed Flexit on surface: {instance.name}");
        }
        else
        {
            Debug.Log("Немає поверхні для розміщення");
        }
    }

    public override bool IsBuildTool()
    {
        return true; // FlexitItem є інструментом для будівництва/руйнування
    }





}
