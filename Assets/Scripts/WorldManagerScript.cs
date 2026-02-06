using System.Collections.Generic;
using UnityEngine;
using System.IO;

[System.Serializable]
public class BlockData
{
    public Vector3 position;
    public string blockType;
}

[System.Serializable]
public class PlayerData
{
    public Vector3 position;
    public Quaternion rotation;
}

[System.Serializable]
public class WorldData
{
    public List<BlockData> blocks = new List<BlockData>();
    public PlayerData player;
}

public class WorldManagerScript : MonoBehaviour
{
    public GameObject[] allPrefabs; // Assign all block prefabs in Inspector
    public GameObject player;       // Assign the player GameObject in Inspector

    private string savePath;

    void Awake()
    {
        savePath = Application.persistentDataPath + "/world.json";
        LoadWorld();
    }

    // Save all blocks and player
    public void SaveWorld()
    {
        WorldData world = new WorldData();

        // Save all blocks
        GameObject[] allBlocks = GameObject.FindGameObjectsWithTag("Block");
        foreach (GameObject block in allBlocks)
        {
            BlockData data = new BlockData();
            data.position = block.transform.position;
            data.blockType = block.name.Replace("(Clone)", "");
            world.blocks.Add(data);
        }

        // Save player
        if (player != null)
        {
            world.player = new PlayerData
            {
                position = player.transform.position,
                rotation = player.transform.rotation
            };
        }

        string json = JsonUtility.ToJson(world, true);
        File.WriteAllText(savePath, json);
        Debug.Log("World saved at: " + savePath);
    }

    // Load all blocks and player
    public void LoadWorld()
    {
        if (!File.Exists(savePath))
        {
            Debug.Log("No world file found.");
            return;
        }

        string json = File.ReadAllText(savePath);
        WorldData world = JsonUtility.FromJson<WorldData>(json);

        // Clear existing blocks first (optional)
        foreach (GameObject block in GameObject.FindGameObjectsWithTag("Block"))
        {
            Destroy(block);
        }

        // Load blocks
        foreach (BlockData data in world.blocks)
        {
            GameObject prefab = FindPrefabByName(data.blockType);
            if (prefab != null)
            {
                Instantiate(prefab, data.position, Quaternion.identity);
            }
        }

        // Load player position and rotation
        if (world.player != null && player != null)
        {
            player.transform.position = world.player.position;
            player.transform.rotation = world.player.rotation;
        }

        Debug.Log("World loaded!");
    }

    // Helper to find prefab by name
    private GameObject FindPrefabByName(string name)
    {
        foreach (GameObject prefab in allPrefabs)
        {
            if (prefab.name == name)
                return prefab;
        }
        return null;
    }

    // Automatically save on exit
    private void OnApplicationQuit()
    {
        SaveWorld();
    }
}
