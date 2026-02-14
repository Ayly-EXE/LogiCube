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

    void GenerateTerrain(WorldManagerScript manager)
    {
        int half = chunkSize / 2;

        for (int x = -half; x < half; x++)
        {
            for (int z = -half; z < half; z++)
            {
                // 1) On calcule les coordonnées pour le bruit
                float nx = (x + noiseOffset.x) * noiseScale;
                float nz = (z + noiseOffset.y) * noiseScale;

                // 2) On récupère une valeur entre 0 et 1
                float noise = Mathf.PerlinNoise(nx, nz);

                // 3) On transforme ça en hauteur en blocs
                int height = Mathf.FloorToInt(Mathf.PerlinNoise(nx, nz) * terrainHeight);

                // 4) On empile les blocs de y=0 jusqu'à y=height
                for (int y = 0; y <= height; y++)
                {
                    Vector3Int gridPos = new Vector3Int(x, y, z);
                    Vector3 worldPos = (Vector3)gridPos;
                    GameObject bloc;
                    if (y > 3)
                    {
                        bloc = Instantiate(dirtBlock, worldPos, Quaternion.identity);
                    }
                    else
                    {
                        bloc = Instantiate(stoneBlock, worldPos, Quaternion.identity);
                    }

                    manager.RegisterGeneratedBlock(bloc, gridPos);
                }
            }
        }
    }
}