using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class ChunkSpawner : MonoBehaviour
{
    public WorldManagerScript worldManager;
    public int viewDistanceInChunks = 1;

    private Dictionary<Vector3Int, BlockType> placedBlocks = new();
    private HashSet<Vector3Int> removedBlocks = new();
    private Dictionary<Vector2Int, GameObject> chunkGOs = new();
    private HashSet<Vector2Int> loadedChunks = new();
    private Vector2Int currentPlayerChunk;
    private bool hasPlayerChunk;
    private int seed;

    public WorldSaveData Initialize(WorldManagerScript manager, string savePath)
    {
        worldManager = manager;
        WorldSaveData save = LoadOrCreate(savePath);
        RefreshVisibleChunks(true);
        return save;
    }

    public void SaveWorld(string savePath, GameObject player)
    {
        var save = CreateSaveData();

        if (player != null)
        {
            save.player = new PlayerData
            {
                position = player.transform.position,
                rotation = player.transform.rotation
            };
        }

        File.WriteAllText(savePath, JsonUtility.ToJson(save, true));
    }

    private WorldSaveData LoadOrCreate(string savePath)
    {
        WorldSaveData save;

        if (!File.Exists(savePath))
            save = new WorldSaveData { seed = Random.Range(0, 999999) };
        else
            save = JsonUtility.FromJson<WorldSaveData>(File.ReadAllText(savePath));

        save.placedBlocks ??= new();
        save.removedBlocks ??= new();

        seed = save.seed;
        placedBlocks.Clear();
        removedBlocks.Clear();

        foreach (var b in save.removedBlocks)
            removedBlocks.Add(b.position);

        foreach (var b in save.placedBlocks)
            placedBlocks[b.position] = System.Enum.Parse<BlockType>(b.blockType);

        return save;
    }

    public void OnBlockPlaced(Vector3Int position, BlockType type)
    {
        BlockType baseType = GetBaseBlockAt(position);

        if (type == baseType)
            placedBlocks.Remove(position);
        else
            placedBlocks[position] = type;

        removedBlocks.Remove(position);

        EnsureChunkLoaded(GetChunkCoord(position));
        RebuildLoadedChunks();
    }

    public void OnBlockDestroyed(Vector3Int position)
    {
        placedBlocks.Remove(position);

        if (GetBaseBlockAt(position).IsSolid())
            removedBlocks.Add(position);
        else
            removedBlocks.Remove(position);

        RebuildLoadedChunks();
    }

    public WorldSaveData CreateSaveData()
    {
        var save = new WorldSaveData { seed = seed };

        foreach (var (pos, type) in placedBlocks)
            save.placedBlocks.Add(new BlockData
            {
                position = pos,
                blockType = type.ToString()
            });

        foreach (var pos in removedBlocks)
            save.removedBlocks.Add(new BlockData
            {
                position = pos
            });

        return save;
    }

    void Update()
    {
        if (worldManager == null)
            return;

        RefreshVisibleChunks();
    }

    public void RefreshVisibleChunks(bool force = false)
    {
        Vector2Int playerChunk = GetPlayerChunk();
        if (!force && hasPlayerChunk && playerChunk == currentPlayerChunk)
            return;

        currentPlayerChunk = playerChunk;
        hasPlayerChunk = true;

        var wantedChunks = new HashSet<Vector2Int>();
        for (int x = -viewDistanceInChunks; x <= viewDistanceInChunks; x++)
            for (int z = -viewDistanceInChunks; z <= viewDistanceInChunks; z++)
                wantedChunks.Add(new Vector2Int(playerChunk.x + x, playerChunk.y + z));

        var chunksToUnload = new List<Vector2Int>();
        foreach (var chunkCoord in loadedChunks)
            if (!wantedChunks.Contains(chunkCoord))
                chunksToUnload.Add(chunkCoord);

        foreach (var chunkCoord in chunksToUnload)
            UnloadChunk(chunkCoord);

        foreach (var chunkCoord in wantedChunks)
            if (!loadedChunks.Contains(chunkCoord))
                LoadChunk(chunkCoord);

        RebuildLoadedChunks();
    }

    public void EnsureChunkLoaded(Vector2Int chunkCoord)
    {
        if (loadedChunks.Contains(chunkCoord))
            return;

        LoadChunk(chunkCoord);
    }

    public void RebuildLoadedChunks()
    {
        foreach (var chunkCoord in loadedChunks)
            RebuildChunk(chunkCoord);
    }

    public Vector2Int GetChunkCoord(Vector3Int blockPos)
    {
        int half = worldManager.generator.chunkSize / 2;
        int chunkX = Mathf.FloorToInt((blockPos.x + half) / (float)worldManager.generator.chunkSize);
        int chunkZ = Mathf.FloorToInt((blockPos.z + half) / (float)worldManager.generator.chunkSize);
        return new Vector2Int(chunkX, chunkZ);
    }

    public BlockType GetBaseBlockAt(Vector3Int position)
    {
        int height = GetTerrainHeight(position.x, position.z, GetSeedOffset());

        if (position.y < 0 || position.y > height)
            return BlockType.Air;

        return GetBlockTypeForHeight(position.y, height);
    }

    private void LoadChunk(Vector2Int chunkCoord)
    {
        var generatedBlocks = GenerateChunkBlocks(chunkCoord);

        foreach (var (pos, type) in generatedBlocks)
        {
            if (removedBlocks.Contains(pos))
                continue;

            if (placedBlocks.TryGetValue(pos, out var placedType))
                worldManager.Blocks[pos] = placedType;
            else
                worldManager.Blocks[pos] = type;
        }

        foreach (var (pos, type) in placedBlocks)
            if (GetChunkCoord(pos) == chunkCoord)
                worldManager.Blocks[pos] = type;

        loadedChunks.Add(chunkCoord);
    }

    private void UnloadChunk(Vector2Int chunkCoord)
    {
        loadedChunks.Remove(chunkCoord);

        var positionsToRemove = new List<Vector3Int>();
        foreach (var pos in worldManager.Blocks.Keys)
            if (GetChunkCoord(pos) == chunkCoord)
                positionsToRemove.Add(pos);

        foreach (var pos in positionsToRemove)
            worldManager.Blocks.Remove(pos);

        if (chunkGOs.TryGetValue(chunkCoord, out var chunkGO))
        {
            Destroy(chunkGO);
            chunkGOs.Remove(chunkCoord);
        }
    }

    private void RebuildChunk(Vector2Int chunkCoord)
    {
        var visibleFaces = new Dictionary<Vector3Int, List<FaceDirection>>();

        foreach (var (pos, type) in worldManager.Blocks)
        {
            if (GetChunkCoord(pos) != chunkCoord) continue;
            if (!type.IsSolid()) continue;

            var faces = FaceCuller.GetVisibleFaces(pos, worldManager.Blocks);
            if (faces.Count > 0)
                visibleFaces[pos] = faces;
        }

        Mesh mesh = ChunkMeshBuilder.Build(visibleFaces, worldManager.Blocks, out var subMeshOrder);

        if (!chunkGOs.TryGetValue(chunkCoord, out var chunkGO) || chunkGO == null)
        {
            chunkGO = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
            chunkGO.transform.SetParent(transform);
            chunkGO.AddComponent<MeshFilter>();
            chunkGO.AddComponent<MeshRenderer>();
            chunkGO.AddComponent<MeshCollider>();
            chunkGOs[chunkCoord] = chunkGO;
        }

        chunkGO.GetComponent<MeshFilter>().mesh = mesh;

        var materials = new Material[subMeshOrder.Length];
        for (int i = 0; i < subMeshOrder.Length; i++)
            materials[i] = worldManager.GetMaterial(subMeshOrder[i]);

        chunkGO.GetComponent<MeshRenderer>().materials = materials;
        chunkGO.GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    private Vector2Int GetPlayerChunk()
    {
        if (worldManager.player == null)
            return Vector2Int.zero;

        return GetChunkCoord(Vector3Int.FloorToInt(worldManager.player.transform.position));
    }

    private Dictionary<Vector3Int, BlockType> GenerateChunkBlocks(Vector2Int chunkCoord)
    {
        var chunkBlocks = new Dictionary<Vector3Int, BlockType>();
        Vector2 offset = GetSeedOffset();
        int half = worldManager.generator.chunkSize / 2;
        int startX = chunkCoord.x * worldManager.generator.chunkSize - half;
        int startZ = chunkCoord.y * worldManager.generator.chunkSize - half;

        for (int x = startX; x < startX + worldManager.generator.chunkSize; x++)
            for (int z = startZ; z < startZ + worldManager.generator.chunkSize; z++)
            {
                int height = GetTerrainHeight(x, z, offset);

                for (int y = 0; y <= height; y++)
                    chunkBlocks[new Vector3Int(x, y, z)] = GetBlockTypeForHeight(y, height);
            }

        return chunkBlocks;
    }

    private int GetTerrainHeight(int x, int z, Vector2 offset)
    {
        float nx = (x + offset.x) * worldManager.generator.noiseScale;
        float nz = (z + offset.y) * worldManager.generator.noiseScale;
        return Mathf.FloorToInt(Mathf.PerlinNoise(nx, nz) * worldManager.generator.terrainHeight);
    }

    private BlockType GetBlockTypeForHeight(int y, int height)
    {
        if (y <= 3)
            return BlockType.Stone;

        if (y == height)
            return BlockType.Grass;

        return BlockType.Dirt;
    }

    private Vector2 GetSeedOffset()
    {
        var prng = new System.Random(seed);
        return new Vector2(prng.Next(-100000, 100000),
                           prng.Next(-100000, 100000));
    }
}
