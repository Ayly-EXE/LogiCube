public enum BlockType
{
    Air = 0,  // vide
    Dirt = 1,  // terre
    Stone = 2,  // pierre
    Grass = 3,  // herbe
    Tnt = 4,
    Water = 5,
}

public static class BlockTypeExtensions
{
    public static bool IsSolid(this BlockType type) => type != BlockType.Air;
    public static bool IsRenderable(this BlockType type) => type != BlockType.Air;
    public static bool IsWater(this BlockType type) => type == BlockType.Water;
}
