using System;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    public GameObject dirtBlock;
    public GameObject stoneBlock;

    public int chunkSize = 32;
    public float noiseScale = 0.1f;
    public int terrainHeight = 10;

    private Vector2 noiseOffset;

    public void Generate(int seed, WorldManagerScript manager)
    {
        var prng = new System.Random(seed);
        noiseOffset = new Vector2(
            prng.Next(-100000, 100000),
            prng.Next(-100000, 100000)
        );

        GenerateTerrain(manager);
    }

    private void GenerateTerrain(WorldManagerScript manager)
    {
        int half = chunkSize / 2;

        for (int x = -half; x < half; x++)
        for (int z = -half; z < half; z++)
        {
            float nx = (x + noiseOffset.x) * noiseScale;
            float nz = (z + noiseOffset.y) * noiseScale;

            int totalHeight = Mathf.FloorToInt(Mathf.PerlinNoise(nx, nz) * terrainHeight);

            for (int y = 0; y <= totalHeight; y++)
            {
                
                var pos = new Vector3Int(x, y, z);
                var go = Instantiate(y > 3 ?  dirtBlock : stoneBlock , (Vector3)pos, Quaternion.identity);

                manager.RegisterGeneratedBlock(go, pos);
            }
        }
    }
}