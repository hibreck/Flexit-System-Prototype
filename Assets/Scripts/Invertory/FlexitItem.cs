using System.Collections.Generic;
using UnityEngine;

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

        if (Physics.Raycast(ray, out RaycastHit hit, 5f)) // 10f — дальність установки
        {
            // Заборона установки на гравця
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                Debug.Log("Не можна встановити на гравця");
                return;
            }

            GameObject instance = Object.Instantiate(flexitPrefab);
            instance.name = flexitPrefab.name;

            // Знаходимо точку на поверхні інстанса, яка буде в хіті
            Collider instanceCollider = instance.GetComponent<Collider>();
            if (instanceCollider != null)
            {
                // Розрахунок позиції з урахуванням форми об’єкта
                Vector3 pointOnSurface = instanceCollider.ClosestPoint(hit.point - hit.normal * 10f);
                Vector3 offset = instance.transform.position - pointOnSurface;

                instance.transform.position = hit.point + offset;
            }
            else
            {
                // Якщо немає колайдера — просто ставимо у точку
                instance.transform.position = hit.point;
            }

            instance.transform.rotation = Quaternion.identity;

            Debug.Log($"Placed Flexit on surface: {instance.name}");
        }
        else
        {
            Debug.Log("Немає поверхні для розміщення");
        }
    }

}
