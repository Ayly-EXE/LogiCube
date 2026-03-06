using System.Collections.Generic;
using UnityEngine;

public enum FaceDirection { Top, Bottom, Right, Left, Front, Back }

public static class FaceCuller
{
    public static readonly Vector3Int[] DirectionVectors =
    {
        Vector3Int.up,                  // Top
        Vector3Int.down,                // Bottom
        Vector3Int.right,               // Right
        Vector3Int.left,                // Left
        new Vector3Int(0, 0,  1),       // Front
        new Vector3Int(0, 0, -1),       // Back
    };

    // Retourne la liste des faces visibles pour un bloc 
    public static List<FaceDirection> GetVisibleFaces(Vector3Int pos, Dictionary<Vector3Int, BlockType> blocks)
    {
        var visible = new List<FaceDirection>();

        for (int i = 0; i < 6; i++)
        {
            Vector3Int neighborPos = pos + DirectionVectors[i];

            bool neighborIsSolid = blocks.TryGetValue(neighborPos, out var neighbor)
                                   && neighbor.IsSolid();

            if (!neighborIsSolid)
                visible.Add((FaceDirection)i);
        }

        return visible;
    }
}