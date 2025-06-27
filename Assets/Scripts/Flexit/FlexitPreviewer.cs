using UnityEngine;
using UnityEngine.InputSystem;

public class FlexitPreviewer : MonoBehaviour
{
    [Header("Settings")]
    public float previewDistance = 5f;
    public LayerMask placeableSurfaces;
    public Material ghostMaterial;
    [SerializeField] private Camera cam;

    private GameObject currentGhost;
    private Item currentItem;
    private PlayerInventory inventory;

    private const float pixelStep = 4f / 64f;

    private void Start()
    {
        inventory = FindAnyObjectByType<PlayerInventory>();
        if (inventory == null)
            Debug.LogWarning("🔴 PlayerInventory не знайдено!");

        if (cam == null)
            Debug.LogWarning("🔴 Камера не призначена у інспекторі!");
    }

    private void Update()
    {
        if (inventory == null || cam == null)
        {
            DestroyGhost();
            return;
        }

        if (EditModeManager.Instance != null && EditModeManager.Instance.IsEditModeActive)
        {
            DestroyGhost();
            return;
        }

        Item slotItem = inventory.slots[inventory.activeSlot].item;

        if (!(slotItem is FlexitItem flexitItem))
        {
            DestroyGhost();
            return;
        }

        if (currentItem != slotItem)
        {
            CreateGhost(flexitItem);
        }

        UpdateGhostTransform(flexitItem);
    }

    private void CreateGhost(FlexitItem flexitItem)
    {
        DestroyGhost();

        currentItem = flexitItem;
        GameObject prefab = flexitItem.flexitPrefab;

        if (prefab == null)
        {
            Debug.LogWarning("Flexit prefab is null!");
            return;
        }

        currentGhost = Instantiate(prefab);
        currentGhost.name = prefab.name + "_Ghost";

        foreach (var col in currentGhost.GetComponentsInChildren<Collider>())
            col.enabled = false;

        foreach (var rb in currentGhost.GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = true;

        SetGhostMaterial(currentGhost);
    }

    private void SetGhostMaterial(GameObject obj)
    {
        foreach (var rend in obj.GetComponentsInChildren<Renderer>())
        {
            Material[] ghostMats = new Material[rend.materials.Length];
            for (int i = 0; i < ghostMats.Length; i++)
                ghostMats[i] = ghostMaterial;

            rend.materials = ghostMats;
        }
    }

    private void DestroyGhost()
    {
        if (currentGhost != null)
            Destroy(currentGhost);

        currentGhost = null;
        currentItem = null;
    }

    private void UpdateGhostTransform(FlexitItem flexitItem)
    {
        if (currentGhost == null || cam == null) return;

        if (!FlexitPlacementUtility.ComputePlacement(cam, flexitItem.flexitPrefab, previewDistance, placeableSurfaces, out Vector3 pos, out Quaternion rot))
        {
            currentGhost.SetActive(false);
            return;
        }

        currentGhost.SetActive(true);

       

        currentGhost.transform.SetPositionAndRotation(pos, rot);
    }
}
