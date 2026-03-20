using System.Collections.Generic;
using UnityEngine;
using System.IO;


// --- Données de sauvegarde ---
[System.Serializable] public class BlockData { public Vector3Int position; public string blockType; }
[System.Serializable] public class PlayerData { public Vector3 position; public Quaternion rotation; }

[System.Serializable]
public class WorldSaveData
{
    public int seed;
    public List<BlockData> placedBlocks = new();
    public List<BlockData> removedBlocks = new();
    public PlayerData player;
}


// --- Mapping BlockType -> Material ---
[System.Serializable]
public class BlockMaterialEntry
{
    public BlockType type;
    public Material material;
}


public class WorldManagerScript : MonoBehaviour
{
    [Header("Références")]
    public WorldGenerator generator;
    public GameObject player;
    public ChunkSpawner chunkSpawner;

    [Header("Matériaux")]
    public List<BlockMaterialEntry> blockMaterials = new();

    private Dictionary<BlockType, Material> materialLookup = new();

    private Dictionary<Vector3Int, BlockType> blocks = new();

    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    [Header("VFX")]
    public ParticleSystem explosionPrefab;

    void Start()
    {
        materialLookup.Clear();
        foreach (var entry in blockMaterials)
            materialLookup[entry.type] = entry.material;

        var save = chunkSpawner.Initialize(this, SavePath);
        player.transform.position = save.player.position;
        player.transform.rotation = save.player.rotation;
    }

    public void PlaceBlock(Vector3 worldPos, BlockType type)
    {
        var p = Vector3Int.FloorToInt(worldPos);
        blocks[p] = type;

        chunkSpawner.OnBlockPlaced(p, type);
    }

    public void DestroyBlock(Vector3 worldPos)
    {
        var p = Vector3Int.FloorToInt(worldPos);
        if (!blocks.ContainsKey(p)) return;

        blocks.Remove(p);
        chunkSpawner.OnBlockDestroyed(p);
        RebuildChunk();
    }

    public void DestroyMultipleBlocks(HashSet<Vector3Int> worldPos)
    {
        foreach (Vector3Int p in worldPos)
        {
            if (!blocks.ContainsKey(p))
                continue;   // ← NOT return

            blocks.Remove(p);
            chunkSpawner.OnBlockDestroyed(p);
        }

        RebuildChunk();
    }

    public void RebuildChunk()
    {
        chunkSpawner.RebuildLoadedChunks();
    }

    public Material GetMaterial(BlockType type)
    {
        if (materialLookup.TryGetValue(type, out var mat))
            return mat;

        Debug.LogWarning($"No material assigned for block type {type}");
        return null;
    }

    public void SaveWorld()
    {
        chunkSpawner.SaveWorld(SavePath, player);
    }

    void OnApplicationQuit()
    {
        SaveWorld();
    }

    public Dictionary<Vector3Int, BlockType> Blocks => blocks;

    Vector3Int GetHitBlockPosition(RaycastHit hit)
    {
        return Vector3Int.FloorToInt(hit.point - hit.normal * 0.01f);
    }

    public void Explode(Vector3 origin)
    {
        int radius = 3;

        if (explosionPrefab != null)
        {
            ParticleSystem vfx = Instantiate(explosionPrefab, origin, Quaternion.identity);
            vfx.Play();
            Destroy(vfx.gameObject, vfx.main.duration + vfx.main.startLifetime.constantMax);
        }

        Vector3Int center = Vector3Int.FloorToInt(origin);

        HashSet<Vector3Int> blocksToDestroy = new HashSet<Vector3Int>();

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    Vector3Int offset = new Vector3Int(x, y, z);
                    Vector3Int pos = center + offset;

                    // sphere check (distance² <= radius²)
                    if (offset.sqrMagnitude > radius * radius)
                        continue;

                    // only destroy existing blocks
                    if (!blocks.ContainsKey(pos))
                        continue;

                    blocksToDestroy.Add(pos);
                }
            }
        }

        DestroyMultipleBlocks(blocksToDestroy);
    }
}
