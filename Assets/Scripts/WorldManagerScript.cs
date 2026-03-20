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

    void Start()
    {
        materialLookup.Clear();
        foreach (var entry in blockMaterials)
            materialLookup[entry.type] = entry.material;

        if (chunkSpawner == null)
            chunkSpawner = GetComponent<ChunkSpawner>();

        if (chunkSpawner == null)
            chunkSpawner = gameObject.AddComponent<ChunkSpawner>();

        var save = chunkSpawner.Initialize(this, SavePath);

        if (save.player != null && player != null)
        {
            player.transform.position = save.player.position;
            player.transform.rotation = save.player.rotation;
        }
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

    public void DestroyMultipleBlocks(List<Vector3Int> worldPos)
    {
        foreach (Vector3 block in worldPos)
        {
            var p = Vector3Int.FloorToInt(block);
            if (!blocks.ContainsKey(p)) return;

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
        List<RaycastHit> allHits = new List<RaycastHit>();
        float radius = 5f;

        float angleStep = 30f;

        for (float yaw = 0; yaw < 360; yaw += angleStep)
        {
            for (float pitch = -60; pitch <= 60; pitch += angleStep)
            {
                Vector3 direction =
                    Quaternion.Euler(pitch, yaw, 0) * Vector3.forward;

                RaycastHit[] hits =
                    Physics.RaycastAll(origin, direction, radius);

                foreach (var hit in hits)
                {
                    if (!allHits.Contains(hit))
                        allHits.Add(hit);
                }

                Debug.DrawRay(origin, direction * radius, Color.green, 1f);
            }
        }

        List<Vector3Int> allBlocks = new List<Vector3Int>();

        foreach (RaycastHit hit in allHits)
        {
            allBlocks.Add(GetHitBlockPosition(hit));
        }



        DestroyMultipleBlocks(allBlocks);

        Debug.Log("Blocks hit: " + allBlocks.Count);
    }
}
