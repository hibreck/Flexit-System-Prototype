using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

[MaterialProperty("_TextureTileSizeMeters")]
public struct TextureTileSize : IComponentData
{
    public float Value;
}