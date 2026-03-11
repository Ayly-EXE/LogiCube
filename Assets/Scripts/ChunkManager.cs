using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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

public class ChunkManager : MonoBehaviour
{
    [Header("Références")]
    public WorldGenerator generator;
    public Transform player;
    public Material dirtMaterial;
    public Material stoneMaterial;

    [Header("Paramètres")]
    public int renderDistance = 6;
    public int chunksPerFrame = 2; // combien de chunks on charge par frame

    // ── état du monde ──────────────────────────────────────────
    private Dictionary<Vector3Int, BlockType> worldBlocks = new();
    private Dictionary<Vector2Int, GameObject> loadedChunks = new();
    private Dictionary<Vector2Int, Dictionary<Vector3Int, BlockType>> chunkCache = new();
    private HashSet<Vector2Int> dirtyChunks = new();

    private int seed;
    private Vector2Int lastPlayerChunk = new Vector2Int(int.MaxValue, 0);
    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    // ──────────────────────────────────────────────────────────
    void Start()
    {
        var save = LoadOrCreate();
        seed = save.seed;
        ApplyDelta(save);

        if (save.player != null && player != null)
        {
            player.position = save.player.position;
            player.rotation = save.player.rotation;
        }
    }

    void Update()
    {
        // ── Rebuild les chunks modifiés ──
        foreach (var chunkPos in dirtyChunks)
            if (loadedChunks.ContainsKey(chunkPos))
            {
                Destroy(loadedChunks[chunkPos]);
                loadedChunks[chunkPos] = BuildChunkGO(chunkPos);
            }
        dirtyChunks.Clear();

        // ── Charge/décharge si le joueur a changé de chunk ──
        Vector2Int playerChunk = WorldToChunkPos(player.position);
        if (playerChunk == lastPlayerChunk) return;
        lastPlayerChunk = playerChunk;
        UpdateChunks(playerChunk);
    }

    // ──────────────────────────────────────────────────────────
    //  API publique
    // ──────────────────────────────────────────────────────────

    public void PlaceBlock(Vector3 worldPos, BlockType type)
    {
        var p = Vector3Int.FloorToInt(worldPos);
        worldBlocks[p] = type;
        dirtyChunks.Add(WorldToChunkPos(worldPos));
    }

    public void DestroyBlock(Vector3 worldPos)
    {
        var p = Vector3Int.FloorToInt(worldPos);
        if (!worldBlocks.ContainsKey(p)) return;
        worldBlocks.Remove(p);
        dirtyChunks.Add(WorldToChunkPos(worldPos));
    }

    // ──────────────────────────────────────────────────────────
    //  Chunk streaming
    // ──────────────────────────────────────────────────────────

    void UpdateChunks(Vector2Int center)
    {
        var toKeep = new HashSet<Vector2Int>();
        var toLoad = new List<Vector2Int>();

        for (int x = -renderDistance; x <= renderDistance; x++)
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                var pos = new Vector2Int(center.x + x, center.y + z);
                toKeep.Add(pos);
                if (!loadedChunks.ContainsKey(pos))
                    toLoad.Add(pos);
            }

        // Charge les chunks les plus proches en premier
        toLoad.Sort((a, b) =>
            Vector2Int.Distance(a, center).CompareTo(Vector2Int.Distance(b, center)));

        StartCoroutine(LoadChunksGradually(toLoad));

        // Décharge les chunks trop loin
        var toUnload = new List<Vector2Int>();
        foreach (var pos in loadedChunks.Keys)
            if (!toKeep.Contains(pos))
                toUnload.Add(pos);

