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

        // Проста логіка: інстанціювати перед гравцем
        Vector3 spawnPosition = user.transform.position + user.transform.forward * 2f;
        Quaternion spawnRotation = Quaternion.identity;

        GameObject placed = Object.Instantiate(flexitPrefab, spawnPosition, spawnRotation);
        placed.name = flexitPrefab.name;

        Debug.Log($"Placed Flexit: {placed.name}");
    }
}
