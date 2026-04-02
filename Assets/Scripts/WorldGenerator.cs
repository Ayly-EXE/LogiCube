using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    [Header("Terrain")]
    public int chunkSize = 32;
    public float noiseScale = 0.05f;
    public int terrainHeight = 30;

    [Header("Structures")]
    [SerializeField] private bool enableStructures = true;
    [SerializeField] private string structuresFolderName = "SavedStructures";
    [SerializeField, Range(0f, 1f)] private float structureSpawnChance = 0.50f;
    [SerializeField, Min(8)] private int structureRegionSize = 24;
    [SerializeField, Min(0)] private int maxGroundVariation = 2;

    private int configuredSeed;
    private bool hasConfiguredSeed;
    private Vector2 seedOffset;

    private readonly List<StructureTemplate> structureTemplates = new();
    private int maxStructureHorizontalReach;
    private int structureRegionSearchRadius = 1;

    public void Configure(int seed)
    {
        if (hasConfiguredSeed && configuredSeed == seed)
            return;

        configuredSeed = seed;
        hasConfiguredSeed = true;
        seedOffset = BuildSeedOffset(seed);

        LoadStructuresFromDisk();
    }

    public Dictionary<Vector3Int, BlockType> Generate(int seed)
    {
        Configure(seed);
        return GenerateChunk(Vector2Int.zero);
    }

    public Dictionary<Vector3Int, BlockType> GenerateChunk(Vector2Int chunkCoord)
    {
        EnsureConfigured();

        var blocks = new Dictionary<Vector3Int, BlockType>();
        int half = chunkSize / 2;
        int startX = chunkCoord.x * chunkSize - half;
        int startZ = chunkCoord.y * chunkSize - half;
        int endXExclusive = startX + chunkSize;
        int endZExclusive = startZ + chunkSize;

        for (int x = startX; x < endXExclusive; x++)
            for (int z = startZ; z < endZExclusive; z++)
            {
                int height = GetTerrainHeight(x, z);

                for (int y = 0; y <= height; y++)
                    blocks[new Vector3Int(x, y, z)] = GetBlockTypeForHeight(y, height);
            }

        if (enableStructures)
            ApplyStructuresToChunk(blocks, startX, startZ, endXExclusive, endZExclusive);

        return blocks;
    }

    public BlockType GetGeneratedBlockAt(Vector3Int position)
    {
        return GetGeneratedBlockAt(position.x, position.y, position.z);
    }

    public BlockType GetGeneratedBlockAt(int x, int y, int z)
    {
        EnsureConfigured();

        if (y < 0)
            return BlockType.Air;

        if (enableStructures && TryGetStructureBlockAt(x, y, z, out BlockType structureType))
            return structureType;

        int height = GetTerrainHeight(x, z);
        if (y > height)
            return BlockType.Air;

        return GetBlockTypeForHeight(y, height);
    }

    private void EnsureConfigured()
    {
        if (!hasConfiguredSeed)
            Configure(0);
    }

    private void LoadStructuresFromDisk()
    {
        structureTemplates.Clear();
        maxStructureHorizontalReach = 0;
        structureRegionSearchRadius = 1;

        if (!enableStructures)
            return;

        string folderPath = Path.Combine(Application.dataPath, structuresFolderName);
        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning($"[WorldGenerator] Dossier de structures introuvable: {folderPath}");
            return;
        }

        string[] files = Directory.GetFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly);
        Array.Sort(files, StringComparer.Ordinal);

        foreach (string filePath in files)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json))
                    continue;

                StructureFileData data = JsonUtility.FromJson<StructureFileData>(json);
                StructureTemplate template = BuildTemplateFromFile(data, filePath);
                if (template != null)
                    structureTemplates.Add(template);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[WorldGenerator] Structure ignoree ({filePath}): {exception.Message}");
            }
        }

        if (structureTemplates.Count == 0)
            return;

        foreach (StructureTemplate template in structureTemplates)
        {
            int reachX = Mathf.Max(Mathf.Abs(template.minX), Mathf.Abs(template.maxX));
            int reachZ = Mathf.Max(Mathf.Abs(template.minZ), Mathf.Abs(template.maxZ));
            maxStructureHorizontalReach = Mathf.Max(maxStructureHorizontalReach, Mathf.Max(reachX, reachZ));
        }

        structureRegionSearchRadius = Mathf.Max(
            1,
            Mathf.CeilToInt((maxStructureHorizontalReach + 1f) / Mathf.Max(1, structureRegionSize)) + 1
        );

        Debug.Log($"[WorldGenerator] {structureTemplates.Count} structure(s) chargee(s), seed {configuredSeed}.");
    }

    private StructureTemplate BuildTemplateFromFile(StructureFileData data, string filePath)
    {
        if (data == null || data.blocks == null || data.blocks.Length == 0)
            return null;

        Dictionary<Vector3Int, BlockType> blockLookup = new();

        for (int i = 0; i < data.blocks.Length; i++)
        {
            RawStructureBlock block = data.blocks[i];
            if (!Enum.IsDefined(typeof(BlockType), block.type))
                continue;

            BlockType blockType = (BlockType)block.type;
            if (!blockType.IsSolid())
                continue;

            blockLookup[new Vector3Int(block.x, block.y, block.z)] = blockType;
        }

        if (blockLookup.Count == 0)
            return null;

        List<StructureBlock> orderedBlocks = new(blockLookup.Count);
        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minY = int.MaxValue;
        int maxY = int.MinValue;
        int minZ = int.MaxValue;
        int maxZ = int.MinValue;

        foreach (KeyValuePair<Vector3Int, BlockType> pair in blockLookup)
        {
            Vector3Int localPos = pair.Key;
            BlockType blockType = pair.Value;
            orderedBlocks.Add(new StructureBlock(localPos, blockType));

            minX = Mathf.Min(minX, localPos.x);
            maxX = Mathf.Max(maxX, localPos.x);
            minY = Mathf.Min(minY, localPos.y);
            maxY = Mathf.Max(maxY, localPos.y);
            minZ = Mathf.Min(minZ, localPos.z);
            maxZ = Mathf.Max(maxZ, localPos.z);
        }

        orderedBlocks.Sort(static (a, b) =>
        {
            int cmpX = a.localPosition.x.CompareTo(b.localPosition.x);
            if (cmpX != 0) return cmpX;

            int cmpY = a.localPosition.y.CompareTo(b.localPosition.y);
            if (cmpY != 0) return cmpY;

            int cmpZ = a.localPosition.z.CompareTo(b.localPosition.z);
            if (cmpZ != 0) return cmpZ;

            return a.type.CompareTo(b.type);
        });

        string templateName = string.IsNullOrWhiteSpace(data.structureName)
            ? Path.GetFileNameWithoutExtension(filePath)
            : data.structureName;

        return new StructureTemplate(
            templateName,
            orderedBlocks,
            blockLookup,
            minX, maxX,
            minY, maxY,
            minZ, maxZ
        );
    }

    private void ApplyStructuresToChunk(
        Dictionary<Vector3Int, BlockType> chunkBlocks,
        int startX,
        int startZ,
        int endXExclusive,
        int endZExclusive)
    {
        if (structureTemplates.Count == 0)
            return;

        int minRegionX = FloorDiv(startX - maxStructureHorizontalReach, structureRegionSize);
        int maxRegionX = FloorDiv(endXExclusive - 1 + maxStructureHorizontalReach, structureRegionSize);
        int minRegionZ = FloorDiv(startZ - maxStructureHorizontalReach, structureRegionSize);
        int maxRegionZ = FloorDiv(endZExclusive - 1 + maxStructureHorizontalReach, structureRegionSize);

        for (int regionX = minRegionX; regionX <= maxRegionX; regionX++)
            for (int regionZ = minRegionZ; regionZ <= maxRegionZ; regionZ++)
            {
                if (!TryGetStructureSpawnInRegion(regionX, regionZ, out StructureSpawn spawn))
                    continue;

                for (int i = 0; i < spawn.template.blocks.Count; i++)
                {
                    StructureBlock block = spawn.template.blocks[i];
                    int worldX = spawn.anchorX + block.localPosition.x;
                    int worldY = spawn.anchorY + block.localPosition.y;
                    int worldZ = spawn.anchorZ + block.localPosition.z;

                    if (worldY < 0)
                        continue;

                    if (worldX < startX || worldX >= endXExclusive || worldZ < startZ || worldZ >= endZExclusive)
                        continue;

                    chunkBlocks[new Vector3Int(worldX, worldY, worldZ)] = block.type;
                }
            }
    }

    private bool TryGetStructureBlockAt(int worldX, int worldY, int worldZ, out BlockType blockType)
    {
        blockType = BlockType.Air;

        if (structureTemplates.Count == 0)
            return false;

        int regionX = FloorDiv(worldX, structureRegionSize);
        int regionZ = FloorDiv(worldZ, structureRegionSize);
        bool found = false;

        for (int rx = regionX - structureRegionSearchRadius; rx <= regionX + structureRegionSearchRadius; rx++)
            for (int rz = regionZ - structureRegionSearchRadius; rz <= regionZ + structureRegionSearchRadius; rz++)
            {
                if (!TryGetStructureSpawnInRegion(rx, rz, out StructureSpawn spawn))
                    continue;

                if (worldY < spawn.anchorY + spawn.template.minY || worldY > spawn.anchorY + spawn.template.maxY)
                    continue;

                Vector3Int localPos = new(
                    worldX - spawn.anchorX,
                    worldY - spawn.anchorY,
                    worldZ - spawn.anchorZ
                );

                if (spawn.template.blockLookup.TryGetValue(localPos, out BlockType candidate))
                {
                    blockType = candidate;
                    found = true;
                }
            }

        return found;
    }

    private bool TryGetStructureSpawnInRegion(int regionX, int regionZ, out StructureSpawn spawn)
    {
        spawn = default;

        if (structureTemplates.Count == 0)
            return false;

        float spawnRoll = Hash01(configuredSeed, regionX, regionZ, 101);
        if (spawnRoll > structureSpawnChance)
            return false;

        int templateIndex = HashRange(configuredSeed, regionX, regionZ, 173, 0, structureTemplates.Count);
        StructureTemplate template = structureTemplates[templateIndex];

        int localX = HashRange(configuredSeed, regionX, regionZ, 251, 0, structureRegionSize);
        int localZ = HashRange(configuredSeed, regionX, regionZ, 331, 0, structureRegionSize);

        int anchorX = regionX * structureRegionSize + localX;
        int anchorZ = regionZ * structureRegionSize + localZ;
        int anchorY = GetTerrainHeight(anchorX, anchorZ);

        if (!CanPlaceStructureNaturally(template, anchorX, anchorY, anchorZ))
            return false;

        spawn = new StructureSpawn(template, anchorX, anchorY, anchorZ);
        return true;
    }

    private bool CanPlaceStructureNaturally(StructureTemplate template, int anchorX, int anchorY, int anchorZ)
    {
        int minTerrain = int.MaxValue;
        int maxTerrain = int.MinValue;
        bool touchesGround = false;

        for (int i = 0; i < template.blocks.Count; i++)
        {
            StructureBlock block = template.blocks[i];

            int worldX = anchorX + block.localPosition.x;
            int worldY = anchorY + block.localPosition.y;
            int worldZ = anchorZ + block.localPosition.z;
            int terrainY = GetTerrainHeight(worldX, worldZ);

            minTerrain = Mathf.Min(minTerrain, terrainY);
            maxTerrain = Mathf.Max(maxTerrain, terrainY);

            if (worldY < terrainY - 1)
                return false;

            if (worldY == terrainY || worldY == terrainY + 1)
                touchesGround = true;
        }

        if (maxTerrain - minTerrain > maxGroundVariation)
            return false;

        return touchesGround;
    }

    private int GetTerrainHeight(int x, int z)
    {
        float nx = (x + seedOffset.x) * noiseScale;
        float nz = (z + seedOffset.y) * noiseScale;
        return Mathf.FloorToInt(Mathf.PerlinNoise(nx, nz) * terrainHeight);
    }

    private BlockType GetBlockTypeForHeight(int y, int height)
    {
        if (y <= 3)
            return BlockType.Stone;

        if (y == height)
            return BlockType.Grass;

        return BlockType.Dirt;
    }

    private static Vector2 BuildSeedOffset(int seed)
    {
        var prng = new System.Random(seed);
        return new Vector2(prng.Next(-100000, 100000), prng.Next(-100000, 100000));
    }

    private static int FloorDiv(int value, int divisor)
    {
        if (divisor == 0)
            return 0;

        return Mathf.FloorToInt(value / (float)divisor);
    }

    private static uint Hash32(int seed, int x, int z, int salt)
    {
        unchecked
        {
            uint hash = 2166136261u;
            hash = (hash ^ (uint)seed) * 16777619u;
            hash = (hash ^ (uint)x) * 16777619u;
            hash = (hash ^ (uint)z) * 16777619u;
            hash = (hash ^ (uint)salt) * 16777619u;
            hash ^= hash >> 13;
            hash *= 1274126177u;
            hash ^= hash >> 16;
            return hash;
        }
    }

    private static float Hash01(int seed, int x, int z, int salt)
    {
        uint hash = Hash32(seed, x, z, salt);
        return (hash & 0x00FFFFFFu) / 16777215f;
    }

    private static int HashRange(int seed, int x, int z, int salt, int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
            return minInclusive;

        uint range = (uint)(maxExclusive - minInclusive);
        uint hash = Hash32(seed, x, z, salt);
        return minInclusive + (int)(hash % range);
    }