        foreach (var pos in toUnload)
            UnloadChunk(pos);
    }

    IEnumerator LoadChunksGradually(List<Vector2Int> toLoad)
    {
        for (int i = 0; i < toLoad.Count; i++)
        {
            LoadChunk(toLoad[i]);

            // Pause toutes les chunksPerFrame chunks
            if (i % chunksPerFrame == 0)
                yield return null;
        }
    }

    void LoadChunk(Vector2Int chunkPos)
    {
        // Si pas en cache → génère et sauvegarde dans le cache
        if (!chunkCache.ContainsKey(chunkPos))
            chunkCache[chunkPos] = generator.GenerateChunk(seed, chunkPos);

        // Ajoute les blocs au monde (sans écraser les modifs du joueur)
        foreach (var (pos, type) in chunkCache[chunkPos])
            if (!worldBlocks.ContainsKey(pos))
                worldBlocks[pos] = type;

        loadedChunks[chunkPos] = BuildChunkGO(chunkPos);
    }

    void UnloadChunk(Vector2Int chunkPos)
    {
        if (loadedChunks.TryGetValue(chunkPos, out var go))
            Destroy(go);

        loadedChunks.Remove(chunkPos);

        // Retire les blocs du monde mais garde le cache
        int startX = chunkPos.x * generator.chunkSize;
        int startZ = chunkPos.y * generator.chunkSize;

        var toRemove = new List<Vector3Int>();
        foreach (var pos in worldBlocks.Keys)
            if (pos.x >= startX && pos.x < startX + generator.chunkSize &&
                pos.z >= startZ && pos.z < startZ + generator.chunkSize)
                toRemove.Add(pos);

        foreach (var pos in toRemove)
            worldBlocks.Remove(pos);
    }

    // ──────────────────────────────────────────────────────────
    //  Construction du mesh
    // ──────────────────────────────────────────────────────────

    GameObject BuildChunkGO(Vector2Int chunkPos)
    {
        int startX = chunkPos.x * generator.chunkSize;
        int startZ = chunkPos.y * generator.chunkSize;

        var chunkBlocks = new Dictionary<Vector3Int, BlockType>();
        foreach (var (pos, type) in worldBlocks)
            if (pos.x >= startX && pos.x < startX + generator.chunkSize &&
                pos.z >= startZ && pos.z < startZ + generator.chunkSize)
                chunkBlocks[pos] = type;

        var visibleFaces = new Dictionary<Vector3Int, List<FaceDirection>>();
        foreach (var (pos, type) in chunkBlocks)
        {
            if (!type.IsSolid()) continue;
            var faces = FaceCuller.GetVisibleFaces(pos, worldBlocks);
            if (faces.Count > 0)
                visibleFaces[pos] = faces;
        }

        var mesh = ChunkMeshBuilder.Build(visibleFaces, chunkBlocks, out var subMeshOrder);

        var materials = new Material[subMeshOrder.Length];
        for (int i = 0; i < subMeshOrder.Length; i++)
            materials[i] = subMeshOrder[i] == BlockType.Stone ? stoneMaterial : dirtMaterial;

        var go = new GameObject($"Chunk_{chunkPos.x}_{chunkPos.y}");
        go.AddComponent<MeshFilter>().mesh = mesh;
        go.AddComponent<MeshRenderer>().materials = materials;
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
        return go;
    }

    Vector2Int WorldToChunkPos(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / generator.chunkSize),
            Mathf.FloorToInt(worldPos.z / generator.chunkSize)
        );
    }

    // ──────────────────────────────────────────────────────────
    //  Save / Load
    // ──────────────────────────────────────────────────────────

    public void SaveWorld()
    {
        var save = new WorldSaveData { seed = seed };

        var baseBlocks = new Dictionary<Vector3Int, BlockType>();
        foreach (var chunkPos in loadedChunks.Keys)
            foreach (var (pos, type) in chunkCache[chunkPos])
                baseBlocks[pos] = type;

        foreach (var (pos, type) in worldBlocks)
            if (!baseBlocks.ContainsKey(pos))
                save.placedBlocks.Add(new BlockData { position = pos, blockType = type.ToString() });

        foreach (var (pos, type) in baseBlocks)
            if (!worldBlocks.ContainsKey(pos))
                save.removedBlocks.Add(new BlockData { position = pos, blockType = type.ToString() });

        if (player != null)
            save.player = new PlayerData
            {
                position = player.position,
                rotation = player.rotation
            };

        File.WriteAllText(SavePath, JsonUtility.ToJson(save, true));
    }

    private WorldSaveData LoadOrCreate()
    {
        if (!File.Exists(SavePath))
            return new WorldSaveData { seed = Random.Range(0, 999999) };

        var save = JsonUtility.FromJson<WorldSaveData>(File.ReadAllText(SavePath));
        save.placedBlocks ??= new();
        save.removedBlocks ??= new();
        return save;
    }

    private void ApplyDelta(WorldSaveData save)
    {
        foreach (var b in save.removedBlocks)
            worldBlocks[b.position] = BlockType.Air;

        foreach (var b in save.placedBlocks)
            worldBlocks[b.position] = System.Enum.Parse<BlockType>(b.blockType);
    }

    void OnApplicationQuit() => SaveWorld();
}