using Unity.Entities;
using Unity.Rendering; // ��� RenderMeshUnmanaged, MaterialPropertyOverride, MaterialMeshInfoSystem
using Unity.Mathematics;
using UnityEngine; // ��� Shader.PropertyToID

// ������� ���� ���������� ���������� ��������.
// ������������ MaterialMeshInfoSystem � ��������� ��� Entities Graphics 1.4.12.
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class UpdateTextureTileSizeSystem : SystemBase
{
    // ID ���������� �������. ����� ����� ������� "Reference" � Blackboard Shader Graph.
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