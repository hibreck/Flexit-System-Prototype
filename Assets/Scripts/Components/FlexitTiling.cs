using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering; // ��� MaterialProperty

[MaterialProperty("_Tiling")]
public struct FlexitTiling : IComponentData
{
    public float2 Value;
}