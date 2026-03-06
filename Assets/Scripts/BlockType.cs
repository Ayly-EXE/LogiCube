public enum BlockType
{
    Air = 0,
    Dirt = 1,
    Stone = 2,
}

public static class BlockTypeExtensions
{
    public static bool IsSolid(this BlockType type) => type != BlockType.Air;
}