#pragma warning disable CS0649
    [Serializable]
    private class StructureFileData
    {
        public string structureName = string.Empty;
        public RawStructureBlock[] blocks = Array.Empty<RawStructureBlock>();
    }

    [Serializable]
    private struct RawStructureBlock
    {
        public int type;
        public int x;
        public int y;
        public int z;
    }
#pragma warning restore CS0649

    private readonly struct StructureBlock
    {
        public readonly Vector3Int localPosition;
        public readonly BlockType type;

        public StructureBlock(Vector3Int localPosition, BlockType type)
        {
            this.localPosition = localPosition;
            this.type = type;
        }
    }

    private sealed class StructureTemplate
    {
        public readonly string name;
        public readonly List<StructureBlock> blocks;
        public readonly Dictionary<Vector3Int, BlockType> blockLookup;

        public readonly int minX;
        public readonly int maxX;
        public readonly int minY;
        public readonly int maxY;
        public readonly int minZ;
        public readonly int maxZ;

        public StructureTemplate(
            string name,
            List<StructureBlock> blocks,
            Dictionary<Vector3Int, BlockType> blockLookup,
            int minX,
            int maxX,
            int minY,
            int maxY,
            int minZ,
            int maxZ)
        {
            this.name = name;
            this.blocks = blocks;
            this.blockLookup = blockLookup;
            this.minX = minX;
            this.maxX = maxX;
            this.minY = minY;
            this.maxY = maxY;
            this.minZ = minZ;
            this.maxZ = maxZ;
        }
    }

    private readonly struct StructureSpawn
    {
        public readonly StructureTemplate template;
        public readonly int anchorX;
        public readonly int anchorY;
        public readonly int anchorZ;

        public StructureSpawn(StructureTemplate template, int anchorX, int anchorY, int anchorZ)
        {
            this.template = template;
            this.anchorX = anchorX;
            this.anchorY = anchorY;
            this.anchorZ = anchorZ;
        }
    }
}
