using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    public int chunkSize = 32;
    public float noiseScale = 0.1f;
    public int terrainHeight = 10;
    public float continentNoiseScale = 0.008f;
    public int continentHeight = 10;
    [Range(0f, 1f)] public float detailStrength = 0.3f;
    public int terrainBaseOffset = -1;
    public WaterGeneration waterGeneration = new();
    public WaterGeneration Water => waterGeneration ??= new();

    public Dictionary<Vector3Int, BlockType> Generate(int seed)
    {
        Vector2 offset = GetSeedOffset(seed);

        var blocks = new Dictionary<Vector3Int, BlockType>();
        int half = chunkSize / 2;

        for (int x = -half; x < half; x++)
            for (int z = -half; z < half; z++)
            {
                int height = GetTerrainHeight(x, z, offset);

                for (int y = 0; y <= height; y++)
                {
                    BlockType type;

                    if(y <=3){
                        type = BlockType.Stone;
                    }
                    else if (y==height){
                        type = BlockType.Grass;
                    }
                    else{
                        type = BlockType.Dirt;
                    }
                
                    blocks[new Vector3Int(x, y, z)] = type;
                }

                Water.AddWaterColumn(blocks, x, z, height);
            }

        return blocks;
    }

    public int GetTerrainHeight(int x, int z, Vector2 offset)
    {
        float continent = SampleNoise(x, z, offset, continentNoiseScale);
        float detail = SampleNoise(x, z, offset, noiseScale);

        float height = Water.waterLevel + terrainBaseOffset;
        height += (continent - 0.5f) * 2f * continentHeight;
        height += (detail - 0.5f) * 2f * terrainHeight * detailStrength;

        return Mathf.Max(0, Mathf.RoundToInt(height));
    }

    public Vector2 GetSeedOffset(int seed)
    {
        var prng = new System.Random(seed);
        return new Vector2(prng.Next(-100000, 100000),
                           prng.Next(-100000, 100000));
    }

    private float SampleNoise(int x, int z, Vector2 offset, float scale)
    {
        float nx = (x + offset.x) * scale;
        float nz = (z + offset.y) * scale;
        return Mathf.PerlinNoise(nx, nz);
    }
}
