using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    public int chunkSize = 32;
    public float noiseScale = 0.1f;
    public int terrainHeight = 30;

    public Dictionary<Vector3Int, BlockType> GenerateChunk(int seed, Vector2Int chunkPos)
    {
        var prng = new System.Random(seed);
        var offset = new Vector2(prng.Next(-100000, 100000),
                                 prng.Next(-100000, 100000));

        var blocks = new Dictionary<Vector3Int, BlockType>();

        // Coordonnées monde = coordonnées locales + offset du chunk
        int startX = chunkPos.x * chunkSize;
        int startZ = chunkPos.y * chunkSize;

        for (int x = 0; x < chunkSize; x++)
            for (int z = 0; z < chunkSize; z++)
            {
                float nx = (startX + x + offset.x) * noiseScale;
                float nz = (startZ + z + offset.y) * noiseScale;
                int height = Mathf.FloorToInt(Mathf.PerlinNoise(nx, nz) * terrainHeight);

                for (int y = 0; y <= height; y++)
                {
                    var type = (y > 3) ? BlockType.Dirt : BlockType.Stone;
                    // Position absolue dans le monde
                    blocks[new Vector3Int(startX + x, y, startZ + z)] = type;
                }
            }

        return blocks;
    }
}