using UnityEngine;

public class PlayerAction : MonoBehaviour
{
    public WorldManagerScript worldManager;

    public BlockType selectedBlock = BlockType.Stone;

    public void ChangeBlockType(BlockType newBlockType)
    {
        selectedBlock = newBlockType;
    }

    // Envoie un rayon depuis la caméra vers l'avant
    public RaycastHit? GetHit(float maxDistance = 100f)
    {
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            return hit;
        return null;
    }

    // Trouve la position du bloc à placer (face adjacente au bloc touché)
    public Vector3 GetBlockPlacementPosition(RaycastHit hit)
    {
        Vector3 insideBlock = hit.point - hit.normal * 0.5f;
        Vector3Int hitBlockPos = Vector3Int.FloorToInt(insideBlock);
        return hitBlockPos + Vector3Int.RoundToInt(hit.normal);
    }

    void Update()
    {
        // Clic droit = poser un bloc
        if (Input.GetMouseButtonDown(1))
        {
            RaycastHit? hit = GetHit();
            if (hit.HasValue)
            {
                Vector3 placePos = GetBlockPlacementPosition(hit.Value);
                worldManager.PlaceBlock(placePos, selectedBlock);
            }
        }

        // Clic gauche = détruire un bloc
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit? hit = GetHit();
            if (hit.HasValue)
            {
                Vector3 insideBlock = hit.Value.point - hit.Value.normal * 0.5f;
                worldManager.DestroyBlock(insideBlock);
            }
        }
    }
}