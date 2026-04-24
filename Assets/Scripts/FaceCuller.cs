using System.Collections.Generic;
using UnityEngine;

public enum FaceDirection { Top, Bottom, Right, Left, Front, Back }

public static class FaceCuller
{
    public static readonly Vector3Int[] DirectionVectors =
    {
        Vector3Int.up,
        Vector3Int.down,
        Vector3Int.right,
        Vector3Int.left,
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 0, -1),
    };

    public static List<FaceDirection> GetVisibleFaces(Vector3Int pos, Dictionary<Vector3Int, BlockType> blocks)
    {
        var visible = new List<FaceDirection>();
        if (!blocks.TryGetValue(pos, out var currentType))
            return visible;

        for (int i = 0; i < 6; i++)
        {
            Vector3Int neighborPos = pos + DirectionVectors[i];
            BlockType neighborType = blocks.TryGetValue(neighborPos, out var neighbor)
                ? neighbor
                : BlockType.Air;

            if (IsFaceVisible(currentType, neighborType, (FaceDirection)i))
                visible.Add((FaceDirection)i);
        }

        return visible;
    }

    private static bool IsFaceVisible(BlockType currentType, BlockType neighborType, FaceDirection face)
    {
        if (currentType.IsWater())
        {
            if (face == FaceDirection.Bottom)
                return false;

            return neighborType == BlockType.Air;
        }

        return neighborType == BlockType.Air || neighborType.IsWater();
    }
}
