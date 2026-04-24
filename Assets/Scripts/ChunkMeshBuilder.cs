using System.Collections.Generic;
using UnityEngine;

public static class ChunkMeshBuilder
{
    private const float WaterSurfaceHeight = 0.86f;

    private static readonly Vector3[][] FaceVertices =
    {
        new[] { new Vector3(0,1,1), new Vector3(1,1,1), new Vector3(1,1,0), new Vector3(0,1,0) },
        new[] { new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(1,0,1), new Vector3(0,0,1) },
        new[] { new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(1,1,1), new Vector3(1,0,1) },
        new[] { new Vector3(0,0,1), new Vector3(0,1,1), new Vector3(0,1,0), new Vector3(0,0,0) },
        new[] { new Vector3(1,0,1), new Vector3(1,1,1), new Vector3(0,1,1), new Vector3(0,0,1) },
        new[] { new Vector3(0,0,0), new Vector3(0,1,0), new Vector3(1,1,0), new Vector3(1,0,0) },
    };

    private static readonly Vector3[][] WaterFaceVertices =
    {
        new[] { new Vector3(0,WaterSurfaceHeight,1), new Vector3(1,WaterSurfaceHeight,1), new Vector3(1,WaterSurfaceHeight,0), new Vector3(0,WaterSurfaceHeight,0) },
        new[] { new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(1,0,1), new Vector3(0,0,1) },
        new[] { new Vector3(1,0,0), new Vector3(1,WaterSurfaceHeight,0), new Vector3(1,WaterSurfaceHeight,1), new Vector3(1,0,1) },
        new[] { new Vector3(0,0,1), new Vector3(0,WaterSurfaceHeight,1), new Vector3(0,WaterSurfaceHeight,0), new Vector3(0,0,0) },
        new[] { new Vector3(1,0,1), new Vector3(1,WaterSurfaceHeight,1), new Vector3(0,WaterSurfaceHeight,1), new Vector3(0,0,1) },
        new[] { new Vector3(0,0,0), new Vector3(0,WaterSurfaceHeight,0), new Vector3(1,WaterSurfaceHeight,0), new Vector3(1,0,0) },
    };

    private static readonly Vector2[] FaceUVs =
    {
        new Vector2(0,0), new Vector2(1,0),
        new Vector2(1,1), new Vector2(0,1),
    };

    public static Mesh Build(
        Dictionary<Vector3Int, List<FaceDirection>> visibleFacesPerBlock,
        Dictionary<Vector3Int, BlockType> blocks,
        out BlockType[] subMeshOrder)
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var trisByType = new Dictionary<BlockType, List<int>>();

        foreach (var (blockPos, faces) in visibleFacesPerBlock)
        {
            BlockType type = blocks[blockPos];
            if (!trisByType.ContainsKey(type))
                trisByType[type] = new List<int>();

            foreach (var face in faces)
                AddFace(type, face, blockPos, vertices, trisByType[type], uvs);
        }

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
            types[i++] = type;
        }

        mesh.RecalculateNormals();
        subMeshOrder = types;
        return mesh;
    }

    private static void AddFace(
        BlockType type, FaceDirection direction, Vector3Int blockPos,
        List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
    {
        int start = vertices.Count;
        Vector3[] faceVertices = type.IsWater()
            ? WaterFaceVertices[(int)direction]
            : FaceVertices[(int)direction];

        foreach (var v in faceVertices)
            vertices.Add(blockPos + v);

        triangles.Add(start + 0); triangles.Add(start + 1); triangles.Add(start + 2);
        triangles.Add(start + 0); triangles.Add(start + 2); triangles.Add(start + 3);

        foreach (var uv in FaceUVs)
            uvs.Add(uv);
    }
}
