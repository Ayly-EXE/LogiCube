using UnityEngine;

public class SlimeSpawner : MonoBehaviour
{
    [Header("Spawn")]
    public SlimeController slimePrefab;
    public Transform spawnOrigin;
    public KeyCode spawnKey = KeyCode.P;
    [Min(0f)] public float spawnDistance = 4f;
    [Min(0f)] public float spawnHeight = 1.5f;
    [Min(1)] public int maxAliveSlimes = 80;

    void Update()
    {
        if (Input.GetKeyDown(spawnKey))
            SpawnSlime();
    }

    public void SpawnSlime()
    {
        if (slimePrefab == null)
        {
            Debug.LogWarning("SlimeSpawner: slimePrefab is missing.");
            return;
        }

        if (FindObjectsByType<SlimeController>(FindObjectsSortMode.None).Length >= maxAliveSlimes)
            return;

        Transform origin = spawnOrigin != null ? spawnOrigin : transform;
        Vector3 spawnPos = origin.position + origin.forward * spawnDistance + Vector3.up * spawnHeight;

        if (Physics.Raycast(spawnPos + Vector3.up * 30f, Vector3.down, out RaycastHit hit, 80f))
            spawnPos = hit.point + Vector3.up * 0.55f;

        Instantiate(slimePrefab, spawnPos, Quaternion.identity);
    }
}
