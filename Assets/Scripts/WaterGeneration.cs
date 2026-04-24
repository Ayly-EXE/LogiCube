using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WaterGeneration
{
    public int waterLevel = 8;

    public bool IsWaterBlock(int y, int terrainHeight)
    {
        return y > terrainHeight && y <= waterLevel;
    }

    public void AddWaterColumn(Dictionary<Vector3Int, BlockType> blocks, int x, int z, int terrainHeight)
    {
        if (terrainHeight >= waterLevel)
            return;

        for (int y = terrainHeight + 1; y <= waterLevel; y++)
            blocks[new Vector3Int(x, y, z)] = BlockType.Water;
    }

    public bool IsUnderWater(int groundHeight)
    {
        return groundHeight < waterLevel;
    }
}
