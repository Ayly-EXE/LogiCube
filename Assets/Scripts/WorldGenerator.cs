using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    public int chunkSize = 32;
    public float noiseScale = 0.1f;
    public int terrainHeight = 10;
    public WaterGeneration waterGeneration = new();
    public WaterGeneration Water => waterGeneration ??= new();

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
}
