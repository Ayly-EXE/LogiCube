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
    public GameObject[] allPrefabs;
    public GameObject player;
    public WorldGenerator generator;
    private string savePath;

    private readonly Dictionary<Vector3Int, GameObject> worldBlocks = new();
    private readonly Dictionary<Vector3Int, string> placed = new();
    private readonly Dictionary<Vector3Int, string> removed = new();

    private WorldSaveData loaded;

    void Awake()
    {
        savePath = Path.Combine(Application.persistentDataPath, "world.json");
        loaded = LoadSaveFileOrCreate();
    }

    void Start()
    {
        // 1) Regénère le monde de base
        generator.Generate(loaded.seed, this);

        // 2) Applique le delta
        ApplyDelta(loaded);

        // 3) Player
        if (loaded.player != null && player != null)
        {
            player.transform.position = loaded.player.position;
            player.transform.rotation = loaded.player.rotation;
        }
    }

    public void RegisterGeneratedBlock(GameObject go, Vector3Int pos)
    {
        worldBlocks[pos] = go;
    }

    public void PlayerPlacedBlock(Vector3 pos, string blockType)
    {
        Vector3Int p = Vector3Int.FloorToInt(pos);

        removed.Remove(p);
        placed[p] = blockType;

        RemoveBlockVisual(p);

        var prefab = FindPrefabByName(blockType);
        if (prefab != null)
        {
            var go = Instantiate(prefab, (Vector3)p, Quaternion.identity);
            worldBlocks[p] = go;
        }
    }

    public void PlayerDestroyedBlock(GameObject hitObject)
    {
        if (hitObject == null) return;
        Vector3Int p = Vector3Int.FloorToInt(hitObject.transform.position);

        if (placed.Remove(p))
        {
            RemoveBlockVisual(p);
            return;
        }

        if (worldBlocks.TryGetValue(p, out var go) && go != null)
        {
            string type = GetBlockTypeFromGO(go);
            removed[p] = type;
        }

        RemoveBlockVisual(p);
    }

    private void RemoveBlockVisual(Vector3Int pos)
    {
        if (worldBlocks.TryGetValue(pos, out var go) && go != null)
        {
            Destroy(go);
        }
        worldBlocks.Remove(pos);
    }

    public void SaveWorld()
    {
        var placedList = new List<BlockData>();
        foreach (var kv in placed)
            placedList.Add(new BlockData { position = kv.Key, blockType = kv.Value });

        var removedList = new List<BlockData>();
        foreach (var kv in removed)
            removedList.Add(new BlockData { position = kv.Key, blockType = kv.Value });

        var save = new WorldSaveData
        {
            seed = loaded.seed,
            placedBlocks = placedList,
            removedBlocks = removedList,
            player = (player == null) ? null : new PlayerData
            {
                position = player.transform.position,
                rotation = player.transform.rotation
            }
        };

        File.WriteAllText(savePath, JsonUtility.ToJson(save, true));
    }

    private WorldSaveData LoadSaveFileOrCreate()
    {
        if (!File.Exists(savePath))
        {
            return new WorldSaveData { seed = 12345 };
        }

        var json = File.ReadAllText(savePath);
        var save = JsonUtility.FromJson<WorldSaveData>(json);

        save.placedBlocks ??= new();
        save.removedBlocks ??= new();
        return save;
    }

    private void ApplyDelta(WorldSaveData save)
    {
        // 1) remove
        foreach (var b in save.removedBlocks)
        {
            if (worldBlocks.TryGetValue(b.position, out var go) && go != null)
            {
                if (GetBlockTypeFromGO(go) == b.blockType)
                    RemoveBlockVisual(b.position);
            }
            else
            {
                RemoveBlockVisual(b.position);
            }
        }

        // 2) place
        foreach (var b in save.placedBlocks)
            PlayerPlacedBlock(b.position, b.blockType);
    }

    private GameObject FindPrefabByName(string name)
    {
        foreach (var prefab in allPrefabs)
            if (prefab != null && prefab.name == name) return prefab;
        return null;
    }

    private string GetBlockTypeFromGO(GameObject go)
    {
        return go.name.Replace("(Clone)", "").Trim();
    }

    private void OnApplicationQuit() => SaveWorld();
}