using Unity.Entities;
using Unity.Rendering; // Для RenderMeshUnmanaged, MaterialPropertyOverride, MaterialMeshInfoSystem
using Unity.Mathematics;
using UnityEngine; // Для Shader.PropertyToID

// Система буде оновлювати властивості матеріалу.
// Використання MaterialMeshInfoSystem є коректним для Entities Graphics 1.4.12.
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class UpdateTextureTileSizeSystem : SystemBase
{
    // ID властивості шейдера. Назва ТОЧНО відповідає "Reference" в Blackboard Shader Graph.
    private static readonly int TextureTileSizePropertyId = Shader.PropertyToID("_TextureTileSizeMeters");

    protected override void OnUpdate()
    {
        Entities
            .WithAll<RenderMeshUnmanaged>()
            .ForEach((ref TextureTileSize tileSize) =>
            {
                tileSize.Value = math.sin((float)SystemAPI.Time.ElapsedTime) * 2f;
            }).Schedule();
    }

}