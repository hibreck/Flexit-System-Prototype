using System.Collections.Generic;
using UnityEngine;

public abstract class Item : ScriptableObject
{
    public string itemName;
    public Sprite icon;

    public abstract Dictionary<string, int> GetStats(); //Універсальний метод для всіх предметів
    public abstract void Use(GameObject user);


}