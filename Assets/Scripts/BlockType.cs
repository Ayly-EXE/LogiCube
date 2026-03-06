public enum BlockType
{
    Air = 0,  // vide
    Dirt = 1,  // terre
    Stone = 2,  // pierre
}

public static class BlockTypeExtensions
{
    public static bool IsSolid(this BlockType type) => type != BlockType.Air;
}