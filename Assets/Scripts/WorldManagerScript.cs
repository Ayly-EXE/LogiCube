using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Collections;


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
    public static string CurrentWorldId { get; private set; }
    public static string WorldsDirectory => Path.Combine(Application.persistentDataPath, "worlds");
    private string SavePath => GetWorldFilePath(CurrentWorldId);
    public bool IsWorldInitialized => worldInitialized;

    [Header("Références")]
    public WorldGenerator generator;
    public GameObject player;
    public ChunkSpawner chunkSpawner;

    [Header("Matériaux")]
    public List<BlockMaterialEntry> blockMaterials = new();

    private Dictionary<BlockType, Material> materialLookup = new();

    private Dictionary<Vector3Int, BlockType> blocks = new();

    private bool worldInitialized;
    private Vector3 startPlayerPosition;
    private Quaternion startPlayerRotation;

    [Header("TNT")]
    public GameObject tntPrimedPrefab;
    public float tntFuseDuration = 2.5f;

    [Header("VFX")]
    public ParticleSystem explosionPrefab;

    public static void SelectWorld(string worldId)
    {
        CurrentWorldId = NormalizeWorldId(worldId);
    }

    public static bool IsValidWorldId(string worldId)
    {
        if (string.IsNullOrWhiteSpace(worldId))
            return false;

        foreach (char c in worldId)
        {
            if (char.IsLetterOrDigit(c))
                continue;

            if (c == '_' || c == '-')
                continue;

            return false;
        }

        return true;
    }

    public static string NormalizeWorldId(string worldId)
    {
        return worldId == null ? string.Empty : worldId.Trim();
    }

    public static List<string> GetAvailableWorldIds()
    {
        List<string> ids = new List<string>();
        if (!Directory.Exists(WorldsDirectory))
            return ids;

        foreach (string path in Directory.GetFiles(WorldsDirectory, "*.json"))
            ids.Add(Path.GetFileNameWithoutExtension(path));

        ids.Sort(System.StringComparer.OrdinalIgnoreCase);
        return ids;
    }

    public static string GetWorldFilePath(string worldId)
    {
        string safeId = IsValidWorldId(worldId) ? worldId : "default";
        return Path.Combine(WorldsDirectory, safeId + ".json");
    }

    void Start()
    {
        materialLookup.Clear();
        foreach (var entry in blockMaterials)
            materialLookup[entry.type] = entry.material;

        if (player != null)
        {
            startPlayerPosition = player.transform.position;
            startPlayerRotation = player.transform.rotation;
        }

        if (string.IsNullOrWhiteSpace(CurrentWorldId))
        {
            SetGameplayEnabled(false);
            return;
        }

        StartSelectedWorld();
    }

    private void SetGameplayEnabled(bool enabled)
    {
        if (player != null)
            player.SetActive(enabled);
    }

    public void StartSelectedWorld()
    {
        if (worldInitialized)
            return;

        if (!IsValidWorldId(CurrentWorldId))
            return;

        Directory.CreateDirectory(WorldsDirectory);
        bool worldFileExists = File.Exists(SavePath);

        var save = chunkSpawner.Initialize(this, SavePath);
        Debug.Log("World file: " + SavePath);

        if (player != null)
        {
            if (save.player != null)
            {
                player.transform.position = save.player.position;
                player.transform.rotation = save.player.rotation;
            }
            else
            {
                player.transform.position = startPlayerPosition;
                player.transform.rotation = startPlayerRotation;
            }
        }

        worldInitialized = true;
        SetGameplayEnabled(true);

        if (!worldFileExists || save.player == null)
            SaveWorld();
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
        if (!worldInitialized)
            return;

        chunkSpawner.SaveWorld(SavePath, player);
    }

    void OnApplicationQuit()
    {
        if (worldInitialized)
            SaveWorld();
    }

    public Dictionary<Vector3Int, BlockType> Blocks => blocks;

    Vector3Int GetHitBlockPosition(RaycastHit hit)
    {
        return Vector3Int.FloorToInt(hit.point - hit.normal * 0.01f);
    }

    private void CascadeExplosion(int radius, Vector3Int center, HashSet<Vector3Int> blocksToDestroy, HashSet<Vector3Int> tnt)
    {
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

                    if (blocks[pos] == BlockType.Tnt && !tnt.Contains(pos))
                    {
                        tnt.Add(pos);
                        CascadeExplosion(radius, pos, blocksToDestroy, tnt);
                        Debug.Log("will kaboom at pos" + pos);

                    }

                    blocksToDestroy.Add(pos);
                }
            }
        }
    }

    public void Explode(Vector3 origin)
    {
        int radius = 3;
        Vector3Int center = Vector3Int.FloorToInt(origin);

        HashSet<Vector3Int> blocksToDestroy = new HashSet<Vector3Int>();
        HashSet<Vector3Int> tnt = new HashSet<Vector3Int>();
        tnt.Add(center);

        CascadeExplosion(radius, center, blocksToDestroy, tnt);

        // Toutes les TNT de la cascade partent ensemble
        StartCoroutine(IgniteThenExplode(blocksToDestroy, tnt));
    }

    private IEnumerator IgniteThenExplode(HashSet<Vector3Int> blocksToDestroy, HashSet<Vector3Int> tnt)
    {
        List<GameObject> primedInstances = new List<GameObject>();

        // 1) allumage simultané (même frame)
        foreach (Vector3Int pos in tnt)
        {
            if (!blocks.TryGetValue(pos, out var type) || type != BlockType.Tnt)
                continue;

            RemoveBlockNoRebuild(pos);

            if (tntPrimedPrefab != null)
                primedInstances.Add(Instantiate(tntPrimedPrefab, pos + Vector3.one * 0.5f, Quaternion.identity));
        }

        RebuildChunk();

        // 2) délai commun
        yield return new WaitForSeconds(tntFuseDuration);

        // 3) explosion simultanée
        foreach (GameObject primed in primedInstances)
            if (primed != null)
                Destroy(primed);

        foreach (Vector3Int pos in tnt)
        {
            ParticleSystem vfx = Instantiate(explosionPrefab, pos, Quaternion.identity);
            vfx.Play();
            Destroy(vfx.gameObject, vfx.main.duration + vfx.main.startLifetime.constantMax);
        }

        DestroyMultipleBlocks(blocksToDestroy);
    }

    private void RemoveBlockNoRebuild(Vector3Int p)
    {
        if (!blocks.ContainsKey(p))
            return;

        blocks.Remove(p);
        chunkSpawner.OnBlockDestroyed(p);
    }

}
