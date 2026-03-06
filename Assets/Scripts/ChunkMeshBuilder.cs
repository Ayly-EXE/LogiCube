using System.Collections.Generic;
using UnityEngine;

public static class ChunkMeshBuilder
{
    private static readonly Vector3[][] FaceVertices =
    {
        // Top
        new[] { new Vector3(0,1,1), new Vector3(1,1,1), new Vector3(1,1,0), new Vector3(0,1,0) },
        // Bottom
        new[] { new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(1,0,1), new Vector3(0,0,1) },
        // Right
        new[] { new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(1,1,1), new Vector3(1,0,1) },
        // Left
        new[] { new Vector3(0,0,1), new Vector3(0,1,1), new Vector3(0,1,0), new Vector3(0,0,0) },
        // Front
        new[] { new Vector3(1,0,1), new Vector3(1,1,1), new Vector3(0,1,1), new Vector3(0,0,1) },
        // Back
        new[] { new Vector3(0,0,0), new Vector3(0,1,0), new Vector3(1,1,0), new Vector3(1,0,0) },
    };

    private static readonly Vector2[] FaceUVs =
    {
        new Vector2(0,0), new Vector2(1,0),
        new Vector2(1,1), new Vector2(0,1),
    };

    public static Mesh Build(
        Dictionary<Vector3Int, List<FaceDirection>> visibleFacesPerBlock,
        Dictionary<Vector3Int, BlockType> blocks,
        out BlockType[] subMeshOrder) // retourne l'ordre des matériaux utilisés
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();

        // Un tableau de triangles par BlockType
        var trisByType = new Dictionary<BlockType, List<int>>();

        foreach (var (blockPos, faces) in visibleFacesPerBlock)
        {
            var type = blocks[blockPos];

            if (!trisByType.ContainsKey(type))
                trisByType[type] = new List<int>();

            foreach (var face in faces)
                AddFace(face, blockPos, vertices, trisByType[type], uvs);
        }

        // Construit le mesh avec un sub-mesh par BlockType
        var mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.subMeshCount = trisByType.Count;

        var types = new BlockType[trisByType.Count];
        int i = 0;
        foreach (var (type, tris) in trisByType)
        {
            mesh.SetTriangles(tris, i);
            types[i] = type;
            i++;
        }

        mesh.RecalculateNormals();
        subMeshOrder = types; // pour que WorldManager sache quel material mettre où
        return mesh;
    }

    private static void AddFace(
        FaceDirection direction,
        Vector3Int blockPos,
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector2> uvs)
    {
        int faceIndex = (int)direction;
        int start = vertices.Count;

        foreach (var v in FaceVertices[faceIndex])
            vertices.Add(blockPos + v);

        triangles.Add(start + 0); triangles.Add(start + 1); triangles.Add(start + 2);
        triangles.Add(start + 0); triangles.Add(start + 2); triangles.Add(start + 3);

        foreach (var uv in FaceUVs)
            uvs.Add(uv);
    }
}