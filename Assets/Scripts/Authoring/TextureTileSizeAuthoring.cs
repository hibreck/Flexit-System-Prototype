using Unity.Entities;
using Unity.Mathematics; // ��� float
using UnityEngine; // ��� MonoBehaviour

// �� MonoBehaviour, ���� �� ������ �������� �� GameObject � ����.
public class TextureTileSizeAuthoring : MonoBehaviour
{
    // �� ��������, ��� �� ������� ����������� � Inspector ��� ������� GameObject.
    // ���� �������, ������ ������� ������� ����� ���� ������ ��������.
    public float TextureTileSizeValue = 1.0f; // ������: 1.0 ������ �� 1 ����.

    // ��������� ���� Baker ��� ������������ MonoBehaviour �� ECS ComponentData.
    public class TextureTileSizeBaker : Baker<TextureTileSizeAuthoring>
    {
        // ����� Bake ����������� Unity ��� ������������ GameObject �� Entity.
        public override void Bake(TextureTileSizeAuthoring authoring)
        {
            // �������� Entity, ��� ������� ��������� GameObject.
            // TransformUsageFlags.Renderable �����, �� �� Entity ���� �����������.
            var entity = GetEntity(TransformUsageFlags.Renderable);

            // ������ ��������� TextureTileSize �� Entity � ��������� � Authoring �������.
            AddComponent(entity, new TextureTileSize
            {
                Value = authoring.TextureTileSizeValue
            });
        }
    }
}