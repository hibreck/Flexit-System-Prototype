using UnityEngine;

public static class FlexitDestroyer
{
    public static void TryDestroyFlexitFromCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("No main camera found.");
            return;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, 5f))
        {
            var block = hit.collider.GetComponent<EditableBlock>();
            if (block != null)
            {
                Object.Destroy(block.gameObject);
                Debug.Log($"🗑 Знищено блок: {block.name}");
            }
            else
            {
                Debug.Log("❌ Вказаний об’єкт не є EditableBlock");
            }
        }
        else
        {
            Debug.Log("❌ Немає блоку для знищення");
        }
    }
}
