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
        if (Keyboard.current != null &&
            (Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed))
        {
            Debug.Log("Alt затиснутий — блок не буде встановлено.");
            return;
        }

        if (flexitPrefab == null)
        {
            Debug.LogWarning("⚠️ flexitPrefab is not assigned.");
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("⚠️ No main camera found.");
            return;
        }

        // Визначаємо маску: всі шари, крім шару "Player"
        LayerMask surfaceMask = ~0; // всі шари
        int playerLayer = LayerMask.NameToLayer("Player");
        surfaceMask &= ~(1 << playerLayer); // виключаємо Player

        if (!FlexitPlacementUtility.ComputePlacement(cam, flexitPrefab, 5f, surfaceMask, out Vector3 pos, out Quaternion rot))
        {
            Debug.Log("❌ Немає поверхні для розміщення.");
            return;
        }


        GameObject instance = Object.Instantiate(flexitPrefab, pos, rot);
        instance.name = flexitPrefab.name;

        Debug.Log($"✅ Placed Flexit on surface: {instance.name}");
    }




    public override bool IsBuildTool()
    {
        return true; // FlexitItem є інструментом для будівництва/руйнування
    }





}
