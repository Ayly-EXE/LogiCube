using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    public int chunkSize = 32;
    public float noiseScale = 0.1f;
    public int terrainHeight = 10;

    public Dictionary<Vector3Int, BlockType> Generate(int seed)
    {
        var prng = new System.Random(seed);
        var offset = new Vector2(prng.Next(-100000, 100000),
                                 prng.Next(-100000, 100000));

        var blocks = new Dictionary<Vector3Int, BlockType>();
        int half = chunkSize / 2;

        for (int x = -half; x < half; x++)
            for (int z = -half; z < half; z++)
            {
                float nx = (x + offset.x) * noiseScale;
                float nz = (z + offset.y) * noiseScale;
                int height = Mathf.FloorToInt(Mathf.PerlinNoise(nx, nz) * terrainHeight);

                for (int y = 0; y <= height; y++)
                {
                    var type = (y > 3) ? BlockType.Dirt : BlockType.Stone;
                    blocks[new Vector3Int(x, y, z)] = type;
                }
            }

        return blocks;
    }
}