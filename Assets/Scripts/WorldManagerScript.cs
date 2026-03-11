using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
/*
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

public class WorldManagerScript : MonoBehaviour
{
    [Header("Références")]
    public WorldGenerator generator;
    public GameObject player;

    [Header("Matériaux")]
    public Material dirtMaterial;
    public Material stoneMaterial;

    private Dictionary<Vector3Int, BlockType> blocks = new();
    private int seed;

    private GameObject chunkGO;

    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    // ──────────────────────────────────────────────
    void Start()
    {
        var save = LoadOrCreate();
        seed = save.seed;

        // 1) On génère le monde de base avec la seed
        blocks = generator.Generate(seed);

        // 2) On remet les modifications du joueur (blocs posés/détruits)
        ApplyDelta(save);

        // 3) On construit le rendu 3D
        RebuildChunk();

        // 4) On replace le joueur où il était
        if (save.player != null && player != null)
        {
            player.transform.position = save.player.position;
            player.transform.rotation = save.player.rotation;
        }
    }

    // ──────────────────────────────────────────────
    //  Actions du joueur
    // ──────────────────────────────────────────────

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

    private void RebuildChunk()
    {
        // Étape 1 : Face Culling — on trouve quelles faces sont visibles
        var visibleFaces = new Dictionary<Vector3Int, List<FaceDirection>>();
        foreach (var (pos, type) in blocks)
        {
            if (!type.IsSolid()) continue;
            var faces = FaceCuller.GetVisibleFaces(pos, blocks);
            if (faces.Count > 0)
                visibleFaces[pos] = faces;
        }

        // Étape 2 : Mesh Builder — on construit le mesh avec seulement ces faces
        var mesh = ChunkMeshBuilder.Build(visibleFaces, blocks, out var subMeshOrder);

        // Étape 3 : On associe un matériau à chaque type de bloc
        var materials = new Material[subMeshOrder.Length];
        for (int i = 0; i < subMeshOrder.Length; i++)
            materials[i] = GetMaterial(subMeshOrder[i]);

        // Étape 4 : On applique le tout à un seul GameObject
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

    private Material GetMaterial(BlockType type) => type switch
    {
        BlockType.Dirt => dirtMaterial,
        BlockType.Stone => stoneMaterial,
        _ => dirtMaterial
    };

    // ──────────────────────────────────────────────
    //  Sauvegarde
    // ──────────────────────────────────────────────

    public void SaveWorld()
    {
        var save = new WorldSaveData { seed = seed };
        var baseBlocks = generator.Generate(seed);

        // Ce que le joueur a ajouté
        foreach (var (pos, type) in blocks)
            if (!baseBlocks.ContainsKey(pos))
                save.placedBlocks.Add(new BlockData { position = pos, blockType = type.ToString() });

        // Ce que le joueur a détruit
        foreach (var (pos, type) in baseBlocks)
            if (!blocks.ContainsKey(pos))
                save.removedBlocks.Add(new BlockData { position = pos, blockType = type.ToString() });

        if (player != null)
            save.player = new PlayerData
            {
                position = player.transform.position,
                rotation = player.transform.rotation
            };

        File.WriteAllText(SavePath, JsonUtility.ToJson(save, true));

        Debug.Log(SavePath);
    }

    private WorldSaveData LoadOrCreate()
    {
        if (!File.Exists(SavePath))
            return new WorldSaveData { seed = UnityEngine.Random.Range(0, 999999) };

        var save = JsonUtility.FromJson<WorldSaveData>(File.ReadAllText(SavePath));
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

    void OnApplicationQuit() => SaveWorld();
}
*/