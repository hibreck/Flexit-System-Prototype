using Unity.Entities;
using Unity.Mathematics; // Для float
using UnityEngine; // Для MonoBehaviour

// Це MonoBehaviour, який ми будемо додавати до GameObject у сцені.
public class TextureTileSizeAuthoring : MonoBehaviour
{
    // Це значення, яке ви зможете налаштувати в Inspector для кожного GameObject.
    // Воно визначає, скільки світових одиниць займає одна плитка текстури.
    public float TextureTileSizeValue = 1.0f; // Дефолт: 1.0 плитка на 1 метр.

    // Вкладений клас Baker для перетворення MonoBehaviour на ECS ComponentData.
    public class TextureTileSizeBaker : Baker<TextureTileSizeAuthoring>
    {
        // Метод Bake викликається Unity для перетворення GameObject на Entity.
        public override void Bake(TextureTileSizeAuthoring authoring)
        {
            // Отримуємо Entity, яка відповідає поточному GameObject.
            // TransformUsageFlags.Renderable вказує, що ця Entity буде візуалізована.
            var entity = GetEntity(TransformUsageFlags.Renderable);

            // Додаємо компонент TextureTileSize до Entity з значенням з Authoring скрипта.
            AddComponent(entity, new TextureTileSize
            {
                Value = authoring.TextureTileSizeValue
            });
        }
    }
}