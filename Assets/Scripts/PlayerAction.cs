using UnityEngine;

public class PlayerAction : MonoBehaviour
{
    public WorldManagerScript worldManager;
    public BlockType selectedBlock = BlockType.Stone;
    public CharacterController playerController;

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
            TryPlaceBlock();

        if (Input.GetMouseButtonDown(0))
            TryBreakBlock();

        if (Input.GetKeyDown(KeyCode.E))
        {
            RaycastHit? hit = GetHit();

            if (!hit.HasValue)
                return;

            Vector3Int block = GetHitBlockPosition(hit.Value);
            var p = Vector3Int.FloorToInt(block);

            Debug.Log(block);

            if (worldManager.Blocks[p] == BlockType.Tnt)
            {
                Debug.Log("Should boom");
                worldManager.Explode((Vector3)block);
            }

        }
    }

    // Envoie un rayon devant la caméra pour voir ce qu'on vise
    RaycastHit? GetHit(float maxDistance = 100f)
    {
        // On démarre un tout petit peu devant la caméra
        // pour éviter de toucher le joueur directement
        Vector3 origin = transform.position + transform.forward * 0.1f;
        Ray ray = new Ray(origin, transform.forward);

        // On récupère tous les objets touchés par le rayon
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);

        // On trie les objets du plus proche au plus loin
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // On cherche le premier objet qui n'est pas le joueur
        foreach (RaycastHit hit in hits)
        {
            if (IsPlayerObject(hit.transform))
                continue;

            return hit;
        }

        // Rien n'a été touché
        return null;
    }

    bool IsPlayerObject(Transform hitTransform)
    {
        if (playerController == null)
            return false;

        Transform playerRoot = playerController.transform;
        return hitTransform == playerRoot || hitTransform.IsChildOf(playerRoot);
    }

    public void ChangeBlockType(BlockType newBlockType)
    {
        selectedBlock = newBlockType;
        Debug.Log("Bloc choisi : " + selectedBlock);
    }

    // Donne la case du bloc touché
    Vector3Int GetHitBlockPosition(RaycastHit hit)
    {
        return Vector3Int.FloorToInt(hit.point - hit.normal * 0.01f);
    }

    // Donne la case où placer le nouveau bloc
    Vector3Int GetBlockPlacementPosition(RaycastHit hit)
    {
        Vector3Int hitBlockPos = GetHitBlockPosition(hit);
        return hitBlockPos + Vector3Int.RoundToInt(hit.normal);
    }

    // Vérifie si le bloc qu'on veut poser serait dans le joueur
    bool IsInsidePlayer(Vector3Int blockPos)
    {
        if (playerController == null)
            return false;

        // On prend la boîte du joueur
        Bounds b = playerController.bounds;

        // On récupère les coins min et max de cette boîte
        // avec une toute petite marge pour éviter les bords exacts
        Vector3 min = b.min + Vector3.one * 0.001f;
        Vector3 max = b.max - Vector3.one * 0.001f;

        // On parcourt toutes les cases occupées par le joueur
        // entre le coin minimum et le coin maximum
        for (int x = Mathf.FloorToInt(min.x); x <= Mathf.FloorToInt(max.x); x++)
        {
            for (int y = Mathf.FloorToInt(min.y); y <= Mathf.FloorToInt(max.y); y++)
            {
                for (int z = Mathf.FloorToInt(min.z); z <= Mathf.FloorToInt(max.z); z++)
                {
                    // Si la case du bloc à poser est une case du joueur,
                    // alors on ne peut pas poser le bloc
                    if (new Vector3Int(x, y, z) == blockPos)
                        return true;
                }
            }
        }

        return false;
    }

    void TryPlaceBlock()
    {
        RaycastHit? hit = GetHit();
        if (!hit.HasValue)
            return;

        Vector3Int placePos = GetBlockPlacementPosition(hit.Value);

        if (!IsInsidePlayer(placePos))
            worldManager.PlaceBlock(placePos, selectedBlock);
    }

    void TryBreakBlock()
    {
        RaycastHit? hit = GetHit();
        if (!hit.HasValue)
            return;

        SlimeController slime = hit.Value.transform.GetComponentInParent<SlimeController>();
        if (slime != null)
        {
            slime.TakeHit();
            return;
        }

        Vector3Int hitBlockPos = GetHitBlockPosition(hit.Value);
        Debug.Log(hitBlockPos);
        worldManager.DestroyBlock(hitBlockPos);
    }
}
