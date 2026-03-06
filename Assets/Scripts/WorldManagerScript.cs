using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
[System.Serializable]
public class BlockData
{
    public Vector3Int position;
    public string blockType;
}

[System.Serializable]
public class PlayerData
{
    public Vector3 position;
    public Quaternion rotation;
}

[System.Serializable]
public class WorldSaveData
{
    public int seed;
    // Delta
    public List<BlockData> placedBlocks = new();
    public List<BlockData> removedBlocks = new();

    public PlayerData player;
}




public class WorldManagerScript : MonoBehaviour
{
    [Header("Références")]
    public WorldGenerator generator;
    public Material chunkMaterial;
    public GameObject player;

    [Header("Materiaux par type de bloc")]
    public Material dirtMaterial;
    public Material stoneMaterial;

    [Header("Save")]
    public string savePath => Path.Combine(Application.persistentDataPath, "save.json");
    private int seed;
    private Dictionary<Vector3Int, BlockType> blocks = new();

    private GameObject chunkGO;

    void Start()
    {
        var save = LoadOrCreate();
        seed = save.seed;
        // 1) Génération de base
        blocks = generator.Generate(save.seed);

        // 2) Applique les modifications sauvegardées
        ApplyDelta(save);

        // 3) Construit le mesh initial
        RebuildChunk();

        // 4) Position du joueur
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
        RebuildChunk();
    }

    public void DestroyBlock(Vector3 worldPos)
    {
        var p = Vector3Int.FloorToInt(worldPos);
        if (!blocks.ContainsKey(p)) return;
        blocks.Remove(p);
        RebuildChunk();
    }

    // ─────────────────────────────────────────
    //  Culling → Mesh → GameObject
    // ─────────────────────────────────────────

    private void RebuildChunk()
    {
        // Étape 1 — Face Culling 
        var visibleFaces = new Dictionary<Vector3Int, List<FaceDirection>>();
        foreach (var (pos, type) in blocks)
        {
            if (!type.IsSolid()) continue;
            var faces = FaceCuller.GetVisibleFaces(pos, blocks);
            if (faces.Count > 0)
                visibleFaces[pos] = faces;
        }

        // Étape 2 — Mesh avec sub-meshes
        var mesh = ChunkMeshBuilder.Build(visibleFaces, blocks, out var subMeshOrder);

        // Étape 3 — Associe un material a chaque sub-mesh
        var materials = new Material[subMeshOrder.Length];
        for (int i = 0; i < subMeshOrder.Length; i++)
            materials[i] = GetMaterial(subMeshOrder[i]);

        ApplyMeshToChunk(mesh, materials);
    }

    private void ApplyMeshToChunk(Mesh mesh, Material[] materials)
    {
        if (chunkGO == null)
        {
            chunkGO = new GameObject("Chunk");
            chunkGO.AddComponent<MeshFilter>();
            chunkGO.AddComponent<MeshRenderer>();
            chunkGO.AddComponent<MeshCollider>();
        }

        chunkGO.GetComponent<MeshFilter>().mesh = mesh;
        chunkGO.GetComponent<MeshRenderer>().materials = materials;
        chunkGO.GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    public void SaveWorld()
    {
        var save = new WorldSaveData { seed = seed };

        // On regénère pour comparer avec la base
        var baseBlocks = generator.Generate(seed);

        foreach (var (pos, type) in blocks)
        {
            if (!baseBlocks.ContainsKey(pos))
                save.placedBlocks.Add(new BlockData { position = pos, blockType = type.ToString() });
        }

        foreach (var (pos, type) in baseBlocks)
        {
            if (!blocks.ContainsKey(pos))
                save.removedBlocks.Add(new BlockData { position = pos, blockType = type.ToString() });
        }

        if (player != null)
            save.player = new PlayerData
            {
                position = player.transform.position,
                rotation = player.transform.rotation
            };

        File.WriteAllText(savePath, JsonUtility.ToJson(save, true));
    }

    private WorldSaveData LoadOrCreate()
    {
        if (!File.Exists(savePath))
            return new WorldSaveData { seed = Random.Range(0, 999999) };

        var save = JsonUtility.FromJson<WorldSaveData>(File.ReadAllText(savePath));
        save.placedBlocks ??= new();
        save.removedBlocks ??= new();
        return save;
    }

    private void ApplyDelta(WorldSaveData save)
    {
        foreach (var b in save.removedBlocks)
            blocks.Remove(b.position);

        foreach (var b in save.placedBlocks)
            blocks[b.position] = System.Enum.Parse<BlockType>(b.blockType);
    }

    private Material GetMaterial(BlockType type) => type switch
    {
        BlockType.Dirt => dirtMaterial,
        BlockType.Stone => stoneMaterial,
        _ => dirtMaterial
    };

    void OnApplicationQuit()
    {
        SaveWorld();
    }
}