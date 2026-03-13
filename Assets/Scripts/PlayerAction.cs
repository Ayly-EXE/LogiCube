using UnityEngine;

public class PlayerAction : MonoBehaviour
{
    public WorldManagerScript worldManager;
    public BlockType selectedBlock = BlockType.Stone;
    public CharacterController playerController;


    public RaycastHit? GetHit(float maxDistance = 100f)
    {
        Vector3 origin = transform.position + transform.forward * 0.1f;
        Ray ray = new Ray(origin, transform.forward);

        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (playerController != null)
            {
                Transform playerRoot = playerController.transform;

                // Ignore tout ce qui appartient au joueur
                if (hit.transform == playerRoot || hit.transform.IsChildOf(playerRoot))
                    continue;
            }

            return hit;
        }

        return null;
    }

    public void ChangeBlockType(BlockType newBlockType)
    {
        selectedBlock = newBlockType;
        Debug.Log("Selected block type changed to: " + selectedBlock);
    }

    public Vector3Int GetHitBlockPosition(RaycastHit hit)
    {
        return Vector3Int.FloorToInt(hit.point - hit.normal * 0.01f);
    }

    public Vector3Int GetBlockPlacementPosition(RaycastHit hit)
    {
        Vector3Int hitBlockPos = GetHitBlockPosition(hit);
        return hitBlockPos + Vector3Int.RoundToInt(hit.normal);
    }

    public bool IsInsidePlayer(Vector3Int blockPos)
    {
        if (playerController == null) return false;

        Bounds b = playerController.bounds;

        Vector3 min = b.min + Vector3.one * 0.001f;
        Vector3 max = b.max - Vector3.one * 0.001f;

        for (int x = Mathf.FloorToInt(min.x); x <= Mathf.FloorToInt(max.x); x++)
        {
            for (int y = Mathf.FloorToInt(min.y); y <= Mathf.FloorToInt(max.y); y++)
            {
                for (int z = Mathf.FloorToInt(min.z); z <= Mathf.FloorToInt(max.z); z++)
                {
                    if (new Vector3Int(x, y, z) == blockPos)
                        return true;
                }
            }
        }

        return false;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            RaycastHit? hit = GetHit();
            if (hit.HasValue)
            {
                Debug.Log("Hit: " + hit.Value.collider.name);

                Vector3Int placePos = GetBlockPlacementPosition(hit.Value);

                if (!IsInsidePlayer(placePos))
                    worldManager.PlaceBlock(placePos, selectedBlock);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit? hit = GetHit();
            if (hit.HasValue)
            {
                Vector3Int hitBlockPos = GetHitBlockPosition(hit.Value);
                worldManager.DestroyBlock(hitBlockPos);
            }
        }
    }
}